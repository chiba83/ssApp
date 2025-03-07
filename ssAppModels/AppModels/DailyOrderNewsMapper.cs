#pragma warning disable  CS8604
using Microsoft.EntityFrameworkCore;
using NormalizeJapaneseAddressesNET;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using System.Text.RegularExpressions;

namespace ssAppModels.AppModels;

public static class DailyOrderNewsMapper
{
   /// <summary>
   /// 配送伝票用プロパティ。置換マップ
   /// Key：検索文字列、Value：置換後の文字列
   /// 伝票の内容品文字数制限対応のため繰り返し出現する文字列を短く置換する。
   /// </summary>
   private static readonly Dictionary<string, string> ReplacementMap = new()
   {
      { "靴下S", "S" }, { "靴下L", "L" }, { "替えブラシ", "" }, { "USB-C", "" },
      { "ライト", "" }, { "ベネチアン", "" }, { "アズキ", "" }, { "スクリュー", "" },
   };
   /// <summary>
   /// 配送伝票用プロパティ。適用枠の記載文字数制限対応。
   /// 伝票の適用枠に注文IDを記載するが文字数制限数に対応するためショップ毎のIDプレフィックス文字数を保持する。
   /// </summary>
   private static readonly Dictionary<string, int> LabelItemCount = new()
   {
      { "003", 2 }, { "020", 1 }, { "092", 2 },
   };
   /// <summary>
   /// 配送伝票用プロパティ。内容品の記載枠数。
   /// 伝票の内容品枠数を配送会社毎に固定変数として保持する。
   /// </summary>
   private static readonly Dictionary<string, int> ShopOrderPrefixSize = new()
   {
      // Yahoo_LARAL ：axis-j-xxxxxxxx
      // Yahoo_Yours ：yours-ja-xxxxxxxx
      // Rakuten_ENZO：413247-20250205-xxxxxxxxxx
      { "Yahoo_LARAL", 7 }, { "Yahoo_Yours", 9 }, { "Rakuten_ENZO", 16 },
   };

   /// <summary>
   /// 梱包グループ（PackingGroup）の関連項目を設定する。
   /// </summary>
   /// <param name="dailyOrderNews"></param>
   /// <param name="shopCode"></param>
   /// <param name="normalizeAddresses"></param>
   /// <param name="_dbContext"></param>
   /// <returns></returns>
   public static List<DailyOrderNews> SetPackingColumns(
      List<DailyOrderNews>? dailyOrderNews, string shopCode, 
      bool normalizeAddresses, ssAppDBContext _dbContext)
   {
      if (dailyOrderNews?.Any() != true) return [];

      // PackingIdごとにグループ化して処理
      return dailyOrderNews
      .GroupBy(
         s => new { s.ShipZip, s.ShipPrefecture, s.ShipCity, s.ShipAddress1, s.ShipAddress2, s.ShipName },
         (key, group) => new { Key = key, Items = group.OrderBy(x => x.OrderId).ToList()})
      .SelectMany((group, groupIndex) =>
      {
         var items = group.Items;
         var originalAddress = $"{group.Key.ShipPrefecture}{group.Key.ShipCity}{group.Key.ShipAddress1}{group.Key.ShipAddress2}";
         var normalize = normalizeAddresses ? NormalizeJapaneseAddresses.Normalize(originalAddress).Result : null;
         var (addressNumber, building) = normalize?.level == 3 ? SplitAddress(normalize.addr) : ("","");
         var lastOrderDate = items.Max(x => x.OrderDate);
         var packingId = $"{shopCode}-{(groupIndex + 1):D4}0";
         var distinctOrderIdCount = items.Select(x => x.OrderId).Distinct().Count();
         var packingLineTotal = items.Count;
         var packingSort = string.Join(",", items.OrderBy(x => x.Skucode)
            .Select(x => $"{x.Skucode}*{x.OrderQty}"));
         var(deliveryCode, PackingQTY) = GetDeliveryCode(items, shopCode, _dbContext);
         var deliveryFee = DistributeAmount(deliveryCode, PackingQTY, packingLineTotal, _dbContext);
         var PackingCont = SplitAndConcatProductNames(items, deliveryCode, _dbContext);
         var packingCont1 = ReplaceDuplicates(PackingCont[0]);
         var packingCont2 = PackingCont.Count > 1 ? ReplaceDuplicates(PackingCont[1]) : string.Empty;
         var orderIds = items.GroupBy(x => x.OrderId).Select(x => x.Key).ToList();
         var shipNotes = ConcatStringsFromPosition(orderIds, shopCode);

         return items.Select((item, itemIndex) =>
         {
            item.LastOrderDate = lastOrderDate;
            if (normalize?.level == 3)
            {
               item.ShipPrefecture = normalize.pref;
               item.ShipCity = normalize.city;
               item.ShipAddress1 = $"{normalize.town}{addressNumber}" ;
               item.ShipAddress2 = building;
            }
            item.NormAddressLevel = normalize?.level;
            item.PackingId = packingId;
            item.PackingOrderIdCount = distinctOrderIdCount;
            item.PackingLineId = itemIndex + 1;
            item.PackingLineTotal = packingLineTotal;
            item.PackingSort = packingSort;
            item.DeliveryFee = (int?)deliveryFee[itemIndex];
            item.ShipNotes = shipNotes;
            var (code, qty) = GetDeliveryCode([item], shopCode, _dbContext);
            item.LineDeliveryCode = code;
            item.PackingQty = qty;
            item.IsDeliveryLabel = false;

            // PackingId + OrderId ごとに最終行を特定
            var isLastLineInOrder = itemIndex == items.LastIndexOf(items.LastOrDefault(x => x.OrderId == item.OrderId));
            if (isLastLineInOrder)
            {
               item.DeliveryCode = deliveryCode;
               item.DeliveryName = _dbContext.Deliveries
                  .FirstOrDefault(x => x.DeliveryCode == deliveryCode)?.DeliveryName;
               item.PackingQty = PackingQTY;
               if (packingLineTotal == itemIndex + 1) item.IsDeliveryLabel = true;
               item.PackingCont1 = packingCont1;
               item.PackingCont2 = packingCont2;
            }
            return item;
         });
      }).ToList();
   }

