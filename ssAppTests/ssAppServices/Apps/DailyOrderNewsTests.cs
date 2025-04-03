#pragma warning disable CS8602, CS8604, CS8620, CS8600
using Microsoft.Extensions.DependencyInjection;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api;
using ssAppServices.Apps;
using ssAppTests.ssAppServices.Helpers;
using NormalizeJapaneseAddressesNET;
using System.Text.RegularExpressions;
using ssAppServices.Api.Yahoo;

namespace ssAppTests.ssAppServices.Apps;

[TestFixture]
public class DailyOrderNewsTests
{
   private ServiceProvider _serviceProvider;
   private ssAppDBContext _dbContext;
   private SetDailyOrderNews _setDailyOrderNews;
   private YahooOrderList _yahooOrderList;
   private YahooOrderInfo _yahooOrderInfo;
   private readonly DateTime startDT = new(2025, 3, 13, 9, 0, 0);
   private readonly DateTime endDT = new(2025, 3, 23, 11, 59, 59);
   //private readonly OrderStatus orderStatus = OrderStatus.NewOrder;
   //private readonly OrderStatus orderStatus = OrderStatus.Packing;
   private readonly OrderStatus orderStatus = OrderStatus.Shipped;
   //private readonly UpdateMode updateMode = UpdateMode.Insert;
   private readonly UpdateMode updateMode = UpdateMode.Replace;
   //private readonly bool normalizeAddresses = true;
   private readonly bool normalizeAddresses = false;


   [SetUp]
   public void SetUp()
   {
      var services = new ServiceCollection();

      // appsettings.json を読み込み、StartupのConfigureServicesを流用
      var startup = new Startup();
      startup.ConfigureServices(services);

      _serviceProvider = services.BuildServiceProvider();

      _dbContext = _serviceProvider.GetRequiredService<ssAppDBContext>();
      _setDailyOrderNews = _serviceProvider.GetRequiredService<SetDailyOrderNews>();
      _yahooOrderList = _serviceProvider.GetRequiredService<YahooOrderList>();
      _yahooOrderInfo = _serviceProvider.GetRequiredService<YahooOrderInfo>();
   }

   [TearDown]
   public void TearDown()
   {
      _dbContext?.Dispose();
      _serviceProvider?.Dispose();
   }

