using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppModels.AppModels
{
   public static class DailyOrderNewsMapper
   {
      // マッピング処理（ HTTPResponseModel -> I/F Model ）
      // Yahoo注文一覧（YahooOrderListResult） -> DailyOrderNewsYahooSearch
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
               .Where(field => validFields.Contains(field.Key)).ToList()
               .ForEach(field =>
                  dailyOrderNews = SetProperty(dailyOrderNews, field.Key, field.Value));

            return dailyOrderNews;
         }).ToList();
      }

      // マッピング処理（ HTTPResponseModel -> I/F Model ）
      // Yahoo注文明細 (YahooOrderInfoResponse) -> DailyOrderNewsYahoo (interface Model)
      public static List<DailyOrderNewsYahoo> YahooOrderInfo(List<YahooOrderInfoResponse> responses, YahooShop yahooShop)
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

            return items.Select((item) =>
            {
               var dailyOrderNews = new DailyOrderNewsYahoo();

               // FlatFields のマッピング
               flatFields
                  .Where(field => validFields.Contains(field.Key)).ToList() // 有効なプロパティのみ
                  .ForEach(field =>
                     dailyOrderNews = SetProperty(dailyOrderNews, field.Key, field.Value));

               // Item フィールドのマッピング
               item.Item?
                  .Where(field => validFields.Contains(field.Key)).ToList()
                  .ForEach(field =>
                     dailyOrderNews = SetProperty(dailyOrderNews, field.Key, field.Value));

               // ItemOptions フィールドのマッピング
               if (item.ItemOptions != null && validFields.Contains("ItemOption"))
                  dailyOrderNews = SetProperty(dailyOrderNews, "ItemOption", GetItemOpetion(item.ItemOptions));

               // Inscription フィールドのマッピング
               if (item.Inscription != null && validFields.Contains("Inscription"))
                  dailyOrderNews = SetProperty(dailyOrderNews, "Inscription", GetItemInscription(item.Inscription));

               return dailyOrderNews;
            });
         }).ToList();
      }

      // マッピング処理（ I/F Model -> DB ）
      // DailyOrderNewsYahoo -> DailyOrderNews
      public static List<DailyOrderNews> YahooToDailyOrderNews(List<DailyOrderNewsYahoo> source, Dictionary<string, String> sellerIds)
      {
         if (source == null || !source.Any()) throw new ArgumentNullException(nameof(source));

         // 最新のOrderTimeをキーグループ化して取得
         var latestOrderDates = source
            .GroupBy(o => new
            {
               o.SellerId,
               o.ShipZipCode,
               o.ShipPrefecture,
               o.ShipCity,
               o.ShipAddress1,
               o.ShipAddress2,
               ShipName = $"{o.ShipLastName} {o.ShipFirstName}" // 氏名を結合
            }).ToDictionary(
               g => g.Key,
               g => g.Max(o => o.OrderTime) // グループ内で最新のOrderTimeを取得
            );

         // マッピング
         var result = new List<DailyOrderNews>();
         foreach (var item in source)
         {
            var mappedItem = new DailyOrderNews
            {
               ShopCode = sellerIds[item.SellerId],               // モール・ショップID
               ShipZip = item.ShipZipCode,                        // 出荷先郵便番号
               ShipPrefecture = item.ShipPrefecture,              // 出荷先都道府県
               ShipCity = item.ShipCity,                          // 出荷先市区町村
               ShipAddress1 = item.ShipAddress1 ?? string.Empty,  // 出荷先住所1
               ShipAddress2 = item.ShipAddress2 ?? string.Empty,  // 出荷先住所2
               ShipName = $"{item.ShipLastName} {item.ShipFirstName}", // 氏名結合
               ShipTel = item.ShipPhoneNumber ?? string.Empty,    // 電話番号
               ShipEmail = item.BillMailAddress ?? string.Empty,  // メールアドレス
               OrderId = item.OrderId,                            // 注文ID
               OrderDate = item.OrderTime,                        // 注文日時
               OrderLineId = item.LineId,                         // 注文行番号
               Skucode = item.ItemId + item.SubCode,              // SKUコード
               OrderQty = item.Quantity,                          // 数量
               ConsumptionTaxRate = item.ItemTaxRatio / 100m,     // 消費税率（intからdecimalへ変換）
               OriginalPrice = item.UnitPrice,                    // オリジナル価格
               CouponDiscount = item.CouponDiscount,              // クーポン値引き
               OrderDetailTotal = item.UnitPrice * item.Quantity 
                  - item.CouponDiscount,                          // 注文明細合計
            };
            // キーに基づいてLastOrderDateをセット（最新注文日時）
            var key = new
            {
               item.SellerId, 
               item.ShipZipCode, 
               item.ShipPrefecture,
               item.ShipCity, 
               item.ShipAddress1, 
               item.ShipAddress2,
               ShipName = $"{item.ShipLastName} {item.ShipFirstName}"
            };
            if (latestOrderDates.TryGetValue(key, out var lastOrderDate))
               mappedItem.LastOrderDate = lastOrderDate;

            result.Add(mappedItem);
         }

         return result;
      }

      // Class のプロパティを設定する（動的Property対応）
      private static T SetProperty<T>(T target, string propertyName, object value) where T : class
      {
         if (target == null)
            throw new ArgumentNullException(nameof(target));

         var property = typeof(T).GetProperty(propertyName);
         if (property == null)
            throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T)}'.");

         // プロパティに値を設定
         property.SetValue(target, value);

         return target;
      }

      // YahooOrderInfoItemOptions の文字列を取得
      private static string GetItemOpetion(List<YahooOrderInfoItemOption> itemOptions)
      {
         return string.Join(";", itemOptions.Select(option =>
            $"{option.Index},{option.Name},{option.Value},{option.Price}"));
      }

      // YahooOrderInfoInscription の文字列を取得
      private static string GetItemInscription(YahooOrderInfoInscription info)
      {
         return $"{info.Index},{info.Name},{info.Value}";
      }
   }
}