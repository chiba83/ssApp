#pragma warning disable CS8618
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

/**************************************************************/
/*               YahooOrderInfoリクエストのモデル              */
/*************************************************************/
// リクエスト仕様
// https://developer.yahoo.co.jp/webapi/shopping/orderInfo.html
// リクエスト仕様：取得情報選択（Field）
// https://developer.yahoo.co.jp/webapi/shopping/orderInfo.html/#field

namespace ssAppModels.ApiModels
{
   /**************************************************************/
   /*             YahooOrderInfoリクエストのモデルCLASS           */
   /*************************************************************/

   // リクエスト全体を表すクラス
   public class YahooOrderInfoMRequest
   {
      public YahooOrderInfoRequestBody Req { get; set; }
   }

   // リクエストボディ
   public class YahooOrderInfoRequestBody
   {
      public YahooOrderInfoTarget Target { get; set; }
      public string SellerId { get; set; }
   }

   // リクエストターゲット
   public class YahooOrderInfoTarget
   {
      public string OrderId { get; set; }
      public string Field { get; set; }
   }

   /**************************************************************/
   /*            YahooOrderInfoレスポンスのモデルCLASS            */
   /*************************************************************/

   public class YahooOrderInfoResponse
   {
      public YahooOrderInfoResultSet ResultSet { get; set; }
   }

   public class YahooOrderInfoResultSet
   {
      public int TotalResultsAvailable { get; set; }
      public int TotalResultsReturned { get; set; }
      public int FirstResultPosition { get; set; }
      public YahooOrderInfoResult Result { get; set; }
   }

   public class YahooOrderInfoResult
   {
      public string Status { get; set; }
      public YahooOrderInfoDynamic OrderInfo { get; set; }
   }

   public class YahooOrderInfoDynamic
   {
      public Dictionary<string, object>? Order { get; set; } = new();
      public Dictionary<string, object>? Pay { get; set; } = new();
      public Dictionary<string, object>? Ship { get; set; } = new();
      public Dictionary<string, object>? Seller { get; set; } = new();
      public Dictionary<string, object>? Buyer { get; set; } = new();
      public Dictionary<string, object>? Detail { get; set; } = new();
      public List<YahooOrderInfoItem>? Items { get; set; } = new();
   }

   public class YahooOrderInfoItem
   {
      public Dictionary<string, object>? Item { get; set; } = new();
      public List<YahooOrderInfoItemOption>? ItemOptions { get; set; } = new();
      public YahooOrderInfoInscription? Inscription { get; set; }
   }

   public class YahooOrderInfoItemOption
   {
      public int? Index { get; set; }
      public string? Name { get; set; }
      public string? Value { get; set; }
      public int? Price { get; set; }
   }

   public class YahooOrderInfoInscription
   {
      public int? Index { get; set; }
      public string? Name { get; set; }
      public string? Value { get; set; }
   }

   /******************************************************************/
   /* YahooOrderInfoレスポンスの動的な選択フィールドと型を管理するクラス */
   /*****************************************************************/

   /// <summary>
   /// 取得情報の選択仕様（Field）
   /// https://developer.yahoo.co.jp/webapi/shopping/orderInfo.html/#field
   /// </summary>
   public static class YahooOrderInfoFieldDefinitions
   {
      /// <summary>
      /// 型情報付きフィールド辞書
      /// キーがフィールド名、値がフィールドの型
      /// </summary>