   private static (string? , string?) SplitAddress(string input)
   {
      // 正規表現で番地と建物名を分離
      var regex = new Regex(@"^(.+?\d+(?:-\d+)*)(?:\s*(.+))?$"); var match = regex.Match(input);
      if (match.Success)
      {
         string houseNumber = match.Groups[1].Value.Trim(); // 番地
         string? buildingName = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null; // 建物名（オプション）
         return (houseNumber, buildingName);
      }
      // 分割できなかった場合、全体を番地として扱い、建物名は null
      return (input, null);
   }

   /// <summary>
   /// 送料を梱包明細数で按分する。
   /// </summary>
   public static List<decimal> DistributeAmount(string deliveryCode, int PackingQTY, int packingLineTotal, ssAppDBContext _dbContext)
   {
      // 例外処理：packingLineTotal が 0 以下の場合、空のリストを返す
      if (packingLineTotal <= 0) return [];

      // 合計金額計算
      var deliveryFee = _dbContext.Deliveries.FirstOrDefault(x => x.DeliveryCode == deliveryCode)?.DeliveryFee ?? 0;
      var totalAmount = deliveryFee * PackingQTY;
      // 合計金額が 0 の場合はすべて 0 を返す
      if (totalAmount == 0)
         return Enumerable.Repeat(0m, packingLineTotal).ToList();

      // 1件あたりの金額（小数点以下切り捨て）
      decimal perItemAmount = Math.Floor((decimal)totalAmount / packingLineTotal);
      // 端数の合計（最後の明細で調整）
      decimal remainder = totalAmount - (perItemAmount * packingLineTotal);
      // 明細ごとの金額リスト
      var distributedAmounts = Enumerable.Repeat(perItemAmount, packingLineTotal).ToList();
      // 端数を最後の明細に加算（安全チェック）
      if (remainder > 0)
         distributedAmounts[packingLineTotal - 1] += remainder;
      return distributedAmounts;
   }

   /// <summary>
   /// 配送伝票関連メソッド。配送適用欄の文字数制限に合わせて各モールの注文IDの固定部分をブランクにする。
   /// 注文IDが複数の場合、2つ目以降のIDの固定部分をブランクにする。
   /// </summary>
   private static string ConcatStringsFromPosition(List<string> inputList,  string shopCode)
   {
      //　例（楽天）
      // inputList = { "413247-20250205-0000000001", "413247-20250205-0000000002", "413247-20250205-0000000003" }
      // startIndex = 16
      // 戻り値 = "413247-20250205-0000000001, 0000000002, 0000000003"
      var startIndex = ShopOrderPrefixSize.TryGetValue(shopCode, out int value) ? value : 1;

      return string.Join(", ", inputList.Select((s, i) => i == 0 
         ? s : (s.Length >= startIndex ? s[startIndex..] : "")));
   }

   /// <summary>
   /// 配送伝票関連メソッド。配送伝票の内容物記載枠数に合わせて注文商品名を等分割する。
   /// </summary>
   public static List<string> SplitAndConcatProductNames(
      List<DailyOrderNews> packingGroupItems, string deliveryCode, 
      ssAppDBContext _dbContext)
   {
      var splitCount = LabelItemCount.TryGetValue(deliveryCode, out int value) ? value : 1;

      // 商品コードから商品名を取得（商品名 + "×" + 購入数）購入数が1の場合は商品名のみ
      var productNames = packingGroupItems.OrderBy(x => x.Skucode)
         .Select(sku =>
         { 
            var skuAbbr = _dbContext.ProductSkus
               .SingleOrDefault(x => x.Skucode == sku.Skucode)?.Skuabbr ?? string.Empty;
            return sku.OrderQty > 1 ? $"{skuAbbr}×{sku.OrderQty}" : skuAbbr; 
         }).ToList();

      // 伝票の内容品１枠あたりの商品名数を計算（splitCount：伝票の枠数）
      int splitSize = (int)Math.Ceiling((double)productNames.Count / splitCount);

      // 商品名をN分割して連結
      var result = productNames
         .Select((value, index) => new { value, index })
         .GroupBy(x => x.index / splitSize)
         .Select(group => string.Join("、", group.Select(x => x.value)))
         .ToList();
      return result;
   }

