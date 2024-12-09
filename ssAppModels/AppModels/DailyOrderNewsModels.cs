using ssAppModels.ApiModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppModels.AppModels
{
   // リクエスト仕様：検索条件（Condition）
   // https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#condition

   public class DailyOrderNewsYahooSearch
   {
      // 注文ID
      public string OrderId { get; set; } = string.Empty;
      // 注文有効フラグ
      public bool IsActive { get; set; }
      // 閲覧済みフラグ
      public bool IsSeen { get; set; }
      // 注文日時
      public DateTime OrderTime { get; set; }
      // 注文ステータス（1 : 予約中、2 : 処理中、3 : 保留、4 : キャンセル、5 : 完了）
      public int OrderStatus { get; set; }
      // 入金ステータス（0 : 未入金、1 : 入金済）
      public int PayStatus { get; set; }
      // 決済ステータス（1 : 決済申込、2 : 支払待ち、3 : 支払完了、4 : 入金待ち、5 : 決済完了、6 : キャンセル、7 : 返金、8 : 有効期限切れ、9 : 決済申込中、10 : オーソリエラー、11 : 売上取消、12 : Suicaアドレスエラー）
      public int SettleStatus { get; set; }
      // 出荷ステータス（0 : 出荷不可、1 : 出荷可、2 : 出荷処理中、3 : 出荷済み、4 : 着荷済み）
      public int ShipStatus { get; set; }
   }

   public class DailyOrderNewsYahoo
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
      public int? CouponDiscount { get; set; }                     // ストアクーポンの値引き額
      public int UnitPrice { get; set; }                          // 商品単価
      public string SellerId { get; set; } = string.Empty;        // セラーID
   }

   public static class DailyOrderNewsModelHelper
   {
      // YahooOrderSearchの出力フィールド一覧取得
      public static List<string> YahooOrderSearchFields()
      {   
         return typeof(DailyOrderNewsYahooSearch)
            .GetProperties().Select(x => x.Name).ToList();
      }

      // YahooOrderInfoの出力フィールド一覧取得
      public static List<string> GetYahooOrderInfoFields()
      { 
         return typeof(DailyOrderNewsYahoo)
            .GetProperties().Select(x => x.Name).ToList();
      }
   }

}