      // Payノードのフィールド辞書
      public static readonly Dictionary<string, Type> Pay = new()
      {
         // 入金および決済関連
         { "PayStatus", typeof(string) }, // 入金ステータス
         { "SettleStatus", typeof(string) }, // 決済ステータス
         { "PayType", typeof(string) }, // 支払い分類
         { "PayKind", typeof(string) }, // 支払い種別
         { "PayMethod", typeof(string) }, // 支払い方法
         { "PayMethodName", typeof(string) }, // 支払い方法名称
         { "SellerHandlingCharge", typeof(int) }, // ストア負担決済手数料
         { "PayActionTime", typeof(DateTime) }, // 支払い日時
         { "PayDate", typeof(DateTime) }, // 入金日
         { "PayNotes", typeof(string) }, // 入金処理備考
         { "SettleId", typeof(string) }, // 決済ID

         // カード関連
         { "CardBrand", typeof(string) }, // カード種別
         { "CardNumber", typeof(string) }, // クレジットカード番号
         { "CardNumberLast4", typeof(string) }, // カード番号下4けた
         { "CardExpireYear", typeof(int) }, // カード有効期限（年）
         { "CardExpireMonth", typeof(int) }, // カード有効期限（月）
         { "CardPayType", typeof(string) }, // カード支払い区分
         { "CardHolderName", typeof(string) }, // カード名義人姓名（独自カード用）
         { "CardPayCount", typeof(int) }, // カード支払回数
         { "CardBirthDay", typeof(DateTime) }, // カード生年月日
         { "UseYahooCard", typeof(bool) }, // Yahoo! JAPAN JCBカード利用有無

         // ウォレット関連
         { "UseWallet", typeof(bool) }, // ウォレット利用有無

         // 請求書および領収書関連
         { "NeedBillSlip", typeof(bool) }, // 請求書有無
         { "NeedDetailedSlip", typeof(bool) }, // 明細書有無
         { "NeedReceipt", typeof(bool) }, // 領収書有無

         // 年齢確認関連
         { "AgeConfirmField", typeof(string) }, // 年齢確認フィールド名
         { "AgeConfirmValue", typeof(int) }, // 年齢確認入力値
         { "AgeConfirmCheck", typeof(bool) }, // 年齢確認チェック有無

         // ご請求先情報
         { "BillFirstName", typeof(string) }, // ご請求先名前
         { "BillFirstNameKana", typeof(string) }, // ご請求先名前カナ
         { "BillLastName", typeof(string) }, // ご請求先名字
         { "BillLastNameKana", typeof(string) }, // ご請求先名字カナ
         { "BillZipCode", typeof(string) }, // ご請求先郵便番号
         { "BillPrefecture", typeof(string) }, // ご請求先都道府県
         { "BillPrefectureKana", typeof(string) }, // ご請求先都道府県フリガナ
         { "BillCity", typeof(string) }, // ご請求先市区郡
         { "BillCityKana", typeof(string) }, // ご請求先市区郡フリガナ
         { "BillAddress1", typeof(string) }, // ご請求先住所1
         { "BillAddress1Kana", typeof(string) }, // ご請求先住所1フリガナ
         { "BillAddress2", typeof(string) }, // ご請求先住所2
         { "BillAddress2Kana", typeof(string) }, // ご請求先住所2フリガナ
         { "BillPhoneNumber", typeof(string) }, // ご請求先電話番号
         { "BillEmgPhoneNumber", typeof(string) }, // ご請求先電話番号（緊急）
         { "BillMailAddress", typeof(string) }, // ご請求先メールアドレス
         { "BillSection1Field", typeof(string) }, // ご請求先所属1フィールド名
         { "BillSection1Value", typeof(string) }, // ご請求先所属1入力情報
         { "BillSection2Field", typeof(string) }, // ご請求先所属2フィールド名
         { "BillSection2Value", typeof(string) }, // ご請求先所属2入力情報

         // 支払番号関連
         { "PayNo", typeof(string) }, // 支払番号
         { "PayNoIssueDate", typeof(DateTime) }, // 支払番号発行日時

         // 確認番号および期限関連
         { "ConfirmNumber", typeof(string) }, // 確認番号
         { "PaymentTerm", typeof(DateTime) }, // 支払期限日時

         // ApplePay関連
         { "IsApplePay", typeof(bool) } // ApplePay利用有無

      };

