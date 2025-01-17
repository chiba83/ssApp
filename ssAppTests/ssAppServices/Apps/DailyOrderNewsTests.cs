#pragma warning disable CS8602, CS8604, CS8620
using Microsoft.Extensions.DependencyInjection;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Apps;
using ssAppTests.ssAppServices.Helpers;

namespace ssAppTests.ssAppServices.Apps
{
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
            var skuCompare = _dbContext.Skuconversions.Where(x => x.ShopCode == yahooShop.ToString()).Select(x => x.ShopSkucode).ToList();
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
}
