﻿#pragma warning disable CS8618
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/**************************************************************/
/*        YahooOrderListリクエスト・レスポンスのモデル           */
/**************************************************************/
// リクエスト仕様
// https://developer.yahoo.co.jp/webapi/shopping/orderList.html
// リクエスト仕様：検索条件（Condition）
// https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#condition
// リクエスト仕様：取得情報選択（Field）
// https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#field


namespace ssAppModels.ApiModels
{
   /**************************************************************/
   /*             YahooOrderListリクエストのモデルCLASS           */
   /*************************************************************/

   public class YahooOrderListRequest
   {
      [JsonProperty("Req")]
      public YahooOrderListRequestBody? Req { get; set; }
   }

   public class YahooOrderListRequestBody
   {
      [JsonProperty("Search")]
      public YahooOrderListCriteria? Search { get; set; }

      [JsonProperty("SellerId")]
      public string SellerId { get; set; } // セラーID（必須項目）
   }

   public class YahooOrderListCriteria
   {
      [JsonProperty("Result")]
      public int Result { get; set; } // 最大取得件数 /Search/Result

      [JsonProperty("Start")]
      public int Start { get; set; } // 検索開始位置 /Search/Start

      [JsonProperty("Condition")]
      public YahooOrderListCondition? Condition { get; set; } // 検索条件 /Search/Condition

      [JsonProperty("Field")]
      public string? Field { get; set; } // 出力フィールド /Search/Fields
   }

   /// <summary>
   /// Yahoo注文検索リクエスの条件項目を定義するクラス
   /// リクエスト仕様：検索条件（Condition）
   /// https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#condition
   /// </summary>
   public class YahooOrderListCondition
   {
      [JsonProperty("OrderId")] // 注文ID
      public string? OrderId { get; set; }

      [JsonProperty("Version")] // バージョン
      public string? Version { get; set; }

      [JsonProperty("OriginalOrderId")] // 受注時注文ID
      public string? OriginalOrderId { get; set; }

      [JsonProperty("ParentOrderId")] // 分割元注文ID
      public string? ParentOrderId { get; set; }

      [JsonProperty("DeviceType")] // デバイス情報
      public string? DeviceType { get; set; }

      [JsonProperty("IsActive")] // 注文有効フラグ
      public bool? IsActive { get; set; }

      [JsonProperty("IsSeen")] // 閲覧済みフラグ
      public bool? IsSeen { get; set; }

      [JsonProperty("OrderTimeFrom")] // 注文日時（開始）
      public string? OrderTimeFrom
      {
         get => _orderTimeFrom;
         set
         {
            if (!IsValidDateFormat(value))
               throw new ArgumentException("OrderTimeFrom must follow the format 'YYYYMMDDHH24MISS'.");
            _orderTimeFrom = value;
         }
      }
      private string? _orderTimeFrom;

      [JsonProperty("OrderTimeTo")] // 注文日時（終了）
      public string? OrderTimeTo
      {
         get => _orderTimeTo;
         set
         {
            if (!IsValidDateFormat(value))
               throw new ArgumentException("OrderTimeTo must follow the format 'YYYYMMDDHH24MISS'.");
            _orderTimeTo = value;
         }
      }
      private string? _orderTimeTo;

      [JsonProperty("OrderStatus")] // 注文ステータス
      public string? OrderStatus { get; set; }

      [JsonProperty("PayMethod")] // 支払い方法
      public string? PayMethod { get; set; }

      [JsonProperty("PayStatus")] // 支払いステータス
      public string? PayStatus { get; set; }

      [JsonProperty("BillFirstNameKana")] // ご請求先名前カナ
      public string? BillFirstNameKana { get; set; }

      [JsonProperty("BillLastNameKana")] // ご請求先名字カナ
      public string? BillLastNameKana { get; set; }

      [JsonProperty("ShipFirstNameKana")] // お届け先名前カナ
      public string? ShipFirstNameKana { get; set; }

      [JsonProperty("ShipLastNameKana")] // お届け先名字カナ
      public string? ShipLastNameKana { get; set; }