   /// <summary>
   /// 目的：Interface Modelへのマッピング処理をテストする。
   /// HTTPレスポンスとDailyOrderNews（Interface Model）のフィールド値を比較チェックする。
   /// </summary>
   [Test]
   public void T01_YahooOrderInfo_Success()
   {
      foreach (YahooShop shop in Enum.GetValues(typeof(YahooShop)))
      {
         Console.WriteLine($"● 検査ショップ： {shop}");
         Console.WriteLine("--------------------------------------------------");

         var (httpOrderList, orderList) = Run_YahooOrderSearch(shop);
         var orderIds = orderList.Search.OrderInfo.Select(x => x.Fields["OrderId"].ToString()).ToList();
         if (orderIds == null || orderIds.Count == 0)
         {
            Console.WriteLine("注文情報がありません。");
            Console.WriteLine("--------------------------------------------------");
            continue;
         }

         var outputFields = AppModelHelpers.GetDailyOrderNewsFields();

         // GetOrderInfo 実行
         var (httpResponses, orderInfos)
            = _yahooOrderInfo.GetOrderInfoWithResponse(orderIds, outputFields, shop);
         // マッピング処理 - Yahoo注文明細 (HttpResponseModel) -> interface Model)
         var dailyOrderNews = DailyOrderNewsMapper.YahooOrderInfo(orderInfos, shop);

         // orderListのデータ件数とhttpOrderListの件数が一致すること。Response件数はGetOrderListTotalで取得
         var rowNumber = YahooApiHelpers.GetOrderListTotal(httpOrderList);
         Assert.That(orderIds, Has.Count.EqualTo(rowNumber), "orderListのデータ件数とhttpResponsesの件数が一致しません。");

         /******************************************************************************/
         // データ件数チェック
         // orderInfoのデータ件数とhttpResponsesの件数が一致すること。Response件数はGetOrderInfoItemCountで取得
         rowNumber = YahooApiHelpers.GetOrderInfoItemCount(httpResponses);
         Assert.That(dailyOrderNews, Has.Count.EqualTo(rowNumber), "orderInfoのデータ件数とhttpResponsesの件数が一致しません。");
         Console.WriteLine($"データ件数（注文Item数）： {rowNumber}");
         Console.WriteLine("--------------------------------------------------");

         /******************************************************************************/
         // DailyOrderNewsのフィールド値を全件チェック
         // Yahoo注文情報のフィールド値を取得
         var fieldDefinitions = YahooOrderInfoFieldDefinitions.GetAllFields();
         var itemFieldList = YahooOrderInfoFieldDefinitions.Item.Keys.ToList();
         var fields = AppModelHelpers.GetDailyOrderNewsFields();

         foreach (var rec in dailyOrderNews)
         {
            string lineId = rec.LineId.ToString();
            var itemValues = YahooApiHelpers.GetOrderInfoItemValues(httpResponses, rec.OrderId, lineId, fieldDefinitions);
            var itemOption = YahooApiHelpers.GetItemOptionValue(httpResponses, rec.OrderId, lineId);
            var inscription = YahooApiHelpers.GetInscriptionValue(httpResponses, rec.OrderId, lineId);

            foreach (var field in fields)
            {
               if (field == "ItemOption")
               {
                  Assert.That(rec.GetType().GetProperty(field)?.GetValue(rec), Is.EqualTo(itemOption), $"{field}の値が一致しません。");
                  continue;
               }
               if (field == "Inscription")
               {
                  Assert.That(rec.GetType().GetProperty(field)?.GetValue(rec), Is.EqualTo(inscription), $"{field}の値が一致しません。");
                  continue;
               }
               if (itemFieldList.Contains(field))
               {
                  Assert.That(rec.GetType().GetProperty(field)?.GetValue(rec), Is.EqualTo(itemValues[field]), $"{field}の値が一致しません。");
                  continue;
               }
               var value = YahooApiHelpers.GetOrderInfoFieldValue(httpResponses, rec.OrderId, field, fieldDefinitions);
               Assert.That(rec.GetType().GetProperty(field)?.GetValue(rec), Is.EqualTo(value), $"{field}の値が一致しません。");
            }
         }
      }
   }

   /// <summary>
   /// </summary>
   [Test]
   public void T02_FetchDailyOrderFromYahoo()
   {
      foreach (YahooShop yahooShop in Enum.GetValues(typeof(YahooShop)))
      {
         Console.WriteLine($"● 検査ショップ： {yahooShop}");
         Console.WriteLine("--------------------------------------------------");

         List<DailyOrderNewsYahoo> DON = null;
         List<DailyOrderNews> DONY = null;
         switch (orderStatus)
         {
            case OrderStatus.NewOrder:
               (DON, DONY) = _setDailyOrderNews.FetchDailyOrderFromYahoo(yahooShop, OrderStatus.NewOrder, null, null, normalizeAddresses, updateMode);
               break;
            case OrderStatus.Packing:
               (DON, DONY) = _setDailyOrderNews.FetchDailyOrderFromYahoo(yahooShop, OrderStatus.Packing, null, null, normalizeAddresses, updateMode);
               break;
            case OrderStatus.Shipped:
               (DON, DONY) = _setDailyOrderNews.FetchDailyOrderFromYahoo(yahooShop, OrderStatus.Shipped, startDT, endDT, normalizeAddresses, updateMode);
               break;
         }
         if (DON?.Any() == false || DONY?.Any() == false)
         {
            Console.WriteLine("注文情報がありません。");
            Console.WriteLine("--------------------------------------------------");
            continue;
         }

         // skuコンバートエラー
         var skuCodes = _dbContext.DailyOrderNews.Where(x => x.ShopCode == yahooShop.ToString()).GroupBy(x => x.Skucode).Select(x => x.Key).ToList();
         var skuCompare = _dbContext.Skuconversions
            .Where(x => x.ShopCode == yahooShop.ToString() && x.Skucode != x.ShopSkucode).Select(x => x.ShopSkucode).ToList();
         Assert.That(skuCodes, Has.None.Matches<string>(sku => skuCompare.Contains(sku)), "skuコンバートエラー");
         Console.WriteLine("Sku-Conversions：Success");
         Console.WriteLine("--------------------------------------------------");
         Assert.That(DON, Has.Count.EqualTo(DONY.Count), "DailyOrderNewsとDailyOrderNewsYahooのデータ件数が一致しません。");

         // DONYのデータ件数とDailyOrderNews.ShopCodeが"Yahoo"で始まる件数が一致すること
         var yahooCount = _dbContext.DailyOrderNews.Count(x => x.ShopCode == yahooShop.ToString() && x.Status == orderStatus.ToString());
         Assert.That(DONY, Has.Count.EqualTo(yahooCount), "DailyOrderNewsYahooのデータ件数とDailyOrderNews.ShopCodeの件数が一致しません。");

         var orderIds = _dbContext.DailyOrderNews.Where(x => x.ShopCode == yahooShop.ToString() && x.Status == orderStatus.ToString())
            .GroupBy(x => x.OrderId).Select(x => x.Key).ToList();

         Console.WriteLine($"データ件数 Success： {orderIds.Count}");
         Console.WriteLine("--------------------------------------------------");
         int rowNumber = 1;
         foreach (var r in _dbContext.DailyOrderNews.Where(x => x.ShopCode == yahooShop.ToString() && x.Status == orderStatus.ToString()))
         {
            Console.WriteLine($"Row {rowNumber++} : {r.OrderId}, {r.OrderLineId}, {r.Skucode}");
            Console.WriteLine("--------------------------------------------------");
         }
      }
   }