      // Shipノードのフィールド辞書
      public static readonly Dictionary<string, Type> Ship = new()
      {
         // 支払い方法変更関連
         { "PayMethodChangeDeadline", typeof(DateTime) }, // 支払い方法変更期限
         { "PayMethodChangeDoneDate", typeof(DateTime) }, // 支払い方法変更完了日時
         { "IsPayMethodChangePossible", typeof(bool) }, // 支払い方法変更可能フラグ

         // 配送方法関連
         { "ShipStatus", typeof(string) }, // 出荷ステータス
         { "ShipMethod", typeof(string) }, // 配送方法
         { "ShipMethodName", typeof(string) }, // 配送方法名称

         // 配送希望関連
         { "ShipRequestDate", typeof(DateTime) }, // 配送希望日
         { "ShipRequestTime", typeof(string) }, // 配送希望時間
         { "ShipNotes", typeof(string) }, // 配送メモ

         // 配送会社関連
         { "ShipCompanyCode", typeof(int) }, // 配送会社コード
         { "ShipInvoiceNumber1", typeof(string) }, // 配送伝票番号1
         { "ShipInvoiceNumber2", typeof(string) }, // 配送伝票番号2
         { "ShipInvoiceNumberEmptyReason", typeof(string) }, // 伝票番号なし理由
         { "ShipUrl", typeof(string) }, // 配送会社URL

         // 日付関連
         { "ShipDate", typeof(DateTime) }, // 出荷日
         { "ArrivalDate", typeof(DateTime) }, // 着荷日
         { "ShippingDeadline", typeof(DateTime) }, // 発送期限
         { "DeliveryDate", typeof(DateTime) }, // お届け指定日

         // その他
         { "DeliveryStatus", typeof(int) }, // 配送ステータス
         { "ShipContactlessDeliveryCode", typeof(string) }, // 配送会社置き場所コード
         { "ShipCompanyReceivedDatetime", typeof(DateTime) }, // 配送会社荷受け日

         // オプション関連
         { "Option1Field", typeof(string) }, // オプションフィールド1
         { "Option1Type", typeof(string) }, // オプションタイプ1
         { "Option1Value", typeof(string) }, // オプション値1
         { "Option2Field", typeof(string) }, // オプションフィールド2
         { "Option2Type", typeof(string) }, // オプションタイプ2
         { "Option2Value", typeof(string) }, // オプション値2

         // ギフト関連
         { "NeedGiftWrap", typeof(bool) }, // ギフト包装有無
         { "GiftWrapCode", typeof(int) }, // ギフト包装コード
         { "GiftWrapMessage", typeof(string) }, // ギフトメッセージ
         { "GiftWrapName", typeof(string) }, // 名入れ

         // お届け先関連
         { "ShipFirstName", typeof(string) }, // お届け先名前
         { "ShipLastName", typeof(string) }, // お届け先名字
         { "ShipZipCode", typeof(string) }, // お届け先郵便番号
         { "ShipPrefecture", typeof(string) }, // お届け先都道府県
         { "ShipCity", typeof(string) }, // お届け先市区郡
         { "ShipAddress1", typeof(string) }, // お届け先住所1
         { "ShipAddress2", typeof(string) }, // お届け先住所2
         { "ShipPhoneNumber", typeof(string) }, // お届け先電話番号

      };

      // Buyerノードのフィールド辞書
      public static readonly Dictionary<string, Type> Buyer = new()
      {
         { "IsLogin", typeof(bool) }, // Yahoo! JAPAN IDログイン有無
         { "FspLicenseCode", typeof(string) }, // FSPライセンスコード（スタークラブの項目）
         { "FspLicenseName", typeof(string) }, // FSPライセンス名（スタークラブの項目）
         { "GuestAuthId", typeof(string) } // ゲストユニークキー（vwoidc）
      };

      // Sellerノードのフィールド辞書
      public static readonly Dictionary<string, Type> Seller = new()
      {
         { "SellerId", typeof(string) }, // セラーID
         { "LineGiftAccount", typeof(string) } // LINEアカウント（LINEショップID）
      };