      [JsonProperty("ShipStatus")] // 出荷ステータス
      public string? ShipStatus { get; set; }

      [JsonProperty("ShipCompanyCode")] // 配送会社コード
      public string? ShipCompanyCode { get; set; }

      [JsonProperty("ShipInvoiceNumber1")] // 配送伝票番号１
      public string? ShipInvoiceNumber1 { get; set; }

      [JsonProperty("ShipInvoiceNumber2")] // 配送伝票番号2
      public string? ShipInvoiceNumber2 { get; set; }

      [JsonProperty("ShipDateFrom")] // 出荷日（開始）
      public string? ShipDateFrom { get; set; }

      [JsonProperty("ShipDateTo")] // 出荷日（終了）
      public string? ShipDateTo { get; set; }

      [JsonProperty("ItemId")] // 商品ID
      public string? ItemId { get; set; }

      [JsonProperty("SubCode")] // サブコード
      public string? SubCode { get; set; }

      private bool IsValidDateFormat(string? value)
      {
         if (string.IsNullOrEmpty(value)) return false;
         return Regex.IsMatch(value, @"^\d{14}$");
      }
   }

   /**************************************************************/
   /*            YahooOrderListレスポンスのモデルCLASS            */
   /*************************************************************/
   
   public class Result
   {
      public string Status { get; set; } // ステータス (例: OK)
      public Search Search { get; set; } // 検索結果情報
   }

   public class Search
   {
      public int TotalCount { get; set; } // 該当件数
      public List<YahooOrderListOrderInfo> OrderInfo { get; set; } = new(); // 注文情報リスト
   }

   /******************************************************************/
   /* YahooOrderListレスポンスの動的な選択フィールドと型を管理するクラス */
   /*****************************************************************/

   /// <summary>
   /// 取得情報の選択仕様（Field）
   /// https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#field
   /// </summary>
   public class YahooOrderListOrderInfo
   {
      public Dictionary<string, object> Fields { get; set; } = new();