   /// <summary>
   /// </summary>
   [Test]
   public void T03_FetchDailyOrderFromRakuten()
   {
      foreach (RakutenShop rakutenShop in Enum.GetValues(typeof(RakutenShop)))
      {
         Console.WriteLine($"● 検査ショップ： {rakutenShop}");
         Console.WriteLine("--------------------------------------------------");

         List<string> orderNumbers = null;
         RakutenGetOrderResponse getOrderResponseTake100 = null;
         switch (orderStatus)
         {
            case OrderStatus.NewOrder:
               (orderNumbers, getOrderResponseTake100) = _setDailyOrderNews.FetchDailyOrderFromRakuten(rakutenShop, OrderStatus.NewOrder, null, null, normalizeAddresses, updateMode);
               break;
            case OrderStatus.Packing:
               (orderNumbers, getOrderResponseTake100) = _setDailyOrderNews.FetchDailyOrderFromRakuten(rakutenShop, OrderStatus.Packing, null, null, normalizeAddresses, updateMode);
               break;
            case OrderStatus.Shipped:
               (orderNumbers, getOrderResponseTake100) = _setDailyOrderNews.FetchDailyOrderFromRakuten(rakutenShop, OrderStatus.Shipped, startDT, endDT, normalizeAddresses, updateMode);
               break;
         }
         // 注文情報が取得できない場合は処理を終了
         if (orderNumbers.Count == 0)
         {
            Console.WriteLine("注文情報がありません。");
            Console.WriteLine("--------------------------------------------------");
            continue;
         }

         // skuコンバートエラー
         var skuCodes = _dbContext.DailyOrderNews.Where(x => x.ShopCode == rakutenShop.ToString()).GroupBy(x => x.Skucode).Select(x => x.Key).ToList();
         var skuCompare = _dbContext.Skuconversions.Where(x => x.ShopCode == rakutenShop.ToString()).Select(x => x.ShopSkucode).ToList();
         Assert.That(skuCodes, Has.None.Matches<string>(sku => skuCompare.Contains(sku)), "skuコンバートエラー");
         Console.WriteLine("Sku-Conversions：Success");
         Console.WriteLine("--------------------------------------------------");

         // DailyOrderNewsのデータ件数とDailyOrderNews.ShopCodeが"Rakuten"で始まる件数が一致すること
         var orderIds = _dbContext.DailyOrderNews.Where(x => x.ShopCode == rakutenShop.ToString() && x.Status == orderStatus.ToString())
            .GroupBy(x => x.OrderId).Select(x => x.Key).ToList();
         Assert.That(orderNumbers, Has.Count.EqualTo(orderIds.Count), $"DailyOrderNews {rakutenShop} のデータ件数とDailyOrderNews.ShopCodeの件数が一致しません。");
         Assert.That(orderNumbers, Is.EquivalentTo(orderIds), "DailyOrderNewsのOrderNumberとSerchOrderで取得したOrderNumberが一致しない。");

         Console.WriteLine($"データ件数 Success： {orderNumbers.Count}");
         Console.WriteLine("--------------------------------------------------");
         int rowNumber = 1;
         foreach (var r in _dbContext.DailyOrderNews.Where(x => x.ShopCode == rakutenShop.ToString() && x.Status == orderStatus.ToString()))
         {
            Console.WriteLine($"Row {rowNumber++} : {r.OrderId.Split('-').Skip(2).DefaultIfEmpty("").Aggregate((a, b) => a + "-" + b)}, {r.OrderLineId}, {r.Skucode}");
            Console.WriteLine("--------------------------------------------------");
         }
      }
   }

