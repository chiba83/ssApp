#pragma warning disable CS8620, CS8602
using ssAppModels.EFModels;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppServices.Api.Yahoo;
using ssAppServices.Api.Rakuten;
using Azure;

// 【 処理概要 】
// 各モールショップの新規注文（出荷対象注文）を取得し、DailyOrderNewsに保存する。
// 【 前提 】
// API（注文番号リスト取得）のページング処理について
// 　・楽天：呼出しメソッドでページング処理が実装されているため全件取得される。
// 　・Yahoo：GetOrderSearchは、2,000件/リクエストなので十分。
// API（注文詳細情報の取得）のページング処理について
// 　・楽天：100件/リクエストなので、ページング処理を実装する。
// 　・Yahoo：GetOrderInfoは全ての注文番号を取得する。日次の取得件数（Max200件想定）であればページング処理実装は不要。

namespace ssAppServices.Apps;

public class SetDailyOrderNews
{
   private readonly ssAppDBContext _dbContext;
   private readonly YahooApiExecute _YahooApiExecute;
   private readonly RakutenApiExecute _rakutenApiExecute;

   public SetDailyOrderNews(
      ssAppDBContext dbContext,
      YahooOrderList yahooOrderList, YahooOrderInfo yahooOrderInfo,
      YahooApiExecute yahooApiExecute,
      RakutenApiExecute rakutenApiExecute)
   {
      _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
      _YahooApiExecute = yahooApiExecute ?? throw new ArgumentNullException(nameof(yahooApiExecute));
      _rakutenApiExecute = rakutenApiExecute ?? throw new ArgumentNullException(nameof(rakutenApiExecute));
   }

   /// <summary>
   /// Rakuten注文情報を取得しDailyOrderNewsに保存する。
   /// 注文明細（GetOrder）Max 100件/リクエストなので、ページング処理を実装する。
   /// </summary>
   /// <returns>
   /// 注文番号リスト：テスト用。
   /// 注文詳細情報：テスト用。
   /// </returns>
   public (List<string>?, RakutenGetOrderResponse?) FetchDailyOrderFromRakuten(RakutenShop rakutenShop)
   {
      // リクエストに対する全オーダー明細を取得します。（上限2000件）
      var getOrderResponse = _rakutenApiExecute.GetNewOrders(null, null, rakutenShop);

      // マッピング処理（ HTTPResponseModel -> DailyOrderNews ）
      var dailyOrderNews = getOrderResponse == null ? null :
         DailyOrderNewsMapper.RakutenToDailyOrderNews(
            getOrderResponse, rakutenShop, _dbContext);

      // 梱包情報をセット
      var mallShop = MallShopConverter.ToMallShop(rakutenShop);
      dailyOrderNews = DailyOrderNewsMapper.SetPackingColumns(dailyOrderNews, mallShop.ToString(), true, _dbContext);

      // テーブル更新処理
      UpdateDailyOrderNews(dailyOrderNews, mallShop);

      var orderNumbers = getOrderResponse.OrderModelList?.Select(x => x.OrderNumber).ToList();
      return (orderNumbers, getOrderResponse);
   }

   /// <summary>
   /// Yahoo注文情報を取得しDailyOrderNewsに保存する。
   /// ページング処理について
   /// GetOrderSearchは、2,000件/リクエストなので十分。ページング不要
   /// GetOrderInfoは引数の全注文番号を取得する。日次の取得件数（Max200件以下）であればページング不要。
   /// </summary>
   /// <returns>
   /// 注文番号リスト：全件を戻り値へセットする。
   /// 注文詳細情報
   /// </returns>
   public (List<DailyOrderNewsYahoo>?, List<DailyOrderNews>?) FetchDailyOrderFromYahoo(YahooShop yahooShop)
   {
      // リクエストに対する全オーダー明細を取得します。（上限2000件）
      var getOrderResponse = _YahooApiExecute.GetNewOrders(null, null, AppModelHelpers.GetDailyOrderNewsFields(), yahooShop);

      // マッピング処理 1 - HttpResponseModel -> interface Model
      var orderInfo = DailyOrderNewsMapper.YahooOrderInfo(getOrderResponse, yahooShop);
      // マッピング処理 2 - interface Model -> DailyOrderNews
      var dailyOrderNews = orderInfo == null ? null : 
         DailyOrderNewsMapper.YahooToDailyOrderNews (
            orderInfo, yahooShop.ToString(), _dbContext);

      // 梱包情報をセット
      var mallShop = MallShopConverter.ToMallShop(yahooShop);
      dailyOrderNews = DailyOrderNewsMapper.SetPackingColumns(dailyOrderNews, mallShop.ToString(), true, _dbContext);

      UpdateDailyOrderNews(dailyOrderNews, mallShop);
      return (orderInfo, dailyOrderNews);
   }

   // DailyOrderNews更新処理
   private void UpdateDailyOrderNews(List<DailyOrderNews>? dailyOrderNews, MallShop mallShop)
   {
      using (var transaction = _dbContext.Database.BeginTransaction())
      {
         try
         {
            // Remove 処理
            var targetOrderNews = _dbContext.DailyOrderNews
               .Where(x => x.ShopCode == mallShop.ToString()).ToList();
            if (targetOrderNews.Any())
            {
               _dbContext.DailyOrderNews.RemoveRange(targetOrderNews);
               _dbContext.SaveChanges(); // SaveChangesでメモリ解放
            }
            // 更新処理
            if (dailyOrderNews == null) return;
            _dbContext.DailyOrderNews.AddRange(dailyOrderNews);
            _dbContext.SaveChanges(); // SaveChangesでメモリ解放

            transaction.Commit();
         }
         catch (Exception ex)
         {
            transaction.Rollback();
            throw new Exception("FetchDailyOrderFromYahooでエラーが発生しました。", ex);
         }
      }
   }
}
