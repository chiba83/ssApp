#pragma warning disable CS8618, CS8602, CS8604
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api.Yahoo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            OrderTimeFrom = DateTime.Now.AddDays(-15).ToString("yyyyMMddHHmmss"), // 修正後フォーマット
            OrderTimeTo = DateTime.Now.ToString("yyyyMMddHHmmss") // 修正後フォーマット
         };

         // 出力フィールドの選択
         var outputFields = new List<string> { "OrderId", "OrderTime" };

         // Act: Yahoo注文検索APIを呼び出す
         var (httpResponse, parsedData) = _yahooOrderList.GetOrderSearchWithResponse(searchCondition, outputFields, YahooShop.Yahoo_Yours);

         // レスポンス総件数とコレクション件数が一致していること
         var totalCount = int.Parse(XDocument.Parse(httpResponse.Content.ReadAsStringAsync().Result)
                              .Descendants("TotalCount")
                              .FirstOrDefault()?.Value ?? "0");
         Assert.That(parsedData.Count, Is.EqualTo(totalCount), "レスポンス総件数とパース済みコレクション件数が一致しません。");

         // リクエストしたフィールドがレスポンスにすべて含まれていること
         var missingFields = parsedData
             .SelectMany(record => outputFields.Where(field => !record.ContainsKey(field)))
             .Distinct().ToList();
         Assert.That(missingFields, Is.Empty, $"レスポンスコレクションに含まれていないフィールド: {string.Join(", ", missingFields)}");

         // 注文日時の範囲チェック
         var orderTimeFromDateTime = DateTime.ParseExact(searchCondition.OrderTimeFrom, "yyyyMMddHHmmss", null);
         var orderTimeToDateTime = DateTime.ParseExact(searchCondition.OrderTimeTo, "yyyyMMddHHmmss", null);
         // parsedData 内の OrderTime をチェック
         var invalidOrders = parsedData
             .Where(d => d.ContainsKey("OrderTime") && d["OrderTime"] != null)
             .Select(d => DateTime.Parse(d["OrderTime"].ToString()))
             .Where(orderTime => orderTime < orderTimeFromDateTime || orderTime > orderTimeToDateTime);
         // 範囲外のデータがあればNG
         Assert.That(invalidOrders.Any(), Is.False, "OrderTime が検索条件の範囲外のデータが含まれています。");
      }
   }
}
