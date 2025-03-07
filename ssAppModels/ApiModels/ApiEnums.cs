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

// 注文ステータスを管理する列挙型
public enum OrderStatus
{
   NewOrder,      // 新規注文 (処理待ち)　Y・R共通
   Packing,       // 梱包処理中 Y・R共通
   Shipping,      // 出荷処理中（出荷日・追跡番号設定） Yのみ
   Shipped,       // 出荷完了（配送完了） Y・R共通
   Present,       // レビュープレゼント対象期間の注文情報 Y・R共通
}

// テーブル更新モードを管理する列挙型
public enum UpdateMode
{
   Insert,   // 新規追加
   Replace   // 既存データを削除して新規追加
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