      // Itemノードのフィールド辞書
      public static readonly Dictionary<string, Type> Item = new()
      {
         // 商品基本情報関連
          { "LineId", typeof(int) }, // 商品ラインID
          { "ItemId", typeof(string) }, // 商品ID
          { "Title", typeof(string) }, // 商品名
          { "SubCode", typeof(string) }, // 商品サブコード
          { "SubCodeOption", typeof(string) }, // 商品サブコードオプション

          // オプション情報関連
          { "ItemOption", typeof(YahooOrderInfoItemOption) }, // 商品オプション
          { "Inscription", typeof(YahooOrderInfoInscription) }, // インスクリプション

          // 商品状態関連
          { "IsUsed", typeof(bool) }, // 中古フラグ
          { "ImageId", typeof(string) }, // 商品画像ID
          { "IsTaxable", typeof(bool) }, // 課税対象
          { "ItemTaxRatio", typeof(int) }, // 消費税率
          { "Jan", typeof(string) }, // JANコード
          { "ProductId", typeof(string) }, // 製品コード
          { "CategoryId", typeof(int) }, // プロダクトカテゴリID

          // 価格および数量関連
          { "AffiliateRatio", typeof(string) }, // アフィリエイト料率
          { "UnitPrice", typeof(int) }, // 商品単価
          { "NonTaxUnitPrice", typeof(int) }, // 商品税抜単価
          { "Quantity", typeof(int) }, // 数量
          { "PointAvailQuantity", typeof(int) }, // ポイント対象数量
          { "ReleaseDate", typeof(DateTime) }, // 発売日
          { "OriginalPrice", typeof(int) }, // 値引き前の単価
          { "OriginalNum", typeof(int) }, // ライン分割前の数量

          // ポイント関連
          { "PointFspCode", typeof(int) }, // 商品別ポイントコード
          { "PointRatioY", typeof(int) }, // 付与ポイント倍率（Yahoo!JAPAN負担）
          { "PointRatioSeller", typeof(int) }, // 付与ポイント倍率（ストア負担）
          { "UnitGetPoint", typeof(int) }, // 単位付与ポイント数
          { "IsGetPointFix", typeof(bool) }, // 付与ポイント確定フラグ
          { "GetPointFixDate", typeof(DateTime) }, // 付与ポイント確定日
          { "StoreBonusRatioSeller", typeof(int) }, // 付与ストアPayPayボーナス倍率（Seller負担）
          { "UnitGetStoreBonus", typeof(int) }, // 単位付与ストアPayPayボーナス数
          { "IsGetStoreBonusFix", typeof(bool) }, // 付与ストアPayPayボーナス確定フラグ
          { "GetStoreBonusFixDate", typeof(DateTime) }, // 付与ストアPayPayボーナス確定日

          // クーポン関連
          { "CouponData", typeof(string) }, // ストアクーポン
          { "CouponDiscount", typeof(int) }, // ストアクーポンの値引き額
          { "CouponUseNum", typeof(int) }, // ストアクーポン適用枚数

          // 発送関連
          { "LeadTimeText", typeof(string) }, // 発送日テキスト
          { "LeadTimeStart", typeof(DateTime) }, // 発送日スタート
          { "LeadTimeEnd", typeof(DateTime) }, // 発送日エンド

          // その他
          { "PriceType", typeof(int) }, // 価格種別
          { "PickAndDeliveryCode", typeof(string) }, // 梱包バーコード情報
          { "YamatoUndeliverableReason", typeof(string) }, // 配送不可理由
          { "PointBaseUnitPrice", typeof(int) }, // ポイント計算基準額
          { "MallCouponData", typeof(string) }, // モールクーポン値引き詳細情報json
          { "YourTimesaleDiscount", typeof(int) }, // あなただけのタイムセール値引価格
          { "SubscriptionPointRatio", typeof(int) }, // 定期購入ポイント倍率
          { "ChannelGoodsPlanId", typeof(int) }, // チャンネルグッズプランID
          { "ChannelGoodsPlanName", typeof(string) } // 保証プラン名称
      };

