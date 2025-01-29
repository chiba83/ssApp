#pragma warning disable  CS8620
using Microsoft.EntityFrameworkCore;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using System.Text.RegularExpressions;

namespace ssAppModels.AppModels;

public static class DailyOrderNewsMapper
{
   public static List<DailyOrderNews> SetPackingColumns(List<DailyOrderNews>? dailyOrderNews, string shopCode, ssAppDBContext _dbContext)
   {
      if (dailyOrderNews?.Any() != true) return new List<DailyOrderNews>();

      // PackingIdごとにグループ化して処理
      return dailyOrderNews
      .GroupBy(
         s => string.Join(" ", s.ShipZip, s.ShipPrefecture, s.ShipCity, s.ShipAddress1, s.ShipAddress2, s.ShipName),
         (key, group) => new
         {
            Key = key,
            Items = group.OrderBy(x => x.OrderId).ToList()
         })
      .SelectMany((group, groupIndex) =>
      {
         DateTime lastOrderDate = group.Items.Max(x => x.OrderDate);
         string packingId = $"{shopCode}-{(groupIndex + 1).ToString("D4")}";
         int distinctOrderIdCount = group.Items.Select(x => x.OrderId).Distinct().Count();
         int packingLineTotal = group.Items.Count;
         string packingSort = string.Join(",", group.Items.OrderBy(x => x.Skucode).Select(x => $"{x.Skucode}*{x.OrderQty}"));
         var(deliveryCode, PackingQTY) = GetDeliveryCode(group.Items, shopCode, _dbContext);

         return group.Items.Select((item, itemIndex) =>
         {
            item.LastOrderDate = lastOrderDate;
            item.PackingId = packingId;
            item.PackingOrderIdCount = distinctOrderIdCount;
            item.PackingLineId = itemIndex + 1;
            item.PackingLineTotal = packingLineTotal;
            item.PackingSort = packingSort;
            item.DeliveryCode = deliveryCode;
            item.PackingQty = PackingQTY;
            return item;
         });
      }).ToList();
   }

