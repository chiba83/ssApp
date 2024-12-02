#pragma warning disable CS8618, CS8602, CS8604
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api.Yahoo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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

      [Test]
      public void GetOrderInfo_ReturnsSuccess()
      {
         // Arrange: 検索条件の作成
         var searchCondition = new YahooOrderListCondition
         {
            OrderTimeFrom = DateTime.Now.AddDays(-2).ToString("yyyyMMddHHmmss"), // 修正後フォーマット
            OrderTimeTo = DateTime.Now.ToString("yyyyMMddHHmmss") // 修正後フォーマット
         };

         // Test用 OrderIDを取得
         var outputFields = new List<string> { "OrderId", "OrderTime" };
         var orderSearchResponse = _yahooOrderList.GetOrderSearch(searchCondition, outputFields, YahooShop.Yahoo_Yours);

         // GetOrderInfo の引数セット
         //var orderIds = orderSearchResponse.Where(dict => dict.ContainsKey("OrderId"))
         // .Select(dict => dict["OrderId"].ToString()).Take(5).ToList();
         var orderIds = new List<string> { "yours-ja-10050779", "yours-ja-10050811" };
         outputFields = new List<string>
            { "OrderId", "OrderTime", "OrderStatus", "LineId", "ItemId",
              "SubCode", "Quantity", "TotalPrice", "PayMethodName", "ShipFirstName",
              "IsLogin", "SellerId", "ItemOption", "Inscription" };

         // GetOrderInfo 実行
         var orderInfos = _yahooOrderInfo.GetOrderInfo(orderIds, outputFields, YahooShop.Yahoo_Yours);

         // Assert: 戻り値のOrderIdsが引数のOrderIdsをすべて含むか確認
         var returnedOrderIds = orderInfos
             .Select(orderInfo => orderInfo.ResultSet.Result.OrderInfo.Order?["OrderId"].ToString())
             .Where(orderId => !string.IsNullOrEmpty(orderId))
             .ToList();

         Assert.That(returnedOrderIds.Count, Is.EqualTo(orderIds.Count), "戻り値のOrderId数が入力のOrderIds数と一致しません。");
         Assert.That(returnedOrderIds, Is.EquivalentTo(orderIds), "戻り値のOrderIdsが入力のOrderIdsをすべて含んでいません。");

         // Assert: 各OrderInfoに対して詳細を検証
         foreach (var orderId in orderIds)
         {
            var matchingOrderInfo = orderInfos
                .FirstOrDefault(orderInfo => orderInfo.ResultSet.Result.OrderInfo.Order?["OrderId"]?.ToString() == orderId);

            Assert.That(matchingOrderInfo, Is.Not.Null, $"OrderId '{orderId}' に対応するOrderInfoが存在しません。");

            var orderDetails = matchingOrderInfo.ResultSet.Result.OrderInfo;

            // outputFieldsの各フィールドが含まれるか検証
            foreach (var field in outputFields)
            {
               if (YahooOrderInfoFieldDefinitions.Order.ContainsKey(field))
               {
                  Assert.That(orderDetails.Order.ContainsKey(field), Is.True,
                      $"OrderId '{orderId}' のOrderノードにフィールド '{field}' がありません。");
               }
               else if (YahooOrderInfoFieldDefinitions.Pay.ContainsKey(field))
               {
                  Assert.That(orderDetails.Pay.ContainsKey(field), Is.True,
                      $"OrderId '{orderId}' のPayノードにフィールド '{field}' がありません。");
               }
               else if (YahooOrderInfoFieldDefinitions.Ship.ContainsKey(field))
               {
                  Assert.That(orderDetails.Ship.ContainsKey(field), Is.True,
                      $"OrderId '{orderId}' のShipノードにフィールド '{field}' がありません。");
               }
               else if (YahooOrderInfoFieldDefinitions.Seller.ContainsKey(field))
               {
                  Assert.That(orderDetails.Seller.ContainsKey(field), Is.True,
                      $"OrderId '{orderId}' のSellerノードにフィールド '{field}' がありません。");
               }
               else if (YahooOrderInfoFieldDefinitions.Buyer.ContainsKey(field))
               {
                  Assert.That(orderDetails.Buyer.ContainsKey(field), Is.True,
                      $"OrderId '{orderId}' のBuyerノードにフィールド '{field}' がありません。");
               }
               else if (YahooOrderInfoFieldDefinitions.Detail.ContainsKey(field))
               {
                  Assert.That(orderDetails.Detail.ContainsKey(field), Is.True,
                      $"OrderId '{orderId}' のDetailノードにフィールド '{field}' がありません。");
               }
               else if (field == "ItemOption")
               {
                  // ItemOptionが存在するかのみを確認
                  Assert.That(orderDetails.Items.Any(item => item.ItemOptions != null), Is.True,
                      $"OrderId '{orderId}' のItemノードにItemOptionが存在しません。");
               }
               else if (field == "Inscription")
               {
                  // Inscriptionは1:1関係のため単一オブジェクトの存在を確認
                  Assert.That(orderDetails.Items.Any(item => item.Inscription != null), Is.True,
                      $"OrderId '{orderId}' のItemノードにInscriptionが存在しません。");
               }
               else if (YahooOrderInfoFieldDefinitions.Item.ContainsKey(field))
               {
                  // Itemノード内の動的フィールドチェック
                  Assert.That(orderDetails.Items.Any(item => item.Item.ContainsKey(field)), Is.True,
                      $"OrderId '{orderId}' のItemノードにフィールド '{field}' がありません。");
               }
               else
               {
                  Assert.Fail($"フィールド '{field}' は未定義のため検証できません。");
               }
            }
         }
      }
   }
}