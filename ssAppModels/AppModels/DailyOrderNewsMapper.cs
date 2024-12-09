using ssAppModels.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppModels.AppModels
{
   public static class DailyOrderNewsMapper
   {
      public static List<DailyOrderNewsYahooSearch> YahooOrderList(YahooOrderListResult result)
      {
         if (result?.Search?.OrderInfo == null)
            return new List<DailyOrderNewsYahooSearch>();
         // 対象プロパティ名のキャッシュ
         var validFields = DailyOrderNewsModelHelper.YahooOrderSearchFields();

         return result.Search.OrderInfo.Select(orderInfo =>
         {
            var dailyOrderNews = new DailyOrderNewsYahooSearch();

            orderInfo.Fields
                .Where(field => validFields.Contains(field.Key)) // 有効なフィールドのみ対象
                .ToList()
                .ForEach(field =>
                {
                   var property = dailyOrderNews.GetType().GetProperty(field.Key);
                   property?.SetValue(dailyOrderNews, field.Value);
                });

            return dailyOrderNews;
         }).ToList();
      }

      public static List<DailyOrderNewsYahoo> YahooOrderInfo(List<YahooOrderInfoResponse> responses)
      {
         if (responses == null || !responses.Any())
            return new List<DailyOrderNewsYahoo>();
         // 対象プロパティ名の取得
         var validFields = DailyOrderNewsModelHelper.GetYahooOrderInfoFields();

         return responses.SelectMany(response =>
         {
            if (response?.ResultSet?.Result?.OrderInfo == null)
               return Enumerable.Empty<DailyOrderNewsYahoo>();

            var orderInfo = response.ResultSet.Result.OrderInfo;

            // Order, Pay, Ship, Seller, Buyer, Detail のフィールドをまとめる
            var flatFields = new[] { orderInfo.Order, orderInfo.Pay, orderInfo.Ship, orderInfo.Seller, orderInfo.Buyer, orderInfo.Detail }
               .Where(dict => dict != null).SelectMany(dict => dict!)
               .ToDictionary(pair => pair.Key, pair => pair.Value);

            // 1:N 関係の Items のマッピング
            var items = orderInfo.Items ?? new List<YahooOrderInfoItem>();

            return items.Select(item =>
            {
               var dailyOrderNews = new DailyOrderNewsYahoo();

               // FlatFields のマッピング
               flatFields
                  .Where(field => validFields.Contains(field.Key)).ToList() // 有効なプロパティのみ
                  .ForEach(field =>
                  {
                     var property = dailyOrderNews.GetType().GetProperty(field.Key);
                     property?.SetValue(dailyOrderNews, field.Value);
                  });

               // Item フィールドのマッピング
               item.Item?
                  .Where(field => validFields.Contains(field.Key)).ToList()
                  .ForEach(field =>
                  {
                     var property = dailyOrderNews.GetType().GetProperty(field.Key);
                     property?.SetValue(dailyOrderNews, field.Value);
                  });

               return dailyOrderNews;
            });
         }).ToList();
      }
   }
}