      /// <summary>
      /// 型情報付きフィールド辞書
      /// キーがフィールド名、値がフィールドの型
      /// </summary>
      public static readonly Dictionary<string, Type> FieldDefinitions = new()
      {
         { "OrderId", typeof(string) }, // 注文ID
         { "Version", typeof(int) },    // バージョン
         { "OriginalOrderId", typeof(string) }, // 受注時注文ID
         { "ParentOrderId", typeof(string) },   // 分割元注文ID
         { "DeviceType", typeof(int) }, // デバイス情報
         { "IsActive", typeof(bool) },  // 注文有効フラグ
         { "IsSeen", typeof(bool) },    // 閲覧済みフラグ
         { "IsSplit", typeof(bool) },   // 分割フラグ
         { "IsRoyalty", typeof(bool) }, // 課金フラグ
         { "IsSeller", typeof(bool) },  // 管理者注文フラグ
         { "IsAffiliate", typeof(bool) }, // アフィリエイトフラグ
         { "IsRatingB2s", typeof(bool) }, // 評価フラグ（Buyer⇒Seller）
         { "OrderTime", typeof(DateTime) }, // 注文日時
         { "ExistMultiReleaseDate", typeof(bool) }, // 複数発売日あり
         { "ReleaseDate", typeof(DateTime) }, // 注文（最長）発売日
         { "LastUpdateTime", typeof(DateTime) }, // 最終更新日時
         { "Suspect", typeof(int) }, // 悪戯フラグ
         { "OrderStatus", typeof(int) }, // 注文ステータス
         { "StoreStatus", typeof(int) }, // ストアステータス
         { "RoyaltyFixTime", typeof(DateTime) }, // 課金確定日時
         { "PrintSlipFlag", typeof(bool) }, // 注文伝票出力有無
         { "PrintDeliveryFlag", typeof(bool) }, // 納品書出力有無
         { "PrintBillFlag", typeof(bool) }, // 請求書出力有無
         { "BuyerCommentsFlag", typeof(bool) }, // バイヤーコメント有無
         { "PayStatus", typeof(int) }, // 入金ステータス
         { "SettleStatus", typeof(int) }, // 決済ステータス
         { "PayType", typeof(int) }, // 支払い分類
         { "PayMethod", typeof(string) }, // 支払い方法
         { "PayMethodName", typeof(string) }, // 支払い方法名称
         { "PayDate", typeof(DateTime) }, // 入金日
         { "SettleId", typeof(string) }, // 決済ID
         { "UseWallet", typeof(bool) }, // ウォレット利用有無
         { "NeedBillSlip", typeof(bool) }, // 請求書有無
         { "NeedDetailedSlip", typeof(bool) }, // 明細書有無
         { "NeedReceipt", typeof(bool) }, // 領収書有無
         { "BillFirstName", typeof(string) }, // ご請求先名前
         { "BillFirstNameKana", typeof(string) }, // ご請求先名前カナ
         { "BillLastName", typeof(string) }, // ご請求先名字
         { "BillLastNameKana", typeof(string) }, // ご請求先名字カナ
         { "BillPrefecture", typeof(string) }, // ご請求先都道府県
         { "ShipStatus", typeof(int) }, // 出荷ステータス
         { "ShipMethod", typeof(string) }, // 配送方法
         { "ShipMethodName", typeof(string) }, // 配送方法名
         { "ShipRequestDate", typeof(DateTime) }, // 配送希望日
         { "ShipRequestTime", typeof(string) }, // 配送希望時間
         { "ShipNotes", typeof(string) }, // 配送メモ
         { "ShipCompanyCode", typeof(int) }, // 配送会社
         { "ReceiveShopCode", typeof(string) }, // 受取店舗コード
         { "ShipInvoiceNumber1", typeof(string) }, // 配送伝票番号１
         { "ShipInvoiceNumber2", typeof(string) }, // 配送伝票番号２
         { "ShipInvoiceNumberEmptyReason", typeof(string) }, // 伝票番号なし理由
         { "ShipUrl", typeof(string) }, // 配送会社URL
         { "ArriveType", typeof(int) }, // きょうつく、あすつく
         { "ShipDate", typeof(DateTime) }, // 出荷日
         { "NeedGiftWrap", typeof(bool) }, // ギフト包装有無
         { "NeedGiftWrapMessage", typeof(bool) }, // ギフトメッセージ有無
         { "NeedGiftWrapPaper", typeof(bool) }, // のし有無
         { "ShipFirstName", typeof(string) }, // お届け先名前
         { "ShipFirstNameKana", typeof(string) }, // お届け先名前カナ
         { "ShipLastName", typeof(string) }, // お届け先名字
         { "ShipLastNameKana", typeof(string) }, // お届け先名字カナ
         { "ShipPrefecture", typeof(string) }, // お届け先都道府県
         { "PayCharge", typeof(decimal) }, // 手数料
         { "ShipCharge", typeof(decimal) }, // 送料
         { "GiftWrapCharge", typeof(decimal) }, // ギフト包装料
         { "Discount", typeof(decimal) }, // 値引き
         { "GiftCardDiscount", typeof(decimal) }, // 商品券利用額
         { "UsePoint", typeof(decimal) }, // 利用ポイント合計
         { "TotalPrice", typeof(decimal) }, // 合計金額
         { "RefundTotalPrice", typeof(decimal) }, // 返金合計金額
         { "UsePointFixDate", typeof(DateTime) }, // 利用ポイント確定日
         { "IsUsePointFix", typeof(bool) }, // 利用ポイント確定有無
         { "IsGetPointFixAll", typeof(bool) }, // 全付与ポイント確定有無
         { "SellerId", typeof(string) }, // セラーID
         { "IsLogin", typeof(bool) }, // Yahoo! JAPAN IDログイン有無
         { "PayNo", typeof(string) }, // 支払番号
         { "PayNoIssueDate", typeof(DateTime) }, // 支払番号発行日時
         { "SellerType", typeof(int) }, // セラー種別
         { "IsPayManagement", typeof(bool) }, // 代金支払い管理注文
         { "ArrivalDate", typeof(DateTime) }, // 着荷日
         { "TotalMallCouponDiscount", typeof(decimal) }, // モールクーポン値引き額
         { "IsReadOnly", typeof(int) }, // 読み取り専用
         { "IsApplePay", typeof(bool) }, // ApplePay利用有無
         { "IsFirstClassDrugIncludes", typeof(bool) }, // 第1類医薬品フラグ
         { "IsFirstClassDrugAgreement", typeof(bool) }, // 第1類医薬品承諾フラグ
         { "IsWelcomeGiftIncludes", typeof(bool) }, // 無料プレゼント含有フラグ
         { "ReceiveSatelliteType", typeof(int) }, // 自宅外配送受取種別
         { "ShipInstructType", typeof(int) }, // 出荷指示区分
         { "ShipInstructStatus", typeof(int) }, // 出荷指示ステータス
         { "YamatoCoopStatus", typeof(int) }, // ヤマト連携ステータス
         { "ReceiveShopType", typeof(int) }, // 店頭注文種別
         { "ReceiveShopName", typeof(string) }, // 配送元店頭名
         { "ExcellentDelivery", typeof(int) }, // 優良配送フラグ
         { "IsEazy", typeof(bool) }, // EAZY注文フラグ
         { "EazyDeliveryCode", typeof(int) }, // EAZYコード
         { "EazyDeliveryName", typeof(string) }, // EAZY受け取り場所名
         { "FraudHoldStatus", typeof(int) }, // 不正保留ステータス
         { "PublicationTime", typeof(DateTime) }, // 公開日時
         { "IsYahooAuctionOrder", typeof(bool) }, // Yahoo!オークション併売フラグ
         { "YahooAuctionMerchantId", typeof(string) }, // Yahoo!オークション管理番号
         { "YahooAuctionId", typeof(string) }, // オークションID
         { "IsYahooAuctionDeferred", typeof(bool) }, // Yahoo!オークション購入後決済フラグ
         { "YahooAuctionCategoryType", typeof(int) }, // Yahoo!オークションカテゴリ種別
         { "YahooAuctionBidType", typeof(int) }, // Yahoo!オークション落札種別
         { "YahooAuctionBundleType", typeof(int) }, // Yahoo!オークション同梱タイプ")
         { "ItemYahooAucId", typeof(string) }, // オークションID")
         { "ItemYahooAucMerchantId", typeof(string) }, // Yahoo!オークション管理番号")
         { "PayMethodChangeDeadline", typeof(DateTime) }, // 支払い方法変更期限")
         { "IsPayMethodChangePossible", typeof(bool) }, // 支払い方法変更可能フラグ")
         { "YourTimesaleDiscount", typeof(decimal) }, // あなただけのタイムセール値引価格")
         { "GoodStoreStatus", typeof(int) }, // 優良店判定")
         { "CurrentGoodStoreBenefitApply", typeof(int) }, // 注文時点の優良店特典適応状態")
         { "CurrentPromoPkgApply", typeof(int) }, // 注文時点のプラン適応状況")
         { "IsSubscription", typeof(bool) }, // 定期購入フラグ")
         { "SubscriptionId", typeof(string) }, // 定期購入親ID")
         { "SubscriptionContinueCount", typeof(int) }, // 定期購入継続回数")
         { "LineGiftOrderId", typeof(string) }, // LINE注文ID")
         { "IsLineGiftOrder", typeof(bool) }, // LINE注文")
         { "IsLineGiftShippable", typeof(bool) }, // LINE出荷可能フラグ")
         { "ShippingDeadline", typeof(DateTime) }, // LINE発送期限")
         { "LineGiftCharge", typeof(decimal) }, // LINE手数料")
         { "TotalImmediateBonusAmount", typeof(decimal) }, // 特典の一部利用合計額")
         { "TotalImmediateBonusRatio", typeof(decimal) }, // 特典の一部利用合計割合")
         { "SocialGiftType", typeof(int) } // ソーシャルギフトタイプ")
      };
   }
}