   /// <summary>
   /// 住所の正規化処理をテストする。
   /// </summary>
   [Test]
   public void T04_NormalizeAddressTest()
   {
      var config = new Config();
      config.JapaneseAddressesApi = @"C:\ssApp\data\full_normalize_data.json";
      int row = 1;
      foreach (var order in _dbContext.DailyOrderNews)
      {
         // 元の住所情報を構築
         string beforeAddress = $"{order.ShipPrefecture} {order.ShipCity} {order.ShipAddress1} {order.ShipAddress2}";

         // 正規化処理（同期的に実行）
         var result = NormalizeJapaneseAddresses.Normalize(beforeAddress).Result;

         // 正規化後の住所情報
         string afterAddress = $"{result.pref} {result.city} {result.town} {result.addr} level:{result.level}";

         // ビフォー・アフターを出力
         Console.WriteLine($"Row {row++}  {order.ShopCode}");
         Console.WriteLine("Before: " + beforeAddress);
         Console.WriteLine("After : " + afterAddress);
         if (result.level == 3)
         {
            var (n, b) = SplitAddress(result.addr);
            Console.WriteLine(n + " ： " + b);
         }
         var (n1, b1) = SplitAddress(beforeAddress);
         Console.WriteLine(n1 + " : " + b1);
         Console.WriteLine("-----------------------------------");
      }
   }