      // Orderノードのフィールド辞書
      public static readonly Dictionary<string, Type> Order = new()
      {
         // 注文の基本情報
         { "OrderId", typeof(string) }, // 注文ID
         { "Version", typeof(int) }, // バージョン
         { "ParentOrderId", typeof(string) }, // 分割元注文ID
         { "ChildOrderId", typeof(string) }, // 分割後注文ID（カンマ区切り）

         // デバイスおよびキャリア情報
         { "DeviceType", typeof(string) }, // デバイス種別
         { "MobileCarrierName", typeof(string) }, // 携帯キャリア名

         // 状態およびフラグ関連
         { "IsSeen", typeof(bool) }, // 閲覧済みフラグ
         { "IsSplit", typeof(bool) }, // 分割フラグ
         { "CancelReason", typeof(int) }, // キャンセル理由
         { "CancelReasonDetail", typeof(string) }, // キャンセル理由詳細
         { "IsRoyalty", typeof(bool) }, // ロイヤルティフラグ
         { "IsRoyaltyFix", typeof(bool) }, // ロイヤルティ確定フラグ
         { "IsSeller", typeof(bool) }, // 管理者注文フラグ
         { "IsAffiliate", typeof(bool) }, // アフィリエイトフラグ
         { "IsRatingB2s", typeof(bool) }, // 評価フラグ
         { "NeedSnl", typeof(bool) }, // SNLオプトイン

         // 時間情報
         { "OrderTime", typeof(DateTime) }, // 注文日時
         { "LastUpdateTime", typeof(DateTime) }, // 最終更新日時
         { "RoyaltyFixTime", typeof(DateTime) }, // ロイヤルティ確定日時
         { "SendConfirmTime", typeof(DateTime) }, // 注文確認メール送信時刻
         { "SendPayTime", typeof(DateTime) }, // 支払完了メール送信時刻
         { "PrintSlipTime", typeof(DateTime) }, // 注文伝票出力時刻
         { "PrintDeliveryTime", typeof(DateTime) }, // 納品書出力時刻
         { "PrintBillTime", typeof(DateTime) }, // 請求書出力時刻

         // その他詳細情報
         { "Suspect", typeof(string) }, // いたずらフラグ
         { "SuspectMessage", typeof(string) }, // いたずらメッセージ
         { "OrderStatus", typeof(string) }, // 注文ステータス
         { "StoreStatus", typeof(int) }, // ストアステータス
         { "BuyerComments", typeof(string) }, // ストアへの要望
         { "SellerComments", typeof(string) }, // セラーコメント
         { "Notes", typeof(string) }, // ストア内メモ
         { "OperationUser", typeof(string) }, // 更新者
         { "Referer", typeof(string) }, // 参照元URL（リファラー）
         { "EntryPoint", typeof(string) }, // 入力ポイント
         { "Clink", typeof(string) }, // 調査用リンク
         { "HistoryId", typeof(int) }, // 履歴ID
         { "UsageId", typeof(int) }, // クーポン利用ID
         { "UseCouponData", typeof(string) }, // 使用したクーポン情報
         { "TotalCouponDiscount", typeof(int) }, // クーポン合計値引き額
         { "ShippingCouponFlg", typeof(int) }, // 送料無料クーポン利用有無
         { "ShippingCouponDiscount", typeof(int) }, // 送料無料クーポン適用時の送料値引き額
         { "CampaignPoints", typeof(string) }, // 後付与ポイント内訳
         { "IsMultiShip", typeof(bool) }, // 複数配送注文フラグ
         { "MultiShipId", typeof(string) }, // 複数配送注文ID
         { "IsReadOnly", typeof(int) }, // 読み取り専用
         { "IsFirstClassDrugIncludes", typeof(bool) }, // 第1類医薬品フラグ
         { "IsFirstClassDrugAgreement", typeof(bool) }, // 第1類医薬品承諾フラグ
         { "IsWelcomeGiftIncludes", typeof(string) }, // 無料プレゼント(ウェルカムギフト)含有フラグ
         { "FraudHoldStatus", typeof(int) }, // 不正保留ステータス
         { "PublicationTime", typeof(DateTime) }, // orderList公開日時
         { "GoodStoreStatus", typeof(string) }, // 優良店判定
         { "CurrentGoodStoreBenefitApply", typeof(string) }, // 注文時点の優良店特典適応状態
         { "CurrentPromoPkgApply", typeof(string) }, // 注文時点のプラン適応状況
         { "LineGiftOrderId", typeof(string) }, // LINE注文ID
         { "IsLineGiftOrder", typeof(bool) }, // LINE注文フラグ
         { "ImmediateBonus", typeof(string) }, // 特典の一部利用内訳
         { "SlowlyShipPoint", typeof(int) }, // おトク指定便付与ポイント数
         { "SlowlyShipPointFixDate", typeof(DateTime) }, // おトク指定便ポイント確定日付
         { "IsSlowlyShipPointFix", typeof(bool) }, // おトク指定便ポイント確定フラグ
         { "CampaignGiftCards", typeof(string) }, // 獲得商品券情報
         { "OverlapOrderResult", typeof(int) }, // 二重注文判定結果
         { "FirstOrderDoneDate", typeof(DateTime) }, // 初回完了日
         { "DonationId", typeof(string) }, // 買って応援便注文
         { "SocialGiftType", typeof(string) }, // ソーシャルギフトタイプ
         { "IsVip", typeof(bool) } // VIP特典対象注文
      };