   /// <summary>
   /// 発送商品名の重複文字列置換処理
   /// 同一商品名の2回目以降の出現箇所を置換する。
   /// </summary>
   public static string ReplaceDuplicates(string input)
   {
      Dictionary<string, bool> seen = [];
      string result = input;

      foreach (var key in ReplacementMap.Keys)
      {
         int firstIndex = result.IndexOf(key);
         if (firstIndex == -1) continue; // 置換対象がない場合はスキップ

         seen[key] = true; // 初回はそのまま

         // 2回目以降の出現箇所を置換
         int secondIndex = result.IndexOf(key, firstIndex + key.Length);
         while (secondIndex != -1)
         {
            result = result.Remove(secondIndex, key.Length).Insert(secondIndex, ReplacementMap[key]);
            secondIndex = result.IndexOf(key, secondIndex + ReplacementMap[key].Length);
         }
      }
      return result;
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
            }))
         .OrderBy(x => x.Priority).FirstOrDefault();

      if (applicableConditions != null)
      {
         deliveryCode = applicableConditions.DeliveryCode;
         packingQty = (int)Math.Ceiling((double)applicableConditions.TotalQty / applicableConditions.MaxPackageCapacity);
      }

      return (deliveryCode, packingQty);
   }

   /// <summary>
   /// マッピング処理（ HTTPResponseModel -> DB Model ）
   /// Rakuten注文明細 (RakutenGetOrderResponse) -> DailyOrderNews
   /// </summary>
   /// <param name="rakutenGetOrderResponse">HTTPResponseModel</param>
   /// <param name="rakutenShop">楽天ショップ</param>
   /// <param name="status">注文ステータス</param>
   /// <param name="_dbContext">DBコンテキスト</param>
   /// <returns></returns>
   public static List<DailyOrderNews> RakutenToDailyOrderNews(
      RakutenGetOrderResponse rakutenGetOrderResponse, RakutenShop rakutenShop,
      OrderStatus status, ssAppDBContext _dbContext)
   {
      if (rakutenGetOrderResponse.OrderModelList?.Any() != true) return [];

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
                     string.Empty, rakutenShop.ToString(), _dbContext.Skuconversions);
               var productName = _dbContext.ProductSkus.FirstOrDefault(x => x.Skucode == skuCode);
               return new DailyOrderNews
               {
                  Status = status.ToString(),
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
                  Skuname = productName?.Skuname ?? string.Empty,
                  Skuabbr = productName?.Skuabbr ?? string.Empty,
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
         return [];
      // 対象プロパティ名の取得
      var validFields = AppModelHelpers.GetDailyOrderNewsFields();

      return responses.SelectMany(response =>
      {
         if (response?.ResultSet?.Result?.OrderInfo == null)
            return [];

         var orderInfo = response.ResultSet.Result.OrderInfo;

         // Order, Pay, Ship, Seller, Buyer, Detail のフィールドをまとめる
         var flatFields = new[] { orderInfo.Order, orderInfo.Pay, orderInfo.Ship, orderInfo.Seller, orderInfo.Buyer, orderInfo.Detail }
            .Where(dict => dict != null).SelectMany(dict => dict!)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

         // 1:N 関係の Items のマッピング
         var items = orderInfo.Items ?? [];

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
      List<DailyOrderNewsYahoo> source, string yahooShop, 
      OrderStatus status, ssAppDBContext _dbContext)
   {
      if (source?.Any() != true) return [];

      // 1. 同一OrderId内のOrderLineIdの最大値を計算
      var orderLineTotals = source
          .GroupBy(item => item.OrderId)
          .ToDictionary(g => g.Key, g => g.Max(item => item.LineId));

      // 2. マッピング処理
      var result = new List<DailyOrderNews>();
      foreach (var item in source)
      {
         var (productCode, skuCode) = GetSKUCode(item.ItemId, item.SubCode,
            item.ItemOption, yahooShop, _dbContext.Skuconversions);
         var productName = _dbContext.ProductSkus.FirstOrDefault(x => x.Skucode == skuCode);

         var mappedItem = new DailyOrderNews
         {
            Status = status.ToString(),                          // 注文ステータス
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
            Skuname = productName?.Skuname ?? string.Empty,      // SKU名
            Skuabbr = productName?.Skuabbr ?? string.Empty,      // SKU略称
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

      var sku = itemId + itemSub;
      if (convertCode != null)
         (itemId, sku) = (convertCode.ProductCode, convertCode.Skucode);

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

      var property = typeof(T).GetProperty(propertyName) ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T)}'.");

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