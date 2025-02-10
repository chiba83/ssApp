#pragma warning disable CS8618
using Newtonsoft.Json;
using ssAppModels.ApiModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppModels.AppModels;

public class YahooDefaultFields
{
   public string OrderId { get; set; } = string.Empty;         // 注文ID
   public DateTime OrderTime { get; set; }                     // 注文日時
   public string OrderStatus { get; set; } = string.Empty;     // 注文ステータス
   public int LineId { get; set; }                             // 商品ラインID
   public string ItemId { get; set; } = string.Empty;          // 商品ID
   public string SubCode { get; set; } = string.Empty;         // 商品サブコード
   public string SubCodeOption { get; set; } = string.Empty;   // 商品サブコードオプション
   public string ItemOption { get; set; } = string.Empty;      // 商品オプション
   public int Quantity { get; set; }                           // 数量
   public int ItemTaxRatio { get; set; }                       // 消費税率
   public int OriginalPrice { get; set; }                      // 値引き前の単価
   public int CouponDiscount { get; set; }                     // ストアクーポンの値引き額
   public int UnitPrice { get; set; }                          // 商品単価
   public string SellerId { get; set; } = string.Empty;        // セラーID
}

public class DailyOrderNewsYahoo: YahooDefaultFields
{
   public string ShipFirstName { get; set; } = string.Empty;   // お届け先名前
   public string ShipLastName { get; set; } = string.Empty;    // お届け先名字
   public string ShipZipCode { get; set; } = string.Empty;     // お届け先郵便番号
   public string ShipPrefecture { get; set; } = string.Empty;  // お届け先都道府県
   public string ShipCity { get; set; } = string.Empty;        // お届け先市区郡
   public string ShipAddress1 { get; set; } = string.Empty;    // お届け先住所1
   public string? ShipAddress2 { get; set; }                   // お届け先住所2
   public string ShipPhoneNumber { get; set; } = string.Empty; // お届け先電話番号
   public string BillMailAddress { get; set; } = string.Empty; // ご請求先メールアドレス
}

public static class AppModelHelpers
{
   // YahooOrderInfoのデフォルト出力フィールド一覧取得
   public static List<string> GetYahooDefaultFields()
   {
      return typeof(YahooDefaultFields)
         .GetProperties().Select(x => x.Name).ToList();
   }
   // YahooOrderInfoの出力フィールド一覧取得
   public static List<string> GetDailyOrderNewsFields()
   { 
      return typeof(DailyOrderNewsYahoo)
         .GetProperties().Select(x => x.Name).ToList();
   }
}

