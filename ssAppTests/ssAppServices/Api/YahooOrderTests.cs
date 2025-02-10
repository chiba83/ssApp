#pragma warning disable CS8618, CS8602, CS8604, CS8620
using Microsoft.Extensions.DependencyInjection;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api;
using ssAppServices.Api.Yahoo;
using System.Xml.Linq;

namespace ssApptests.ssAppServices.Api
{
   [TestFixture]
   public class YahooOrderTests
   {
      private ServiceProvider _serviceProvider;
      private ssAppDBContext _dbContext;
      private YahooOrderList _yahooOrderList;
      private YahooOrderInfo _yahooOrderInfo;

      [SetUp]
      public void SetUp()
      {
         var services = new ServiceCollection();

         // appsettings.json を読み込み、StartupのConfigureServicesを流用
         var startup = new Startup();
         startup.ConfigureServices(services);

         _serviceProvider = services.BuildServiceProvider();

         _dbContext = _serviceProvider.GetRequiredService<ssAppDBContext>();
         _yahooOrderList = _serviceProvider.GetRequiredService<YahooOrderList>();
         _yahooOrderInfo = _serviceProvider.GetRequiredService<YahooOrderInfo>();
      }

      [TearDown]
      public void TearDown()
      {
         _dbContext?.Dispose();
         _serviceProvider?.Dispose();
      }

