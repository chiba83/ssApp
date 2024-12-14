using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ssAppModels.EFModels;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppCommon.Extensions;
using ssAppServices.Api.Yahoo;
using Azure;

namespace ssAppServices.Apps
{
   public class SetDailyOrderNews
   {
      private readonly ssAppDBContext _dbContext;
      private readonly YahooOrderList _yahooOrderList;
      private readonly YahooOrderInfo _yahooOrderInfo;

      public SetDailyOrderNews(ssAppDBContext dbContext, YahooOrderList yahooOrderList, YahooOrderInfo yahooOrderInfo)
      {
         _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
         _yahooOrderList = yahooOrderList ?? throw new ArgumentNullException(nameof(yahooOrderList));
         _yahooOrderInfo = yahooOrderInfo ?? throw new ArgumentNullException(nameof(yahooOrderInfo));
      }

      public (List<DailyOrderNewsYahoo>, List<DailyOrderNews>) RunDailyOrderNewsWorkflow()
      {
         var dailyOrderNewsYahoo = new List<DailyOrderNewsYahoo>();
         foreach (YahooShop shop in Enum.GetValues(typeof(YahooShop)))
         {
            var orderList = GetYahooOrderList(shop);
            var orderInfo = GetYahooOrderInfo(orderList, shop);
            dailyOrderNewsYahoo.AddRange(orderInfo);
         }

         // マッピング処理 - DailyOrderNewsYahoo -> DailyOrderNews
         var yahooSellerIds = ssAppDBHelper.GetShopTokenSeller(_dbContext, Mall.Yahoo);
         var dailyOrderNews = DailyOrderNewsMapper.YahooToDailyOrderNews(dailyOrderNewsYahoo, yahooSellerIds);

         // DailyOrderNews更新処理
         UpdateDailyOrderNews(dailyOrderNews);

         return (dailyOrderNewsYahoo, dailyOrderNews);
      }

      // DailyOrderNews更新処理
      private void UpdateDailyOrderNews(List<DailyOrderNews> dailyOrderNews)
      {
         // DailyOrderNewsのデータを削除。DailyOrderNews.ShopCodeの先頭文字列が"Yahoo"を削除
         var targetOrderNews = _dbContext.DailyOrderNews.Where(x => x.ShopCode.StartsWith("Yahoo")).ToList();
         if (targetOrderNews.Any())
         {
            _dbContext.DailyOrderNews.RemoveRange(targetOrderNews);
            _dbContext.SaveChanges();
         }

         if (dailyOrderNews == null) return;
         // データベース更新処理
         _dbContext.DailyOrderNews.AddRange(dailyOrderNews);
         _dbContext.SaveChanges();
      }

      /// <summary>
      /// Yahoo注文一覧を取得（本番メソッド）
      /// </summary>
      public List<DailyOrderNewsYahooSearch> GetYahooOrderList(YahooShop yahooShop)
      {
         var (_, orderList)
            = GetYahooOrderListWithResponse(yahooShop);
         return orderList;
      }

      /// <summary>
      /// Yahoo注文一覧を取得。HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (HttpResponseMessage, List<DailyOrderNewsYahooSearch>) 
         GetYahooOrderListWithResponse(YahooShop yahooShop)
      {
         // APIリクエスト作成
         var searchCondition = YahooOrderListConditionFormat.DailyOrderNews;
         var outputFields = DailyOrderNewsModelHelper.YahooOrderSearchFields();
         // HTTP API実行
         var (httpResponses, orderList) = _yahooOrderList.GetOrderSearchWithResponse(searchCondition, outputFields, yahooShop);
         // DailyOrderNewsインターフェースモデルクラスへマッピング
         return (httpResponses, DailyOrderNewsMapper.YahooOrderList(orderList));
      }

      /// <summary>
      /// Yahoo注文情報を取得（本番メソッド）
      /// </summary>
      public List<DailyOrderNewsYahoo> GetYahooOrderInfo(List<DailyOrderNewsYahooSearch> orderList, YahooShop yahooShop)
      {
         // HTTPレスポンスを無視し、パース結果のみ返す
         var (_, dailyOrderNews)
            = GetYahooOrderInfoWithResponse(orderList, yahooShop);
         return dailyOrderNews;
      }

      /// <summary>
      /// Yahoo注文情報を取得。HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (List<HttpResponseMessage>, List<DailyOrderNewsYahoo>) 
         GetYahooOrderInfoWithResponse(List<DailyOrderNewsYahooSearch> orderList, YahooShop yahooShop)
      {
         // APIリクエスト作成
         var orderIds = orderList.Select(x => x.OrderId).ToList();
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
