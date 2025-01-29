#pragma warning disable CS8602, CS8604, CS8620, CS8600
using Microsoft.Extensions.DependencyInjection;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Apps;
using ssAppTests.ssAppServices.Helpers;
using NormalizeJapaneseAddressesNET;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace ssAppTests.ssAppServices.Apps;

[TestFixture]
public class DailyOrderNewsTests
{
   private ServiceProvider _serviceProvider;
   private ssAppDBContext _dbContext;
   private SetDailyOrderNews _setDailyOrderNews;

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
   }

   [TearDown]
   public void TearDown()
   {
      _dbContext?.Dispose();
      _serviceProvider?.Dispose();
   }

   [Test]
   public void T01_YahooOrderInfo_Success()
   {
      foreach (YahooShop shop in Enum.GetValues(typeof(YahooShop)))
      {
         Console.WriteLine($"● 検査ショップ： {shop.ToString()}");
         Console.WriteLine("--------------------------------------------------");

         var (httpOrderList, orderList) = Run_YahooOrderSearch(shop);
         var orderIds = orderList.Search.OrderInfo.Select(x => x.Fields["OrderId"].ToString()).ToList();
         if (orderIds == null  || orderIds.Count == 0)
         {
            Console.WriteLine("注文情報がありません。");
            Console.WriteLine("--------------------------------------------------");
            continue;
         }

         var (httpResponses, orderInfo) = _setDailyOrderNews.GetYahooOrderInfoWithResponse(orderIds, shop);

         // orderListのデータ件数とhttpOrderListの件数が一致すること。Response件数はGetOrderListTotalで取得
         var rowNumber = YahooApiHelpers.GetOrderListTotal(httpOrderList);
         Assert.That(orderIds.Count, Is.EqualTo(rowNumber), "orderListのデータ件数とhttpResponsesの件数が一致しません。");

         /******************************************************************************/
         // データ件数チェック
         // orderInfoのデータ件数とhttpResponsesの件数が一致すること。Response件数はGetOrderInfoItemCountで取得
         rowNumber = YahooApiHelpers.GetOrderInfoItemCount(httpResponses);
         Assert.That(orderInfo.Count, Is.EqualTo(rowNumber), "orderInfoのデータ件数とhttpResponsesの件数が一致しません。");
         Console.WriteLine($"データ件数（注文Item数）： {rowNumber}");
         Console.WriteLine("--------------------------------------------------");

         /******************************************************************************/
         // DailyOrderNewsのフィールド値を全件チェック
         // Yahoo注文情報のフィールド値を取得
         var fieldDefinitions = YahooOrderInfoFieldDefinitions.GetAllFields();
         var itemFieldList = YahooOrderInfoFieldDefinitions.Item.Keys.ToList();
         var fields = DailyOrderNewsModelHelper.GetYahooOrderInfoFields();

         foreach (var rec in orderInfo)
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

   [Test]
   public void T02_FetchDailyOrderFromYahoo()
   {
      foreach (YahooShop yahooShop in Enum.GetValues(typeof(YahooShop)))
      {
         Console.WriteLine($"● 検査ショップ： {yahooShop.ToString()}");
         Console.WriteLine("--------------------------------------------------");
         var (DON, DONY) = _setDailyOrderNews.FetchDailyOrderFromYahoo(yahooShop);
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
         Assert.That(DON.Count, Is.EqualTo(DONY.Count), "DailyOrderNewsとDailyOrderNewsYahooのデータ件数が一致しません。");

         // DONYのデータ件数とDailyOrderNews.ShopCodeが"Yahoo"で始まる件数が一致すること
         var yahooCount = _dbContext.DailyOrderNews.Count(x => x.ShopCode == yahooShop.ToString());
         Assert.That(DONY.Count, Is.EqualTo(yahooCount), "DailyOrderNewsYahooのデータ件数とDailyOrderNews.ShopCodeの件数が一致しません。");

         var orderIds = _dbContext.DailyOrderNews.Where(x => x.ShopCode == yahooShop.ToString())
               .GroupBy(x => x.OrderId).Select(x => x.Key).ToList();

         Console.WriteLine($"データ件数 Success： {orderIds.Count}");
         Console.WriteLine("--------------------------------------------------");
         int rowNumber = 1;
         foreach (var r in _dbContext.DailyOrderNews.Where(x => x.ShopCode == yahooShop.ToString()))
         {
            Console.WriteLine($"Row {rowNumber++} : {r.OrderId}, {r.OrderLineId}, {r.Skucode}");
            Console.WriteLine("--------------------------------------------------");
         }
      }
   }

   [Test]
   public void T03_FetchDailyOrderFromRakuten()
   {
      foreach (RakutenShop rakutenShop in Enum.GetValues(typeof(RakutenShop)))
      {
         Console.WriteLine($"● 検査ショップ： {rakutenShop.ToString()}");
         Console.WriteLine("--------------------------------------------------");
         var (orderNumbers, getOrderResponseTake100) = _setDailyOrderNews.FetchDailyOrderFromRakuten(rakutenShop);

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
         var orderIds = _dbContext.DailyOrderNews.Where(x => x.ShopCode == rakutenShop.ToString())
               .GroupBy(x => x.OrderId).Select(x => x.Key).ToList();
         Assert.That(orderNumbers.Count, Is.EqualTo(orderIds.Count), $"DailyOrderNews {rakutenShop.ToString()} のデータ件数とDailyOrderNews.ShopCodeの件数が一致しません。");
         Assert.That(orderNumbers, Is.EquivalentTo(orderIds), "DailyOrderNewsのOrderNumberとSerchOrderで取得したOrderNumberが一致しない。");

         Console.WriteLine($"データ件数 Success： {orderNumbers.Count}");
         Console.WriteLine("--------------------------------------------------");
         int rowNumber = 1;
         foreach (var r in _dbContext.DailyOrderNews.Where(x => x.ShopCode == rakutenShop.ToString()))
         {
            Console.WriteLine($"Row {rowNumber++} : {r.OrderId.Split('-').Skip(2).DefaultIfEmpty("").Aggregate((a, b) => a + "-" + b)}, {r.OrderLineId}, {r.Skucode}");
            Console.WriteLine("--------------------------------------------------");
         }
      }
   }

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
            var(n,b) = SplitAddress(result.addr);
            Console.WriteLine(n + " ： " + b);
         }
         var (n1, b1) = SplitAddress(beforeAddress);
         Console.WriteLine(n1 + " : " + b1);
         Console.WriteLine("-----------------------------------");
      }
   }

   [Test]
   public void T05_GetDeliveryCodeTest()
   {
      // テストシナリオ・期待値を一元管理
      var testScenarios = new List<(List<DailyOrderNews> Items, string ExpectedDeliveryCode, int ExpectedPackingQty)>
      {
         // #01
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 }
         }, "003", 1),
         // #02
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200070-eb25", Skucode = "200070-eb25", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 1 }
         }, "092", 1),
         // #03
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200070-eb25", Skucode = "200070-eb25", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 4 }
         }, "092", 1),
         // #04
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200070-eb25", Skucode = "200070-eb25", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 9 }
         }, "020", 1),
         // #05
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200070-eb10", Skucode = "200070-eb10", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200070-eb25", Skucode = "200070-eb25", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 11 }
         }, "020", 2),
         // #06
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200030", Skucode = "200030White", OrderQty = 1 }
         }, "092", 1),
         // #07
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200030", Skucode = "200030White", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200080", Skucode = "20008002Black-SKU", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200110", Skucode = "20011001Black", OrderQty = 1 }
         }, "020", 1),
         // #08
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200030", Skucode = "200030White", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200080", Skucode = "20008002Black-SKU", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200110", Skucode = "20011001Black", OrderQty = 2 }
         }, "020", 2),
         // #09
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200030", Skucode = "200030White", OrderQty = 6 },
            new DailyOrderNews { ProductCode = "200080", Skucode = "20008002Black-SKU", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200110", Skucode = "20011001Black", OrderQty = 2 }
         }, "020", 3),
         // #10
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200160", Skucode = "200160S20W1P150C", OrderQty = 1 }
         }, "020", 1),
         // #11
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200160", Skucode = "200160S20W1P150C", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200160", Skucode = "200160S65W1P150L", OrderQty = 4 }
         }, "020", 2),
         // #12
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200160", Skucode = "200160S20W1P", OrderQty = 2 }
         }, "020", 1),
         // #13
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200160", Skucode = "200160S20W1P", OrderQty = 2 },
            new DailyOrderNews { ProductCode = "200160", Skucode = "200160S65W3P", OrderQty = 10 }
         }, "020", 2),
         // #14
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 1 },
            new DailyOrderNews { ProductCode = "200150", Skucode = "200150", OrderQty = 1 }
         }, "092", 1),
         // #15
         (new List<DailyOrderNews>
         {
            new DailyOrderNews { ProductCode = "200070-eb50", Skucode = "200070-eb50", OrderQty = 13 },
            new DailyOrderNews { ProductCode = "200150", Skucode = "200150", OrderQty = 1 }
         }, "020", 2),
      };

      int row = 1;
      foreach (var scenario in testScenarios)
      {
         var(actualDeliveryCode, actualPackingQty) = DailyOrderNewsMapper.GetDeliveryCode(scenario.Items, RakutenShop.Rakuten_ENZO.ToString(), _dbContext);
         Console.WriteLine($"Test Case #{row++}: {string.Join(", ", scenario.Items.Select(i => $"{i.ProductCode} ({i.OrderQty})"))}");
         Console.WriteLine($"Expected: {scenario.ExpectedDeliveryCode}, {scenario.ExpectedPackingQty}");
         Console.WriteLine($"Actual: {actualDeliveryCode}, {actualPackingQty}");
         Console.WriteLine("------------------------------------------------");
         Assert.That(actualDeliveryCode, Is.EqualTo(scenario.ExpectedDeliveryCode));
         Assert.That(actualPackingQty, Is.EqualTo(scenario.ExpectedPackingQty));
      }
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
      var (httpResponses, orderList) = _setDailyOrderNews.GetYahooOrderListWithResponse(yahooShop);
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
