#pragma warning disable CS8603
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppModels.ApiModels;

// ショップコードを管理する列挙型
public enum YahooShop
{
   Yahoo_LARAL,
   Yahoo_Yours
}
public enum RakutenShop
{
   Rakuten_ENZO
}
public enum MallShop
{
   Yahoo_LARAL,
   Yahoo_Yours,
   Rakuten_ENZO
}
public enum Mall
{
   Yahoo,
   Rakuten
}

// OAuth 2.0 のグラントタイプを管理する列挙型
public enum GrantType
{
   authorization_code,
   refresh_token
}

public static class MallShopConverter
{
   /// <summary>
   /// YahooShop, RakutenShop から MallShop へ変換
   /// </summary>
   public static MallShop ToMallShop(Enum shop)
   {
      return Enum.GetValues(typeof(MallShop))
         .Cast<MallShop>()
         .FirstOrDefault(m => m.ToString() == shop.ToString());
   }
   /// <summary>
   /// MallShop から RakutenShop / YahooShop へ変換（ジェネリック対応）
   /// </summary>
   public static T ToSpecificShop<T>(MallShop mallShop) where T : Enum
   {
      string shopName = mallShop.ToString();
      foreach (T shop in Enum.GetValues(typeof(T)).Cast<T>())
      {
         if (shop.ToString() == shopName) return shop;
      }
      return default; // 該当なしの場合
   }
}