      [Test]
      public void T01_GetOrderSearch_Success()
      {
         // Arrange: 検索条件の作成
         var endDT = DateTime.Now;
         var startDT = endDT.AddDays(-3);
         var outputFields = string.Join(",", YahooOrderListRequestFactory.OutputFieldsDebug2);

         foreach (YahooShop yahooShop in Enum.GetValues(typeof(YahooShop)))
         {
            var sellerid = ApiHelpers.GetShopToken(_dbContext, yahooShop.ToString()).SellerId;
            var yahooOrderListRequest = YahooOrderListRequestFactory.NewOrderRequest(startDT, endDT, 1, outputFields, sellerid);

            // Act: Yahoo注文検索APIを呼び出す
            var (httpResponse, responseData) = _yahooOrderList.GetOrderSearchWithResponse(yahooOrderListRequest, yahooShop);
            var parsedData = responseData.Search.OrderInfo;

            // レスポンス総件数とコレクション件数が一致していること
            var totalCount = int.Parse(XDocument.Parse(httpResponse.Content.ReadAsStringAsync().Result)
                              .Descendants("TotalCount").FirstOrDefault()?.Value ?? "0");
            Assert.That(parsedData.Count, Is.EqualTo(totalCount), "レスポンス総件数とパース済みコレクション件数が一致しません。");

            if (totalCount == 0)
            {
               Console.WriteLine($"● 検査ショップ： {yahooShop.ToString()}");
               Console.WriteLine("--------------------------------------------------");
               Console.WriteLine($"注文件数： 0");
               Console.WriteLine("--------------------------------------------------");
               continue;
            }
            // リクエストしたフィールドがレスポンスにすべて含まれていること
            var f1 = parsedData.SelectMany(x => x.Fields.Keys).Distinct().ToList();
            var f2 = parsedData.SelectMany(x => x.Items.Keys).Distinct().ToList();
            var rpFields = f1.Concat(f2);
            var missingFields = rpFields.Where(x => !outputFields.Contains(x)).ToList();
            Assert.That(missingFields, Is.Empty, $"レスポンスコレクションに含まれていないフィールド: {string.Join(", ", missingFields)}");

            // 注文日時の範囲チェック
            var orderTimeFromDateTime = DateTime.ParseExact(yahooOrderListRequest.Req.Search.Condition.OrderTimeFrom, "yyyyMMddHHmmss", null);
            var orderTimeToDateTime = DateTime.ParseExact(yahooOrderListRequest.Req.Search.Condition.OrderTimeTo, "yyyyMMddHHmmss", null);
            // parsedData 内の OrderTime をチェック
            var invalidOrders = parsedData
                .Where(d => d.Fields.ContainsKey("OrderTime") && d.Fields["OrderTime"] != null)
                .Select(d => DateTime.Parse(d.Fields["OrderTime"].ToString()))
                .Where(orderTime => orderTime < orderTimeFromDateTime || orderTime > orderTimeToDateTime);

            // 範囲外のデータがあればNG
            Assert.That(invalidOrders.Any(), Is.False, "OrderTime が検索条件の範囲外のデータが含まれています。");

            // OrderId にnullが含まれていないこと
            var orderIds = YahooOrderSearchHelper.GetOrderIdList(responseData);
            Assert.That(orderIds.Any(x => x == null), Is.False, "OrderId にnullが含まれています。");

            Console.WriteLine($"● 検査ショップ： {yahooShop.ToString()}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"注文件数： {totalCount} 件");
            Console.WriteLine("T01_GetOrderSearch_Success");
            Console.WriteLine("--------------------------------------------------");
         }
      }

      [Test]
      public void T02_GetOrderInfo_Success()
      {
         var endDT = DateTime.Now;
         var startDT = endDT.AddDays(-3);
         var outputFields = string.Join(",", YahooOrderListRequestFactory.OutputFieldsDebug);

         foreach (YahooShop yahooShop in Enum.GetValues(typeof(YahooShop)))
         {
            var sellerid = ApiHelpers.GetShopToken(_dbContext, yahooShop.ToString()).SellerId;
            var yahooOrderListRequest = YahooOrderListRequestFactory.NewOrderRequest(startDT, endDT, 1, outputFields, sellerid);

            // Test用 OrderIDを取得
            var orderSearchResponse = _yahooOrderList.GetOrderSearch(yahooOrderListRequest, yahooShop);

            // GetOrderInfo の引数セット
            var orderIds = orderSearchResponse.Search.OrderInfo.Select(x => x.Fields["OrderId"].ToString()).ToList();
            var requestFields = new List<string>
            { "OrderId", "OrderTime", "OrderStatus", "LineId", "ItemId",
              "SubCode", "Quantity", "TotalPrice", "PayMethodName", "ShipFirstName",
              "ShipStatus", "ShipMethod", "IsLogin", "SellerId", "ItemOption", "Inscription" };

            // GetOrderInfo 実行
            var orderInfos = _yahooOrderInfo.GetOrderInfo(orderIds, requestFields, yahooShop);

            // Assert: 戻り値のOrderIdsが引数のOrderIdsをすべて含むか確認
            var compOrderIds = YahooOrderInfoHelper.GetOrderIdList(orderInfos);
            Assert.That(compOrderIds.Count, Is.EqualTo(orderIds.Count), "戻り値のOrderId数が入力のOrderIds数と一致しません。");
            Assert.That(orderIds.Except(compOrderIds).Any(), Is.False, "戻り値のOrderIdsが入力のOrderIdsをすべて含んでいません。");

            // Assert: リクエストFieldに対するレスポンスFieldの一致検証
            if (orderInfos.Count > 0)
            { 
               var fields = YahooOrderInfoHelper.GetOrderInfoAllFields(orderInfos);
               var exceptFields = requestFields.Except(fields).ToList();
               Assert.That(exceptFields.Any(), Is.False, $"フィールド '{string.Join(" , ", exceptFields)}' がありません。");
            }
            Console.WriteLine($"● 検査ショップ： {yahooShop.ToString()}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"注文件数： {orderInfos.Count} 件");
            Console.WriteLine("T02_GetOrderInfo_Success");
            Console.WriteLine("--------------------------------------------------");
         }
      }

      [Test]
      public void T03_RunGetOrderSearch_Success()
      {
         var endDT = DateTime.Now.AddDays(-5);
         var startDT = endDT.AddDays(-120);

         foreach (YahooShop yahooShop in Enum.GetValues(typeof(YahooShop)))
         {
            var sellerid = ApiHelpers.GetShopToken(_dbContext, yahooShop.ToString()).SellerId;
            var outputFields = string.Join(",", YahooOrderListRequestFactory.OutputFieldsDefault);
            var yahooOrderListRequest = YahooOrderListRequestFactory.ShippedOrderRequest(startDT, endDT, 1, outputFields, sellerid);

            // Test用 OrderIDsを取得
            var (totalCount, OrderIds) = _yahooOrderList.RunGetOrderSearch(yahooOrderListRequest, yahooShop);
            Assert.That(OrderIds.Count, Is.EqualTo(totalCount),
              "TotalRecordsAmountとOrderNumberListのカウントが一致しません。");
            Assert.That(OrderIds.Distinct().Count(), Is.EqualTo(OrderIds.Count), "orderIDsに重複データがあります。");

            Console.WriteLine($"● 検査ショップ： {yahooShop.ToString()}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"注文件数： {totalCount} 件");
            Console.WriteLine("T03_RunGetOrderSearch_Success：ページネーションテスト");
            Console.WriteLine("--------------------------------------------------");
         }
      }
   }
}