#pragma warning disable  CS8620
using Microsoft.EntityFrameworkCore;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using System.Text.RegularExpressions;

namespace ssAppModels.AppModels
{
   public static class DailyOrderNewsMapper
   {
      // マッピング処理（ HTTPResponseModel -> DB ）
      // Rakuten注文明細 (RakutenGetOrderResponse) -> DailyOrderNews
      public static List<DailyOrderNews> RakutenToDailyOrderNews(
         RakutenGetOrderResponse rakutenGetOrderResponse,
         RakutenShop rakutenShop,
         DbSet<Skuconversion> skuConversion)
      {
         if (rakutenGetOrderResponse.OrderModelList?.Any() != true)
            return new List<DailyOrderNews>();

         var latestOrderDates = rakutenGetOrderResponse.OrderModelList.SelectMany(o => o.PackageModelList.Select(x => x.SenderModel), 
            (o, s) => new { 
               key = string.Join(" ", s.ZipCode1, s.ZipCode2, s.Prefecture, s.City, s.SubAddress, s.FamilyName, s.FirstName),
               orderDatetime = o.OrderDatetime})
            .GroupBy(x => x.key)
            .ToDictionary(g => g.Key, g => g.Max(x => x.orderDatetime));

         return rakutenGetOrderResponse.OrderModelList.SelectMany(order => order.PackageModelList
            .SelectMany(package => package.ItemModelList
               .Select((item, index) =>
               {
                  var sender = package.SenderModel;
                  var couponTotal = order.CouponModelList?
                        .SingleOrDefault(c => c.ItemDetailId == item.ItemDetailId)?.CouponTotalPrice ?? 0;
                  var lineCount = package.ItemModelList.Count;
                  var itemNumber = item.ItemNumber ?? item.ManageNumber;
                  var merchantDefinedSkuId = item.SkuModelList.FirstOrDefault()?
                        .MerchantDefinedSkuId ?? string.Empty;
                  var skuCode = GetSKUCode(itemNumber, merchantDefinedSkuId,
                        string.Empty, rakutenShop.ToString(), skuConversion);
                  var key = string.Join(" ", sender.ZipCode1, sender.ZipCode2, sender.Prefecture, 
                        sender.City, sender.SubAddress, sender.FamilyName, sender.FirstName);

                  return new DailyOrderNews
                  {
                     ShopCode = rakutenShop.ToString(),
                     ShipZip = sender.ZipCode1 + sender.ZipCode2,
                     ShipPrefecture = sender.Prefecture,
                     ShipCity = sender.City,
                     ShipAddress1 = sender.SubAddress,
                     ShipName = $"{sender.FamilyName} {sender.FirstName}",
                     ShipTel = $"{sender.PhoneNumber1}{sender.PhoneNumber2}{sender.PhoneNumber3}",
                     ShipEmail = string.Empty,
                     OrderId = order.OrderNumber,
                     OrderDate = order.OrderDatetime,
                     OrderLineId = index + 1,
                     Skucode = skuCode,
                     OrderQty = item.Units,
                     ConsumptionTaxRate = (decimal)item.TaxRate,
                     OriginalPrice = item.PriceTaxIncl * item.Units,
                     CouponDiscount = couponTotal,
                     OrderDetailTotal = item.PriceTaxIncl * item.Units - couponTotal,
                     LastOrderDate = latestOrderDates[key]
                  };
               })
            )
         ).ToList();
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
      public static List<DailyOrderNews> YahooToDailyOrderNews(
         List<DailyOrderNewsYahoo> source, 
         string yahooShop, 
         DbSet<Skuconversion> skuConversion)
      {
         if (source == null || !source.Any()) return new List<DailyOrderNews>();

         // 最新のOrderTimeをキーグループ化して取得
         var latestOrderDates = source
            .GroupBy(o => string.Join(" ", o.SellerId, o.ShipZipCode, o.ShipPrefecture, o.ShipCity, o.ShipAddress1, o.ShipAddress2, o.ShipLastName, o.ShipFirstName))
            .ToDictionary(g => g.Key, g => g.Max(o => o.OrderTime));

         // マッピング
         var result = new List<DailyOrderNews>();
         foreach (var item in source)
         {
            var mappedItem = new DailyOrderNews
            {
               ShopCode = yahooShop,                                // モール・ショップID
               ShipZip = item.ShipZipCode.Replace("-", ""),         // 出荷先郵便番号
               ShipPrefecture = item.ShipPrefecture,                // 出荷先都道府県
               ShipCity = item.ShipCity,                            // 出荷先市区町村
               ShipAddress1 = item.ShipAddress1 ?? string.Empty,    // 出荷先住所1
               ShipAddress2 = item.ShipAddress2 ?? string.Empty,    // 出荷先住所2
               ShipName = $"{item.ShipLastName} {item.ShipFirstName}", // 氏名結合
               ShipTel = item.ShipPhoneNumber ?? string.Empty,      // 電話番号
               ShipEmail = item.BillMailAddress ?? string.Empty,    // メールアドレス
               OrderId = item.OrderId,                              // 注文ID
               OrderDate = item.OrderTime,                          // 注文日時
               OrderLineId = item.LineId,                           // 注文行番号
               Skucode = GetSKUCode(item.ItemId, item.SubCode, 
                  item.ItemOption, yahooShop, skuConversion),       // SKUコード
               OrderQty = item.Quantity,                            // 数量
               ConsumptionTaxRate = item.ItemTaxRatio / 100m,       // 消費税率（intからdecimalへ変換）
               OriginalPrice = (item.UnitPrice + item.CouponDiscount)
                  * item.Quantity,                                  // オリジナル価格
               CouponDiscount = item.CouponDiscount * item.Quantity,// クーポン値引き
               OrderDetailTotal = (item.UnitPrice + item.CouponDiscount) * item.Quantity
                  - item.CouponDiscount * item.Quantity,            // 注文明細合計
            };
            // キーに基づいてLastOrderDateをセット（最新注文日時）
            var key = string.Join(" ", item.SellerId, item.ShipZipCode, item.ShipPrefecture, item.ShipCity, item.ShipAddress1, item.ShipAddress2, item.ShipLastName, item.ShipFirstName);
            if (latestOrderDates.TryGetValue(key, out var lastOrderDate))
               mappedItem.LastOrderDate = lastOrderDate;

            result.Add(mappedItem);
         }

         return result;
      }

      // 商品番号の共通化変換
      private static string GetSKUCode(string itemId, string itemSub,
         string itemOption, string shopCode, DbSet<Skuconversion> skuConversion)
      {
         //var orderId = item.ItemId + item.SubCode;
         var convertSKU = skuConversion
            .Where(x => x.ShopCode == shopCode 
               && x.ProductCode == itemId
               && x.ShopSkucode == itemSub)
            .Select(x => x.ProductCode + x.Skucode).FirstOrDefault();
         
         var sku = convertSKU ?? itemId + itemSub;

         // Rakuten-ENZO SKUコンバート
         if (shopCode == RakutenShop.Rakuten_ENZO.ToString())
            return Regex.Replace(sku, @"(\w{6,})\1", "$1");

         // Yahoo-Yours SKUコンバート
         if (shopCode == YahooShop.Yahoo_Yours.ToString())
            return Regex.Replace(sku, @"(\w{6,})\1", "$1");

         // Yahoo-LARAL SKUコンバート
         if (shopCode == YahooShop.Yahoo_LARAL.ToString())
         {
            sku = Regex.Replace(sku, @"(\w{8,})\1", "$1");
            var size = Regex.Match(itemOption, @"(\d+)(?=cm)");
            return size.Success ? $"{sku}-{size.Value}" : sku;
         }
         return string.Empty;
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