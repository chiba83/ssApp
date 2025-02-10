using Azure;
using ssAppCommon.Extensions;
using ssAppModels.ApiModels;
using ssAppServices.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ssAppTests.ssAppServices.Helpers;

public static class YahooApiHelpers
{
   // HTTPレスポンスからOrderList件数を取得
   public static int GetOrderListTotal(HttpResponseMessage responses)
   {
      var responseBody = XDocument.Parse(responses.Content.ReadAsStringAsync().Result);
      return int.Parse(responseBody.Descendants("TotalCount").First().Value);
   }

   // HTTPレスポンスからOrderListのフィールド値を取得。Indexで絞り込む
   public static object GetOrderListFieldValue(HttpResponseMessage responses, int index, string field, Dictionary<string, Type> fieldDefinitions)
   {
      var fieldType = fieldDefinitions.GetValueOrDefault(field);
      var responseBody = XDocument.Parse(responses.Content.ReadAsStringAsync().Result);
      var value = responseBody.Descendants("Search").Elements("OrderInfo")
         .Where(x => (int?)x.Element("Index") == index)
         .Select(x => (string?)x.Element(field)).FirstOrDefault();
      var obj = Reflection.CreateInstance(fieldType, value);

      return obj;
   }

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
      var obj = Reflection.CreateInstance(fieldType, value);
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
            x => Reflection.CreateInstance(fieldDefinitions.GetValueOrDefault(x.Name.LocalName), x.Value)
         ) ?? new Dictionary<string, object>();

      return itemFields;
   }

   // HTTPレスポンスからItemOption関連フィールド値を取得。OrderId、LineIdで絞り込む
   public static string GetItemOptionValue(List<HttpResponseMessage> responses, string orderId, string lineId)
   {
      var responseBody = responses.Select(x => XDocument.Parse(x.Content.ReadAsStringAsync().Result));
      var orderInfo = responseBody.SelectMany(x => x.Descendants("OrderInfo"));
      var itemFields = orderInfo.FirstOrDefault(x => x.Element("OrderId")?.Value == orderId)
         ?.Descendants("Item")
         .Where(x => x.Descendants("LineId").Any(e => e.Value == lineId))
         .SelectMany(x => x.Elements().Where(e => e.Name == "ItemOption")) ?? Enumerable.Empty<XElement>();
      var itemOption = itemFields.Any() 
         ? string.Join(";", itemFields.Select(x => string.Join(",", x.Elements().Select(e => e.Value))))
         : "";

      return itemOption;
   }

   // HTTPレスポンスからInscription関連フィールド値を取得。OrderId、LineIdで絞り込む
   public static string GetInscriptionValue(List<HttpResponseMessage> responses, string orderId, string lineId)
   {
      var responseBody = responses.Select(x => XDocument.Parse(x.Content.ReadAsStringAsync().Result));
      var orderInfo = responseBody.SelectMany(x => x.Descendants("OrderInfo"));
      var itemFields = orderInfo.FirstOrDefault(x => x.Element("OrderId")?.Value == orderId)
         ?.Descendants("Item")
         .Where(x => x.Descendants("LineId").Any(e => e.Value == lineId))
         .SelectMany(x => x.Elements().Where(e => e.Name == "Inscription")) ?? Enumerable.Empty<XElement>();
      var info = itemFields.Any()
         ? string.Join(",", itemFields.Elements().Select(x => x.Value)) : "";

      return info;
   }
}
