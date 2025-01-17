#pragma warning disable CS8620
using ssAppModels.EFModels;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppServices.Api.Yahoo;
using ssAppServices.Api.Rakuten;

// 【 処理概要 】
// 各モールショップの新規注文（出荷対象注文）を取得し、DailyOrderNewsに保存する。
// 【 前提 】
// API（注文番号リスト取得）のページング処理について
// 　・楽天：呼出しメソッドでページング処理が実装されているため全件取得される。
// 　・Yahoo：GetOrderSearchは、2,000件/リクエストなので十分。
// API（注文詳細情報の取得）のページング処理について
// 　・楽天：100件/リクエストなので、ページング処理を実装する。
// 　・Yahoo：GetOrderInfoは全ての注文番号を取得する。日次の取得件数（Max200件想定）であればページング処理実装は不要。

namespace ssAppServices.Apps
{
   public class SetDailyOrderNews
   {
      private readonly ssAppDBContext _dbContext;
      private readonly YahooOrderList _yahooOrderList;
      private readonly YahooOrderInfo _yahooOrderInfo;
      private readonly RakutenSearchOrder _rakutenSearchOrder;
      private readonly RakutenGetOrder _rakutenGetOrder;

      public SetDailyOrderNews(
         ssAppDBContext dbContext,
         YahooOrderList yahooOrderList, YahooOrderInfo yahooOrderInfo,
         RakutenSearchOrder rakutenSearchOrder, RakutenGetOrder rakutenGetOrder)
      {
         _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
         _yahooOrderList = yahooOrderList ?? throw new ArgumentNullException(nameof(yahooOrderList));
         _yahooOrderInfo = yahooOrderInfo ?? throw new ArgumentNullException(nameof(yahooOrderInfo));
         _rakutenSearchOrder = rakutenSearchOrder ?? throw new ArgumentNullException(nameof(rakutenSearchOrder));
         _rakutenGetOrder = rakutenGetOrder ?? throw new ArgumentNullException(nameof(rakutenGetOrder));
      }

      /// <summary>
      /// Rakuten注文情報を取得しDailyOrderNewsに保存する。
      /// 注文明細（GetOrder）Max 100件/リクエストなので、ページング処理を実装する。
      /// </summary>
      /// 戻り値：(注文番号リスト, 注文詳細情報)→デバッグ用
      /// 　注文番号リストは全件を戻り値へセットする。
      /// 　注文情報は最初のレスポンス（最大100件）のみ戻り値へセットする。
      public (List<string>, RakutenGetOrderResponse) FetchDailyOrderFromRakuten(RakutenShop rakutenShop)
      {
         var mallShop = Enum.GetValues(typeof(MallShop)).Cast<MallShop>()
               .FirstOrDefault(m => m.ToString() == rakutenShop.ToString());
         // 注文一覧取得：RakutenSearchOrder実行
         var searchOrderParameter = RakutenSearchOrderRequestFactory.NewOrderRequest(null, null, 1);
         // RunGetSearchOrderはページング処理を実行します。全オーダーを取得します。
         var searchOrder = _rakutenSearchOrder.RunGetSearchOrder(searchOrderParameter, rakutenShop);
         
         // 注文情報が取得できない場合は処理を終了
         if (searchOrder.PaginationResponse?.TotalRecordsAmount.GetValueOrDefault() == 0
            ||  searchOrder.OrderNumberList?.Any() != true)
            return (new List<string>(), new RakutenGetOrderResponse());

         var orderNumbers = searchOrder.OrderNumberList;
         var initialGetOrder = new RakutenGetOrderResponse();

         using (var transaction = _dbContext.Database.BeginTransaction())
         {
            try
            {
               // Remove 処理
               RemoveDailyOrderNews(mallShop);
               _dbContext.SaveChanges(); // SaveChangesでメモリ解放

               // 注文明細取得：GetOrder 実行。Max 100件/Request(1ページ)
               for (int page = 1; page <= searchOrder.PaginationResponse!.TotalPages; page++)
               {
                  // GetOrder リクエストパラメータ
                  var getOrderParameter = RakutenGetOrderRequestFactory.LatestVersionRequest( 
                     searchOrder.OrderNumberList.Skip((page - 1) * 100).Take(100).ToList());

                  // GetOrder 実行。Max 100件/Request(1ページ)
                  var getOrder = _rakutenGetOrder.GetOrder(getOrderParameter, rakutenShop);
                  if (page == 1) initialGetOrder = getOrder;

                  // マッピング処理（ HTTPResponseModel -> DB ）
                  var dailyOrderNews = DailyOrderNewsMapper.RakutenToDailyOrderNews(
                        getOrder, rakutenShop, _dbContext.Skuconversions);

                  // DailyOrderNews更新処理
                  UpdateDailyOrderNews(dailyOrderNews);
                  _dbContext.SaveChanges(); // SaveChangesでメモリ解放
               }
               transaction.Commit();
            }
            catch (Exception ex)
            {
               transaction.Rollback();
               throw new Exception("FetchDailyOrderFromRakutenでエラーが発生しました。", ex);
            }
            return (orderNumbers, initialGetOrder);
         } 
      }

