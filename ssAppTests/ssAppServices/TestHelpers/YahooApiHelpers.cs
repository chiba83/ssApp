using Azure;
using ssAppModels.ApiModels;
using ssAppServices.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ssAppTests.ssAppServices.Helpers
{
   public static class YahooApiHelpers
   {
      // HTTPレスポンスから注文情報のItem件数を取得
      public static int GetOrderInfoItemCount(List<HttpResponseMessage> responses)
      {
         var responseBody = responses.Select(x => XDocument.Parse(x.Content.ReadAsStringAsync().Result));
         return responseBody.Descendants("Item").Count();
      }

      // HTTPレスポンスから注文情報のフィールド値を取得。OrderIdで絞り込む
      public static object GetOrderInfoFieldValue(List<HttpResponseMessage> responses, string orderId, string field, Dictionary<string, Type> fieldDefinitions) 
      {
         var responseBody = responses.Select(x => XDocument.Parse(x.Content.ReadAsStringAsync().Result));
         var orderInfo = responseBody.SelectMany(x => x.Descendants("OrderInfo"));
         var node = orderInfo.FirstOrDefault(x => x.Element("OrderId")?.Value == orderId);
         var fieldType = fieldDefinitions.GetValueOrDefault(field);
         var value = node?.Descendants(field).FirstOrDefault()?.Value;
         var obj = ApiHelpers.CreateInstance(fieldType, value);
         return obj;
      }

      // HTTPレスポンスから注文情報のItem関連フィールド値を取得。OrderId、LineIdで絞り込む
      public static Dictionary<string, object> GetOrderInfoItemValues(List<HttpResponseMessage> responses, string orderId, string lineId, Dictionary<string, Type> fieldDefinitions)
      {
         var responseBody = responses.Select(x => XDocument.Parse(x.Content.ReadAsStringAsync().Result));
         var orderInfo = responseBody.SelectMany(x => x.Descendants("OrderInfo"));
         var itemFields = orderInfo.FirstOrDefault(x => x.Element("OrderId")?.Value == orderId)
            ?.Descendants("Item")
            .Where(x => x.Descendants("LineId").Any(e => e.Value == lineId))
            .SelectMany(x => x.Elements().Where(e => e.Name != "ItemOption"))
            .ToDictionary(
               x => x.Name.LocalName,
               x => ApiHelpers.CreateInstance(fieldDefinitions.GetValueOrDefault(x.Name.LocalName), x.Value)
            ) ?? new Dictionary<string, object>();

         return itemFields;
      }

      private static object SetItemOptionValue(XElement itemOption)
      {
         // itemoptionノードはオカレンス。オカレンスノード配下の全エレメントをstringで文字結合してobjectで返す
         return itemOption.Elements().Select(x => x.Value).Aggregate((x, y) => x + y);


      }
   }
}
