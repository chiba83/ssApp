#pragma warning disable CS8618, CS8629
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api.Yahoo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ssApptests.ssAppServices.Api
{
   [TestFixture]
   public class YahooOrderListTests
   {
      private ServiceProvider _serviceProvider;
      private ssAppDBContext _dbContext;
      private YahooOrderList _yahooOrderList;

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
      }

      [TearDown]
      public void TearDown()
      {
         _dbContext?.Dispose();
         _serviceProvider?.Dispose();
      }

      [Test]
      public void GetOrderSearchAsync_ValidRequest_ReturnsSuccess()
      {
         // Arrange: 検索条件の作成
         var searchCondition = new YahooOrderListCondition
         {
            //Seller_Id = "yours-ja",
            OrderTimeFrom = DateTime.Now.ToString("yyyyMMddHHmmss"), // 修正後フォーマット
            OrderTimeTo = DateTime.Now.AddDays(1).ToString("yyyyMMddHHmmss") // 修正後フォーマット
         };

         // 出力フィールドの選択
         var outputFields = new List<string> { "OrderId", "OrderTime" };

         // Act: Yahoo注文検索APIを呼び出す
         var result = _yahooOrderList.GetOrderSearch(searchCondition, outputFields, YahooShop.Yahoo_Yours);

         // Assert: レスポンス結果を検証
         Assert.That(result.IsSuccessStatusCode, Is.True, "注文検索APIは成功しませんでした。");
      }
   }
}
