using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Apps;
using ssAppTests.ssAppServices.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            var (httpResponses, orderInfo) = _setDailyOrderNews.GetYahooOrderInfoWithResponse(orderList, shop);

            // orderListのデータ件数とhttpOrderListの件数が一致すること。Response件数はGetOrderListTotalで取得
            var rowNumber = YahooApiHelpers.GetOrderListTotal(httpOrderList);
            Assert.That(orderList.Count, Is.EqualTo(rowNumber), "orderListのデータ件数とhttpResponsesの件数が一致しません。");

            /******************************************************************************/
            // DailyOrderNewsYahooSearchのフィールド値を全件チェック
            // Yahoo注文情報のフィールド値を取得
            var fieldDefinitions = YahooOrderListFieldDefinitions.FieldDefinitions;
            var fields = DailyOrderNewsModelHelper.YahooOrderSearchFields();
            int i = 0;
            foreach (var rec in orderList)
            {
               i++;
               foreach (var field in fields)
               {
                  var value = YahooApiHelpers.GetOrderListFieldValue(httpOrderList, i, field, fieldDefinitions);
                  Assert.That(rec.GetType().GetProperty(field)?.GetValue(rec), Is.EqualTo(value), $"{field}の値が一致しません。");
               }
            }

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
            fieldDefinitions = YahooOrderInfoFieldDefinitions.GetAllFields();
            var itemFieldList = YahooOrderInfoFieldDefinitions.Item.Keys.ToList();
            fields = DailyOrderNewsModelHelper.GetYahooOrderInfoFields();

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
      public void T02_RunDailyOrderNewsWorkflow()
      {
         var (DON, DONY) = _setDailyOrderNews.RunDailyOrderNewsWorkflow();
         // DONのデータ件数とDONYの件数が一致すること
         Assert.That(DON.Count, Is.EqualTo(DONY.Count), "DailyOrderNewsとDailyOrderNewsYahooのデータ件数が一致しません。");
         // DONYのデータ件数とDailyOrderNews.ShopCodeが"Yahoo"で始まる件数が一致すること
         var yahooCount = _dbContext.DailyOrderNews.Count(x => x.ShopCode.StartsWith("Yahoo"));
         Assert.That(DONY.Count, Is.EqualTo(yahooCount), "DailyOrderNewsYahooのデータ件数とDailyOrderNews.ShopCodeが\"Yahoo\"で始まる件数が一致しません。");
      }

      private (HttpResponseMessage, List<DailyOrderNewsYahooSearch>) Run_YahooOrderSearch(YahooShop yahooShop)
      {
         var (httpResponses, orderList) = _setDailyOrderNews.GetYahooOrderListWithResponse(yahooShop);
         int rowNumber = 1;
         foreach (var order in orderList)
         {
            var properties = order.GetType().GetProperties();
            var values = properties.Select(p => $"{p.Name}: {p.GetValue(order)}");
            Console.WriteLine($"Row {rowNumber}: {string.Join(", ", values)}");
            Console.WriteLine("--------------------------------------------------");
            rowNumber++;
         }
         return (httpResponses, orderList);
      }
   }
}