      /// <summary>
      /// Yahoo注文情報を取得しDailyOrderNewsに保存する。
      /// </summary>
      /// 戻り値：(注文番号リスト, 注文詳細情報)→デバッグ用
      /// 　注文番号リストは全件を戻り値へセットする。
      /// ページング処理について
      /// 　Yahoo：GetOrderSearchは、2,000件/リクエストなので十分。ページング不要
      /// 　Yahoo：GetOrderInfoは引数の全注文番号を取得する。日次の取得件数（Max200件以下）であればページング不要。
      public (List<DailyOrderNewsYahoo>?, List<DailyOrderNews>?) FetchDailyOrderFromYahoo(YahooShop yahooShop)
      {
         // YahooOrderSearch実行：注文一覧取得。Max 2,000件の注文リストを取得
         var yahooOrderListResult = GetYahooOrderList(yahooShop);
         var orderIds = yahooOrderListResult.Search.OrderInfo
             .Select(x => x.Fields["OrderId"].ToString()).ToList();

         // YahooOrderInfo実行：注文詳細情報取得（引数orderIdsの詳細情報を取得：Max 2,000件）
         var orderInfo = GetYahooOrderInfo(orderIds, yahooShop);

         // マッピング処理 - DailyOrderNewsYahoo -> DailyOrderNews
         var dailyOrderNews = orderInfo == null ? null : 
            DailyOrderNewsMapper.YahooToDailyOrderNews (
               orderInfo, yahooShop.ToString(), _dbContext.Skuconversions);

         using (var transaction = _dbContext.Database.BeginTransaction())
         {
            try
            {
               var mallShop = Enum.GetValues(typeof(MallShop)).Cast<MallShop>()
                  .FirstOrDefault(m => m.ToString() == yahooShop.ToString());
               // Remove 処理
               RemoveDailyOrderNews(mallShop);
               _dbContext.SaveChanges(); // SaveChangesでメモリ解放
               // DailyOrderNews更新処理
               UpdateDailyOrderNews(dailyOrderNews);
               _dbContext.SaveChanges(); // SaveChangesでメモリ解放
               transaction.Commit();
            }
            catch (Exception ex)
            {
               transaction.Rollback();
               throw new Exception("FetchDailyOrderFromYahooでエラーが発生しました。", ex);
            }
         }
         return (orderInfo, dailyOrderNews);
      }

      // DailyOrderNewsのMallShopデータを削除
      private void RemoveDailyOrderNews(MallShop mallShop)
      {
         // DailyOrderNewsのデータを削除。
         var targetOrderNews = _dbContext.DailyOrderNews
            .Where(x => x.ShopCode == mallShop.ToString()).ToList();
         if (targetOrderNews.Any())
            _dbContext.DailyOrderNews.RemoveRange(targetOrderNews);
      }

      // DailyOrderNews更新処理
      private void UpdateDailyOrderNews(List<DailyOrderNews>? dailyOrderNews)
      {
         if (dailyOrderNews == null) return;
         _dbContext.DailyOrderNews.AddRange(dailyOrderNews);
      }

      /// <summary>
      /// Yahoo注文一覧を取得（本番メソッド）
      /// </summary>
      public YahooOrderListResult GetYahooOrderList(YahooShop yahooShop)
      {
         var (_, yahooOrderListResult)
            = GetYahooOrderListWithResponse(yahooShop);
         return yahooOrderListResult;
      }

      /// <summary>
      /// Yahoo注文一覧を取得。HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (HttpResponseMessage, YahooOrderListResult)
         GetYahooOrderListWithResponse(YahooShop yahooShop)
      {
         // APIリクエスト作成
         var sellerId = ssAppDBHelper.GetShopToken(_dbContext, yahooShop.ToString()).SellerId;
         var outputFields = string.Join(",", YahooOrderListRequestFactory.OutputFieldsDefault);
         var yahooOrderListRequest = YahooOrderListRequestFactory.NewOrderRequest(null, null, 1, outputFields, sellerId);
         // HTTP API実行
         var (httpResponses, yahooOrderListResult) = _yahooOrderList.GetOrderSearchWithResponse(yahooOrderListRequest, yahooShop);
         // DailyOrderNewsインターフェースモデルクラスへマッピング
         return (httpResponses, yahooOrderListResult);
      }

      /// <summary>
      /// Yahoo注文情報を取得（本番メソッド）
      /// </summary>
      public List<DailyOrderNewsYahoo>? GetYahooOrderInfo(List<string>? orderIds, YahooShop yahooShop)
      {
         // HTTPレスポンスを無視し、パース結果のみ返す
         var (_, dailyOrderNews)
            = GetYahooOrderInfoWithResponse(orderIds, yahooShop);
         return dailyOrderNews;
      }

      /// <summary>
      /// Yahoo注文情報を取得。HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (List<HttpResponseMessage>?, List<DailyOrderNewsYahoo>?) 
         GetYahooOrderInfoWithResponse(List<string>? orderIds, YahooShop yahooShop)
      {
         if (orderIds == null) return (null, null);
         // APIリクエスト作成
         var outputFields = DailyOrderNewsModelHelper.GetYahooOrderInfoFields();
         
         // GetOrderInfo 実行
         var (httpResponses, orderInfos) 
            = _yahooOrderInfo.GetOrderInfoWithResponse(orderIds, outputFields, yahooShop);

         // マッピング処理 - Yahoo注文明細 (HttpResponseModel) -> DailyOrderNewsYahoo (interface Model)
         var dailyOrderNews = DailyOrderNewsMapper.YahooOrderInfo(orderInfos, yahooShop);

         return (httpResponses, dailyOrderNews);
      }
   }
}
