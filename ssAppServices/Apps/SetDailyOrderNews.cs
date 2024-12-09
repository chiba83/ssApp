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

      public List<DailyOrderNewsYahooSearch> GetYahooOrderList() 
      {
         // APIリクエスト作成
         var searchCondition = YahooOrderListConditionFormat.DailyOrderNews;
         var outputFields = DailyOrderNewsModelHelper.YahooOrderSearchFields();
         // HTTP API実行
         var response = _yahooOrderList.GetOrderSearch(searchCondition, outputFields, YahooShop.Yahoo_Yours);
         // DailyOrderNewsインターフェースモデルクラスへマッピング
         return DailyOrderNewsMapper.YahooOrderList(response);
      }

      /// <summary>
      /// Yahoo注文情報を取得（本番メソッド）
      /// </summary>
      public List<DailyOrderNewsYahoo> GetYahooOrderInfo(List<DailyOrderNewsYahooSearch> orderList)
      {
         // HTTPレスポンスを無視し、パース結果のみ返す
         var (_, dailyOrderNews)
            = GetYahooOrderInfoWithResponse(orderList);
         return dailyOrderNews;
      }

      /// <summary>
      /// Yahoo注文情報を取得。HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (List<HttpResponseMessage> httpResponses, List<DailyOrderNewsYahoo>) 
         GetYahooOrderInfoWithResponse(List<DailyOrderNewsYahooSearch> orderList)
      {
         // APIリクエスト作成
         var orderIds = orderList.Select(x => x.OrderId).ToList();
         var outputFields = DailyOrderNewsModelHelper.GetYahooOrderInfoFields();
         // GetOrderInfo 実行
         var (httpResponses, orderInfos) 
            = _yahooOrderInfo.GetOrderInfoWithResponse(orderIds, outputFields, YahooShop.Yahoo_Yours);
         // DailyOrderNewsインターフェースモデルクラスへマッピング
         var dailyOrderNews = DailyOrderNewsMapper.YahooOrderInfo(orderInfos);
         return (httpResponses, dailyOrderNews);
      }
   }
}
