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

public class DailyOrderNewsRakuten
{
   public string ZipCode { get; set; }                 // 郵便番号 (RakutenGetOrderSenderModel) ZipCode1 + ZipCode2
   public string Prefecture { get; set; }              // 都道府県 (RakutenGetOrderSenderModel)
   public string City { get; set; }                    // 郡市区 (RakutenGetOrderSenderModel)
   public string SubAddress { get; set; }              // それ以降の住所 (RakutenGetOrderSenderModel)
   public string FamilyName { get; set; }              // 姓 (RakutenGetOrderSenderModel)
   public string? FirstName { get; set; }              // 名 (RakutenGetOrderSenderModel)
   public string PhoneNumber { get; set; }             // 電話番号 (RakutenGetOrderSenderModel) PhoneNumber1 + PhoneNumber2 + PhoneNumber3
   public DateTime OrderDatetime { get; set; }         // 注文日時 (RakutenGetOrderOrderModel)
   public string OrderNumber { get; set; }             // 注文番号 (RakutenGetOrderOrderModel)
   public string ItemDetailId { get; set; }            // 商品明細ID (RakutenGetOrderItemModel)
   public int LineId { get; set; }                     // 商品ラインID：同一のOrderNumberに対し1から連番を採番する。OrderNumberが変わったら1から採番。
   public int LineCount { get; set; }                  // 商品ライン数：同一のOrderNumberカウント（ラインカウント数）をセット。
   public string ItemNumber { get; set; }              // 商品番号 (RakutenGetOrderItemModel)
   public string MerchantDefinedSkuId { get; set; }    // システム連携用SKU番号 (RakutenGetOrderskuModel) Nullの場合は、itemNumber (RakutenGetOrderItemModel)
   public string Skucode { get; set; }                 // SkuCode skuConversionによる変換後のコード
   public int Units { get; set; }                      // 個数 (RakutenGetOrderItemModel)
   public double TaxRate { get; set; }                 // 商品税率 (RakutenGetOrderItemModel)
   public int OriginalPrice { get; set; }              // オリジナル価格 (RakutenGetOrderItemModel) PriceTaxIncl * Units
   public int CouponShopPrice { get; set; }            // クーポン利用金額 (RakutenGetOrderCouponModel)  RakutenGetOrderItemModel.ItemDetailId = RakutenGetOrderCouponModel.ItemDetailId のCouponTotalPriceをセット。Nullはゼロをセット。CouponModelListはItemDetailIdに対し1:0..1
   public int TotalPrice { get; set; }                 // 合計金額 (RakutenGetOrderOrderModel)
   public int CouponTotalShopPrice { get; set; }       // 店舗発行クーポン利用額 (RakutenGetOrderOrderModel) CouponShopPrice
   public int PaymentCharge { get; set; }              // 決済手数料 (RakutenGetOrderOrderModel)
   public int AdditionalFeeOccurAmountToShop { get; set; }// 店舗負担金合計 (RakutenGetOrderOrderModel)
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
   public int CouponDiscount { get; set; }                     // ストアクーポンの値引き額
   public int UnitPrice { get; set; }                          // 商品単価
   public string SellerId { get; set; } = string.Empty;        // セラーID
}

public static class DailyOrderNewsModelHelper
{
   // YahooOrderInfoの出力フィールド一覧取得
   public static List<string> GetYahooOrderInfoFields()
   { 
      return typeof(DailyOrderNewsYahoo)
         .GetProperties().Select(x => x.Name).ToList();
   }
}
