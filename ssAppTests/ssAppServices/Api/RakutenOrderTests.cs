#pragma warning disable CS8604
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
         var searchOrderParameter = RakutenSearchOrderRequestFactory.NewOrderRequest();
         // SearchOrder 実行
         var (response, searchOrder) = _rakutenSearchOrder.GetSearchOrderWithResponse(searchOrderParameter, RakutenShop.Rakuten_ENZO);
         Assert.That(response.IsSuccessStatusCode, Is.True, "RakutenSearchOrderリクエスト失敗");

         if (searchOrder.OrderNumberList == null || !searchOrder.OrderNumberList.Any()) return;

         // GetOrder リクエストパラメータ
         var getOrderParameter = RakutenGetOrderRequestFactory.LatestVersionRequest(searchOrder.OrderNumberList);
         // GetOrder 実行
         var (response2, getOrder) = _rakutenGetOrder.GetOrderWithResponse(getOrderParameter, RakutenShop.Rakuten_ENZO);
         Assert.That(response2.IsSuccessStatusCode, Is.True, "RakutenGethOrderリクエスト失敗");
         // 全件一致の検証
         Assert.That(getOrder.OrderModelList.Select(m => m.OrderNumber), Is.EquivalentTo(searchOrder.OrderNumberList));
      }
   }
}