   /// <summary>
   /// 出荷方法と梱包数（箱数）を取得する。（注文商品を配送先へ、どの配送方法で何箱出荷するか決定する）
   ///
   /// # 業務ルール
   /// １つのショップ配送先グループに対し配送方法は1つです。1つの配送方法で複数の箱（N梱包）を出荷します。
   ///
   /// # 処理概要
   /// 梱包する商品とその数量によって条件に合致する配送方法コードと梱包数を取得する。
   /// 配送条件の判定は以下のテーブルを参照する。
   /// _dbContext.ShippingGroups：配送条件グループマスタ
   /// _dbContext.ShippingGroupMembers：配送条件グループ構成マスタ（構成商品リスト）
   /// _dbContext.ShopToShippingGroup：ショップと配送条件グループの紐付け
   /// _dbContext.ShippingConditions：配送条件グループの条件リスト（注文数による配送方法と1梱包あたりの最大商品数）
   ///
   /// # 処理詳細（条件適用仕様）
   /// 1. ショップコードに紐づく配送条件グループ（ShopToShippingGroup.ShopCode）を適用する。
   /// 2. 条件グループ構成商品は、"product"または"sku"のいずれか（ShippingGroupMembers.MemberType）で判定する。
   /// 3. 2の構成商品郡（MemberTypeによりProductCode or SKUCode）の注文数（OrderQty）合計が配送条件グループの条件リスト（ShippingConditions.MinThresholdQuantity）に合致する場合、配送方法コードと梱包数（ShippingConditions.DeliveryCode, MaxPackageCapacity）を取得する。
   /// 4. 2の構成商品郡の注文数（OrderQty）合計と3の梱包数量（MaxPackageCapacity）から梱包数（箱数）を計算する。
   /// 5. 注文商品が多岐になり、複数の配送条件グループに合致する場合、複数の配送コード（ShippingConditions.DeliveryCode）の中から優先順位（Delivery.DeliveryPriority）の高いもの（昇順）を１つ適用し、その梱包数を採用する。
   /// 6. 配送条件グループに合致しない場合、戻り値の配送方法コードはstring.Empty、梱包数は1を返す。
   /// </summary>
   /// <param name="packingGroupItems">配送先グループに対する梱包商品リスト</param>
   /// <param name="shopCode">ショップコード</param>
   /// <param name="_dbContext">DBコンテキスト</param>
   /// <returns>配送方法コード、梱包数</returns>
   public static (string deliveryCode, int packingQty) GetDeliveryCode(List<DailyOrderNews> packingGroupItems, string shopCode, ssAppDBContext _dbContext)
   {
      // 初期値設定
      string deliveryCode = string.Empty;
      int packingQty = 1;

      // ショップに紐づく配送条件グループを取得
      var shippingGroupIds = _dbContext.ShopToShippingGroups
         .Where(s => s.ShopCode == shopCode).Select(s => s.ShippingGroupId).ToList();

      // 配送条件グループごとに注文数量を集計
      var groupQuantities = packingGroupItems
         .SelectMany(item => _dbContext.ShippingGroupMembers
            .Where(member => shippingGroupIds.Contains(member.ShippingGroupId) &&
               ((member.MemberType == "product" && member.MemberId == item.ProductCode) ||
               (member.MemberType == "sku" && member.MemberId == item.Skucode)))
            .Select(member => new { member.ShippingGroupId, item.OrderQty }))
         .GroupBy(x => x.ShippingGroupId)
         .Select(group => new
         {
            ShippingGroupID = group.Key,
            TotalQty = group.Sum(x => x.OrderQty)
         });

      // 適用可能な配送条件を判定
      var applicableConditions = groupQuantities
         .SelectMany(gq => _dbContext.ShippingConditions
            .Where(sc => sc.ShippingGroupId == gq.ShippingGroupID && gq.TotalQty >= sc.MinThresholdQuantity)
            .Select(sc => new
            {
               sc.DeliveryCode,
               gq.TotalQty,
               sc.MaxPackageCapacity,
               Priority = _dbContext.Deliveries.Single(d => d.DeliveryCode == sc.DeliveryCode).DeliveryPriority
            })
         ).OrderBy(x => x.Priority).FirstOrDefault();

      if (applicableConditions != null)
      {
         deliveryCode = applicableConditions.DeliveryCode;
         packingQty = (int)Math.Ceiling((double)applicableConditions.TotalQty / applicableConditions.MaxPackageCapacity);
      }

      return (deliveryCode, packingQty);
   }