      // 明細 (Detail) ノードのフィールド辞書
      public static readonly Dictionary<string, Type> Detail = new()
      {
         // 手数料および送料関連
         { "PayCharge", typeof(int) }, // 手数料
         { "ShipCharge", typeof(int) }, // 送料
         { "GiftWrapCharge", typeof(int) }, // ギフト包装料

         // 金額調整および値引き関連
         { "Discount", typeof(int) }, // 値引き
         { "Adjustments", typeof(int) }, // 調整額
         { "SettleAmount", typeof(int) }, // 決済金額
         { "UsePoint", typeof(int) }, // 利用ポイント合計
         { "GiftCardDiscount", typeof(int) }, // 商品券利用額

         // 合計金額および入金関連
         { "TotalPrice", typeof(int) }, // 合計金額
         { "SettlePayAmount", typeof(int) }, // 入金金額

         // ポイントおよびボーナス確定関連
         { "IsGetPointFixAll", typeof(bool) }, // 全付与ポイント確定有無
         { "TotalMallCouponDiscount", typeof(int) }, // モールクーポン値引き額
         { "IsGetStoreBonusFixAll", typeof(bool) }, // 全付与ストアPayPayボーナス確定フラグ

         // LINEおよび特典関連
         { "LineGiftCharge", typeof(int) }, // LINE手数料
         { "TotalImmediateBonusAmount", typeof(int) }, // 特典の一部利用合計額
         { "TotalImmediateBonusRatio", typeof(int) }, // 特典の一部利用合計割合

         // 支払い関連
         { "PayMethodAmount", typeof(int) }, // 支払い金額
         { "CombinedPayMethodAmount", typeof(int) } // 併用支払い金額
      };

      // 全ノードをマージする辞書（動的利用を想定）
      public static Dictionary<string, Type> GetAllFields()
      {
         return Pay
             .Concat(Ship).Concat(Buyer).Concat(Seller)
             .Concat(Item).Concat(Order).Concat(Detail)
             .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
      }

      // フィールドの所属グループを取得する
      public static string GetGroupByType(Type fieldType)
      {
         if (Item.Values.Contains(fieldType))
            return nameof(Item);
         if (Detail.Values.Contains(fieldType))
            return nameof(Detail);
         if (Order.Values.Contains(fieldType))
            return nameof(Order);
         if (Ship.Values.Contains(fieldType))
            return nameof(Ship);
         if (Pay.Values.Contains(fieldType))
            return nameof(Pay);
         if (Buyer.Values.Contains(fieldType))
            return nameof(Buyer);
         if (Seller.Values.Contains(fieldType))
            return nameof(Seller);

         return $"Unknown : {fieldType.Name}";
      }

      // グループ一覧を取得する
      public static List<string> GetGroups()
      {
         return new List<string> 
         {
            nameof(Item), nameof(Detail), nameof(Order), nameof(Ship),
            nameof(Pay), nameof(Buyer), nameof(Seller)
         };
      }
   }
}