   /// <summary>
   /// 発送方法コードと梱包数を取得するテスト
   /// </summary>
   [Test]
   public void T05_GetDeliveryCodeTest()
   {
      // テストシナリオ・期待値を一元管理
      var testScenarios = new List<(List<DailyOrderNews> Items, string ExpectedDeliveryCode, int ExpectedPackingQty)>
      {
         // #01
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 }
         }, "003", 1),
         // #02
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 },
            new() { ProductCode = "200070-eb25", Skucode = "200070-eb25", OrderQty = 1 },
            new() { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 1 }
         }, "092", 1),
         // #03
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 },
            new() { ProductCode = "200070-eb25", Skucode = "200070-eb25", OrderQty = 1 },
            new() { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 4 }
         }, "092", 1),
         // #04
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 },
            new() { ProductCode = "200070-eb25", Skucode = "200070-eb25", OrderQty = 1 },
            new() { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 9 }
         }, "020", 1),
         // #05
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 },
            new() { ProductCode = "200070-eb25", Skucode = "200070-eb25", OrderQty = 1 },
            new() { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 11 }
         }, "020", 2),
         // #06
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200030", Skucode = "200030White", OrderQty = 1 }
         }, "092", 1),
         // #07
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200030", Skucode = "200030White", OrderQty = 1 },
            new() { ProductCode = "200080", Skucode = "20008002Black-SKU", OrderQty = 1 },
            new() { ProductCode = "200110", Skucode = "20011001Black", OrderQty = 1 }
         }, "020", 1),
         // #08
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200030", Skucode = "200030White", OrderQty = 1 },
            new() { ProductCode = "200080", Skucode = "20008002Black-SKU", OrderQty = 1 },
            new() { ProductCode = "200110", Skucode = "20011001Black", OrderQty = 2 }
         }, "020", 2),
         // #09
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200030", Skucode = "200030White", OrderQty = 6 },
            new() { ProductCode = "200080", Skucode = "20008002Black-SKU", OrderQty = 1 },
            new() { ProductCode = "200110", Skucode = "20011001Black", OrderQty = 2 }
         }, "020", 3),
         // #10
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200160", Skucode = "200160S20W1P150C", OrderQty = 1 }
         }, "020", 1),
         // #11
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200160", Skucode = "200160S20W1P150C", OrderQty = 1 },
            new() { ProductCode = "200160", Skucode = "200160S65W1P150L", OrderQty = 4 }
         }, "020", 2),
         // #12
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200160", Skucode = "200160S20W1P", OrderQty = 2 }
         }, "020", 1),
         // #13
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200160", Skucode = "200160S20W1P", OrderQty = 2 },
            new() { ProductCode = "200160", Skucode = "200160S65W3P", OrderQty = 10 }
         }, "020", 2),
         // #14
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 1 },
            new() { ProductCode = "200150", Skucode = "200150", OrderQty = 1 }
         }, "092", 1),
         // #15
         (new List<DailyOrderNews>
         {
            new() { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 13 },
            new() { ProductCode = "200150", Skucode = "200150", OrderQty = 1 }
         }, "020", 2),
      };

      int row = 1;
      foreach (var (Items, ExpectedDeliveryCode, ExpectedPackingQty) in testScenarios)
      {
         var (actualDeliveryCode, actualPackingQty) = DailyOrderNewsMapper.GetDeliveryCode(Items, RakutenShop.Rakuten_ENZO.ToString(), _dbContext);
         Console.WriteLine($"Test Case #{row++}: {string.Join(", ", Items.Select(i => $"{i.ProductCode} ({i.OrderQty})"))}");
         Console.WriteLine($"Expected: {ExpectedDeliveryCode}, {ExpectedPackingQty}");
         Console.WriteLine($"Actual: {actualDeliveryCode}, {actualPackingQty}");
         Console.WriteLine("------------------------------------------------");
         Assert.That(actualDeliveryCode, Is.EqualTo(ExpectedDeliveryCode));
         Assert.That(actualPackingQty, Is.EqualTo(ExpectedPackingQty));
      }
   }

   /// <summary>
   /// 発送商品名の重複文字列置換処理をテストする。
   /// </summary>
   [Test]
   public void T06_ReplaceDuplicatesTest()
   {
      string testString = "靴下S黒、靴下L灰、靴下S白、替えブラシEB20、替えブラシEB50、USB-C、靴下L3色、ベネチアン40cm、アズキ、スクリュー、ベネチアン50cm";
      string expectedOutput = "靴下S黒、靴下L灰、S白、替えブラシEB20、EB50、USB-C、L3色、ベネチアン40cm、アズキ、スクリュー、50cm";
      string result = DailyOrderNewsMapper.ReplaceDuplicates(testString);
      Console.WriteLine(testString);
      Console.WriteLine(expectedOutput);
      Console.WriteLine(result);
      Assert.That(result, Is.EqualTo(expectedOutput));
   }

   private static (string? HouseNumber, string? BuildingName) SplitAddress(string input)
   {
      // 正規表現で番地と建物名を分離
      var regex = new Regex(@"^(.+?\d+(?:-\d+)*)(?:\s*(.+))?$"); var match = regex.Match(input);
      if (match.Success)
      {
         string houseNumber = match.Groups[1].Value.Trim(); // 番地
         string buildingName = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null; // 建物名（オプション）
         return (houseNumber, buildingName);
      }

      // 分割できなかった場合、全体を番地として扱い、建物名は null
      return (input, null);
   }

   private (HttpResponseMessage, YahooOrderListResult) Run_YahooOrderSearch(YahooShop yahooShop)
   {
      // APIリクエスト作成
      var sellerId = ApiHelpers.GetShopToken(_dbContext, yahooShop.ToString()).SellerId;
      var outputFields = string.Join(",", YahooOrderListRequestFactory.OutputFieldsDefault);
      var yahooOrderListRequest = YahooOrderListRequestFactory.NewOrderRequest(null, null, 1, outputFields, sellerId);
      // HTTP API実行
      var (httpResponses, orderList) = _yahooOrderList.GetOrderSearchWithResponse(yahooOrderListRequest, yahooShop);

      int rowNumber = 1;
      foreach (var orderId in orderList.Search.OrderInfo.Select(x => x.Fields["OrderId"].ToString()))
      {
         Console.WriteLine($"Row {rowNumber} : {orderId}");
         Console.WriteLine("--------------------------------------------------");
         rowNumber++;
      }
      return (httpResponses, orderList);
   }
}