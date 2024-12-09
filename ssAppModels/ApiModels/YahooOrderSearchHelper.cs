using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppModels.ApiModels
{
   public static class YahooOrderSearchHelper
   {
      // OrderIdリストを取得
      public static List<string?> GetOrderIdList(YahooOrderListResult responses)
      {
         return responses.Search.OrderInfo
            .Select(x => x.Fields["OrderId"].ToString()).ToList();
      }
   }
}
