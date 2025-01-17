#pragma warning disable CS8602, CS8604
using Microsoft.Extensions.DependencyInjection;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api.Rakuten;

namespace ssAppTests.ssAppServices.Api
{
   [TestFixture]
   public class RakutenOrderTests
   {
      private ServiceProvider _serviceProvider;
      private ssAppDBContext _dbContext;
      private RakutenSearchOrder _rakutenSearchOrder;
      private RakutenGetOrder _rakutenGetOrder;

      [SetUp]
      public void SetUp()
      {
         var services = new ServiceCollection();

         // appsettings.json を読み込み、StartupのConfigureServicesを流用
         var startup = new Startup();
         startup.ConfigureServices(services);

         _serviceProvider = services.BuildServiceProvider();

         _dbContext = _serviceProvider.GetRequiredService<ssAppDBContext>();
         _rakutenSearchOrder = _serviceProvider.GetRequiredService<RakutenSearchOrder>();
         _rakutenGetOrder = _serviceProvider.GetRequiredService<RakutenGetOrder>();
      }

      [TearDown]
      public void TearDown()
      {
         _dbContext?.Dispose();
         _serviceProvider?.Dispose();
      }

      [Test]
      public void T01_RakutenGetOrderTest()
      {
         // SearchOrder リクエストパラメータ
         DateTime endDate = DateTime.Now;
         DateTime startDate = endDate.AddDays(-10);
         var searchOrderParameter = RakutenSearchOrderRequestFactory.NewOrderRequest(startDate, endDate, null);
         // SearchOrder 実行
         var (response, searchOrder) = _rakutenSearchOrder.GetSearchOrderWithResponse(searchOrderParameter, RakutenShop.Rakuten_ENZO);
         Assert.That(response.IsSuccessStatusCode, Is.True, "RakutenSearchOrderリクエスト失敗");

         if (searchOrder.OrderNumberList == null || !searchOrder.OrderNumberList.Any())
         {
            Console.WriteLine($"● 検査ショップ： {RakutenShop.Rakuten_ENZO.ToString()}");
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"注文件数： 0");
            return;
         }

         // GetOrder リクエストパラメータ
         var getOrderParameter = RakutenGetOrderRequestFactory.LatestVersionRequest(searchOrder.OrderNumberList.Take(100).ToList());
         // GetOrder 実行
         var (response2, getOrder) = _rakutenGetOrder.GetOrderWithResponse(getOrderParameter, RakutenShop.Rakuten_ENZO);
         Assert.That(response2.IsSuccessStatusCode, Is.True, "RakutenGethOrderリクエスト失敗");
         // OrderNumber全件一致の検証
         Assert.That(getOrder.OrderModelList.Select(m => m.OrderNumber), Is.EquivalentTo(searchOrder.OrderNumberList), "リクエスト・レスポンスのOrderNumberに不一致があります");
         // OrderNumber件数の一致
         Assert.That(getOrder.OrderModelList.Count, Is.EqualTo(searchOrder.OrderNumberList.Count), "件数不一致");
         Console.WriteLine($"● 検査ショップ： {RakutenShop.Rakuten_ENZO.ToString()}");
         Console.WriteLine("--------------------------------------------------");
         Console.WriteLine($"注文件数： {searchOrder.OrderNumberList.Count}");
         var i = 1;
         foreach (var order in getOrder.OrderModelList)
         {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"注文番号{i++}： {order.OrderNumber}");
         }
      }

      [Test]
      public void T02_RakutenGetOrderPagesTest()
      {
         DateTime endDate = DateTime.Now.AddDays(-10);
         DateTime startDate = endDate.AddDays(-60);
         var searchOrderParameter = RakutenSearchOrderRequestFactory.ShippedOrderRequest(startDate, endDate, 1);
         var searchOrder = _rakutenSearchOrder.RunGetSearchOrder(searchOrderParameter, RakutenShop.Rakuten_ENZO);
         Assert.That(searchOrder.OrderNumberList.Count, Is.EqualTo(searchOrder.PaginationResponse.TotalRecordsAmount),
           "TotalRecordsAmountとOrderNumberListのカウントが一致しません。");
         Assert.That(searchOrder.OrderNumberList.Distinct().Count(), Is.EqualTo(searchOrder.OrderNumberList.Count), "OrderNumberListに重複データがあります。");
         Console.WriteLine($"● 検査ショップ： {RakutenShop.Rakuten_ENZO.ToString()}");
         Console.WriteLine("--------------------------------------------------");
         Console.WriteLine($"ページング全体注文件数： {searchOrder.OrderNumberList.Count}");
      }
   }
}