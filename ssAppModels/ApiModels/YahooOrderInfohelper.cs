using ssAppModels.ApiModels;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ssAppModels.ApiModels
{
   public static class YahooOrderInfoHelper
   {
      // OrderIdリストを取得
      public static List<string?> GetOrderIdList(List<YahooOrderInfoResponse> orderInfoResponses)
      {
         return orderInfoResponses
            .Select(x => x.ResultSet.Result.OrderInfo.Order?["OrderId"]?.ToString())
            .ToList();
      }

      // OrderInfoのItemフィールドを取得
      public static List<string> GetOrderInfoItemFields(List<YahooOrderInfoResponse> orderInfoResponses)
      {
         return orderInfoResponses
            .SelectMany(x => x.ResultSet.Result.OrderInfo.Items ?? Enumerable.Empty<YahooOrderInfoItem>())
            .SelectMany(x => x.Item?.Keys ?? Enumerable.Empty<string>()).Distinct().ToList()
            ?? Enumerable.Empty<string>().ToList();
      }

      // OrderInfoのItemOptionsフィールドを取得
      public static List<string> GetOrderInfoItemOptionsFields(List<YahooOrderInfoResponse> orderInfoResponses)
      {
         return orderInfoResponses
            .SelectMany(x => x.ResultSet.Result.OrderInfo.Items ?? Enumerable.Empty<YahooOrderInfoItem>())
            .Any(x => x.ItemOptions != null)
            ? new List<string> { "ItemOption" }
            : Enumerable.Empty<string>().ToList();
      }

      // OrderInfoのInscriptionフィールドを取得
      public static List<string> GetOrderInfoInscriptionFields(List<YahooOrderInfoResponse> orderInfoResponses)
      {
         return orderInfoResponses
            .SelectMany(x => x.ResultSet.Result.OrderInfo.Items ?? Enumerable.Empty<YahooOrderInfoItem>())
            .Any(x => x.Inscription != null)
            ? new List<string> { "Inscription" }
            : Enumerable.Empty<string>().ToList();
      }

      public static List<string> GetOrderInfoFields(List<YahooOrderInfoResponse> orderInfoResponses)
      {
         var groups = YahooOrderInfoFieldDefinitions.GetGroups();

         // Item以外のフィールドを取得
         return groups.SelectMany(group =>
            orderInfoResponses.SelectMany(x =>
            {
               // 対象のプロパティを取得
               var propertyValue = x.ResultSet.Result.OrderInfo
                  .GetType().GetProperty(group)?
                  .GetValue(x.ResultSet.Result.OrderInfo);

               // プロパティが Dictionary<string, object> である場合、キーを取得
               if (propertyValue is Dictionary<string, object> dictionary)
                  return dictionary.Keys; // 辞書のキーを返す
               return Enumerable.Empty<string>(); // プロパティが対象外の場合は空のリスト
            }
         )).Distinct().ToList(); // 重複を排除してリスト化
      }

      public static List<string> GetOrderInfoAllFields(List<YahooOrderInfoResponse> orderInfoResponses)
      {
         var fields = GetOrderInfoFields(orderInfoResponses);
         var itemFields = GetOrderInfoItemFields(orderInfoResponses);
         var itemOptionsFields = GetOrderInfoItemOptionsFields(orderInfoResponses);
         var inscriptionFields = GetOrderInfoInscriptionFields(orderInfoResponses);

         // fieldsマージ
         return fields.Concat(itemFields).Concat(itemOptionsFields)
            .Concat(inscriptionFields).ToList();
      }
   }
}
