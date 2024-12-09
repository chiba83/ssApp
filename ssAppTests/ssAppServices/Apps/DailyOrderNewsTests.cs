using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ssAppModels.ApiModels;
using ssAppModels.AppModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api.Yahoo;
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
      private YahooOrderList _yahooOrderList;
      private YahooOrderInfo _yahooOrderInfo;
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
         _yahooOrderList = _serviceProvider.GetRequiredService<YahooOrderList>();
         _yahooOrderInfo = _serviceProvider.GetRequiredService<YahooOrderInfo>();
         _setDailyOrderNews = _serviceProvider.GetRequiredService<SetDailyOrderNews>();
      }

      [TearDown]
      public void TearDown()
      {
         _dbContext?.Dispose();
         _serviceProvider?.Dispose();
      }

      [Test]
      public void T01_YahooOrderSearch_Success()
      {
         var orderList = Run_YahooOrderSearch();
         Assert.That(orderList.Any(x => x.OrderId.IsNullOrEmpty()), Is.False, $"NullのOrderIdがあります。");
      }

      [Test]
      public void T02_YahooOrderInfo_Success()
      {
         var orderList = Run_YahooOrderSearch().ToList();
         var (httpResponses, orderInfo) = _setDailyOrderNews.GetYahooOrderInfoWithResponse(orderList);

         /******************************************************************************/
         // データ件数チェック
         // orderInfoのデータ件数とhttpResponsesの件数が一致すること。Response件数はGetOrderInfoItemCountで取得
         Assert.That(orderInfo.Any(), Is.True, $"DailyOrderNewsYahooデータがありません。");
         var rowNumber = YahooApiHelpers.GetOrderInfoItemCount(httpResponses);
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
            string lineId =  rec.LineId.ToString();
            var itemValues = YahooApiHelpers.GetOrderInfoItemValues(httpResponses, rec.OrderId, lineId, fieldDefinitions);

            foreach (var field in fields)
            {
               //field名がItemOptionの場合はスキップ
               if (field == "ItemOption") continue;

               if (itemFieldList.Contains(field))
                  Assert.That(rec.GetType().GetProperty(field).GetValue(rec), Is.EqualTo(itemValues[field]), $"{field}の値が一致しません。");
               else
               {
                  var value = YahooApiHelpers.GetOrderInfoFieldValue(httpResponses, rec.OrderId, field, fieldDefinitions);
                  Assert.That(rec.GetType().GetProperty(field).GetValue(rec), Is.EqualTo(value), $"{field}の値が一致しません。");
               }
            }
         }
      }

      private List<DailyOrderNewsYahooSearch> Run_YahooOrderSearch()
      {
         var orderList = _setDailyOrderNews.GetYahooOrderList();
         int rowNumber = 1;
         foreach (var order in orderList)
         {
            var properties = order.GetType().GetProperties();
            var values = properties.Select(p => $"{p.Name}: {p.GetValue(order)}");
            Console.WriteLine($"Row {rowNumber}: {string.Join(", ", values)}");
            Console.WriteLine("--------------------------------------------------");
            rowNumber++;
         }
         return orderList;
      }
   }
}