   // マッピング処理（ HTTPResponseModel -> DB Model ）
   // Rakuten注文明細 (RakutenGetOrderResponse) -> DailyOrderNews
   public static List<DailyOrderNews> RakutenToDailyOrderNews(
      RakutenGetOrderResponse rakutenGetOrderResponse,
      RakutenShop rakutenShop,
      DbSet<Skuconversion> skuConversion)
   {
      if (rakutenGetOrderResponse.OrderModelList?.Any() != true) return new List<DailyOrderNews>();

      return rakutenGetOrderResponse.OrderModelList.SelectMany(order => order.PackageModelList
         .SelectMany(package => package.ItemModelList
            .Select((item, index) =>
            {
               var sender = package.SenderModel;
               var couponTotal = order.CouponModelList?
                     .SingleOrDefault(c => c.ItemDetailId == item.ItemDetailId)?.CouponTotalPrice ?? 0;
               var lineTotal = package.ItemModelList.Count;
               var itemNumber = item.ItemNumber ?? item.ManageNumber;
               var skuModel = item.SkuModelList.FirstOrDefault();
               var merchantDefinedSkuId = item.SkuModelList
                     .FirstOrDefault()?.MerchantDefinedSkuId ?? string.Empty;
               var (productCode, skuCode) = GetSKUCode(itemNumber, merchantDefinedSkuId,
                     string.Empty, rakutenShop.ToString(), skuConversion);

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
                  OrderLineTotal = lineTotal,
                  ProductCode = productCode,
                  Skucode = skuCode,
                  OrderQty = item.Units,
                  ConsumptionTaxRate = (decimal)item.TaxRate,
                  OriginalPrice = item.PriceTaxIncl * item.Units,
                  CouponDiscount = couponTotal,
                  OrderDetailTotal = item.PriceTaxIncl * item.Units - couponTotal,
                  CustomField1 = package.BasketId,
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

   // マッピング処理（ I/F Model -> DB Model ）
   // DailyOrderNewsYahoo -> DailyOrderNews
   public static List<DailyOrderNews> YahooToDailyOrderNews(
      List<DailyOrderNewsYahoo> source, 
      string yahooShop, 
      DbSet<Skuconversion> skuConversion)
   {
      if (source?.Any() != true) return new List<DailyOrderNews>();

      // 1. 同一OrderId内のOrderLineIdの最大値を計算
      var orderLineTotals = source
          .GroupBy(item => item.OrderId)
          .ToDictionary(g => g.Key, g => g.Max(item => item.LineId));

      // 2. マッピング処理
      var result = new List<DailyOrderNews>();
      foreach (var item in source)
      {
         var (productCode, skuCode) = GetSKUCode(item.ItemId, item.SubCode,
            item.ItemOption, yahooShop, skuConversion);
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
            OrderLineTotal = orderLineTotals[item.OrderId],      // 注文行番号合計
            ProductCode = productCode,                           // 商品コード
            Skucode = skuCode,                                   // SKUコード
            OrderQty = item.Quantity,                            // 数量
            ConsumptionTaxRate = item.ItemTaxRatio / 100m,       // 消費税率（intからdecimalへ変換）
            OriginalPrice = (item.UnitPrice + item.CouponDiscount)
               * item.Quantity,                                  // オリジナル価格
            CouponDiscount = item.CouponDiscount * item.Quantity,// クーポン値引き
            OrderDetailTotal = (item.UnitPrice + item.CouponDiscount) * item.Quantity
               - item.CouponDiscount * item.Quantity,            // 注文明細合計
         };
         result.Add(mappedItem);
      }

      return result;
   }

   // 商品番号の共通化変換
   private static (string, string) GetSKUCode(string itemId, string? itemSub,
      string itemOption, string shopCode, DbSet<Skuconversion> skuConversion)
   {
      //var orderId = item.ItemId + item.SubCode;
      var convertCode = skuConversion
         .Where(x => x.ShopCode == shopCode &&
            x.ShopProductCode == itemId && x.ShopSkucode == itemSub)
         .Select(x => new { x.ProductCode, x.Skucode }).FirstOrDefault();

      if (convertCode != null)
         return (convertCode.ProductCode, convertCode.Skucode);

      var sku = itemId + itemSub;

      // Rakuten-ENZO SKUコンバート
      if (shopCode == RakutenShop.Rakuten_ENZO.ToString())
         return (itemId, Regex.Replace(sku, @"(\w{6,})\1", "$1"));

      // Yahoo-Yours SKUコンバート
      if (shopCode == YahooShop.Yahoo_Yours.ToString())
         return (itemId, Regex.Replace(sku, @"(\w{6,})\1", "$1"));

      // Yahoo-LARAL SKUコンバート
      if (shopCode == YahooShop.Yahoo_LARAL.ToString())
      {
         sku = Regex.Replace(sku, @"(\w{8,})\1", "$1");
         var size = Regex.Match(itemOption, @"(\d+)(?=cm)");
         return (itemId, size.Success ? $"{sku}-{size.Value}" : sku);
      }

      return (string.Empty, string.Empty);
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