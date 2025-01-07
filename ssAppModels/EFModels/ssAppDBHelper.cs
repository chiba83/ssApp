using ssAppModels.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppModels.EFModels
{
   public static class ssAppDBHelper
   {
      /// <summary>
      /// 指定されたShopCodeに対応するShopTokenを取得
      /// </summary>
      public static ShopToken GetShopToken(ssAppDBContext dbContext, string shopCode)
      {
         return dbContext.ShopTokens.FirstOrDefault(st => st.ShopCode == shopCode)
             ?? throw new Exception($"指定されたShopCode（{shopCode}）に対応するShopTokenが見つかりません。");
      }

      /// <summary>
      /// 指定されたMall に対応するSellerId, ShopCodeを取得
      /// </summary>
      public static Dictionary<string, string> GetShopTokenSeller(ssAppDBContext dbContext, Mall mall)
      {
         return dbContext.ShopTokens
            .Where(x => x.ShopCode.StartsWith(mall.ToString()))
            .ToDictionary(x => x.SellerId, x => x.ShopCode)
            ?? throw new Exception($"指定されたMall（{mall.ToString()}）に対応するShopTokenが見つかりません。");
      }

   }
}
