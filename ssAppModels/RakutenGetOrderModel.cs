#pragma warning disable CS8618
using Newtonsoft.Json;

/**************************************************************/
/*        RakutenGetOrderリクエスト・レスポンスのモデル       */
/**************************************************************/
// 仕様
// https://webservice.rms.rakuten.co.jp/merchant-portal/view/ja/common/1-1_service_index/rakutenpayorderapi/getorder

namespace ssAppModels.ApiModels
{
   /**************************************************************/
   /*            RakutenGetOrderリクエストのモデルCLASS            */
   /**************************************************************/
   public class RakutenGetOrderRequest
   {
      [JsonProperty("orderNumberList")]
      public List<string> OrderNumberList { get; set; }        // 注文番号リスト
      [JsonProperty("version")]
      public int Version { get; set; }                         // バージョン番号
   }

   // orderNumberList 注文番号リスト
   //    100: 注文確認待ち 200: 楽天処理中 300: 発送待ち 400: 変更確定待ち 500: 発送済
   // version バージョン番号
   //    7: SKU対応 8: 配送品質向上制度対応 9: 置き配対応
   public class RakutenGetOrderRequestFactory
   {
      // 共通リクエスト生成メソッド
      private static RakutenGetOrderRequest CreateRequest(List<string> orderNumberList, int version)
      {
         return new RakutenGetOrderRequest
         {
            OrderNumberList = orderNumberList,
            Version = version
         };
      }
      // 最新バージョンリクエスト
      public static RakutenGetOrderRequest LatestVersionRequest(List<string> orderNumberList)
      {
         return CreateRequest(orderNumberList, 9);
      }
   }

   /**************************************************************/
   /*            RakutenGetOrderレスポンスのモデルCLASS            */
   /**************************************************************/
   public class RakutenGetOrderResponse
   {
      [JsonProperty("messageModelList")]
      public List<RakutenGetOrderMessageModel> MessageModelList { get; set; } // メッセージモデルリスト
      [JsonProperty("orderModelList")]
      public List<RakutenGetOrderOrderModel>? OrderModelList { get; set; }    // 受注情報モデルリスト
      [JsonProperty("version")]
      public int Version { get; set; }                         // バージョン番号
   }

   public class RakutenGetOrderMessageModel
   {
      [JsonProperty("messageType")]
      public string MessageType { get; set; }                  // メッセージ種別
      [JsonProperty("messageCode")]
      public string MessageCode { get; set; }                  // メッセージコード
      [JsonProperty("message")]
      public string Message { get; set; }                      // メッセージ
      [JsonProperty("orderNumber")]
      public string? OrderNumber { get; set; }                 // 注文番号
   }

   public class RakutenGetOrderOrderModel
   {
      [JsonProperty("orderNumber")]
      public string OrderNumber { get; set; }                  // 注文番号
      [JsonProperty("orderProgress")]
      public int OrderProgress { get; set; }                   // ステータス
      [JsonProperty("subStatusId")]
      public int? SubStatusId { get; set; }                    // サブステータスID
      [JsonProperty("subStatusName")]
      public string? SubStatusName { get; set; }               // サブステータス
      [JsonProperty("orderDatetime")]
      public DateTime OrderDatetime { get; set; }              // 注文日時
      [JsonProperty("shopOrderCfmDatetime")]
      public DateTime? ShopOrderCfmDatetime { get; set; }      // 注文確認日時
      [JsonProperty("orderFixDatetime")]
      public DateTime? OrderFixDatetime { get; set; }          // 注文確定日時
      [JsonProperty("shippingInstDatetime")]
      public DateTime? ShippingInstDatetime { get; set; }      // 発送指示日時
      [JsonProperty("shippingCmplRptDatetime")]
      public DateTime? ShippingCmplRptDatetime { get; set; }   // 発送完了報告日時
      [JsonProperty("cancelDueDate")]
      public DateTime? CancelDueDate { get; set; }             // キャンセル期限日
      [JsonProperty("deliveryDate")]
      public DateTime? DeliveryDate { get; set; }              // お届け日指定
      [JsonProperty("shippingTerm")]
      public int? ShippingTerm { get; set; }                   // お届け時間帯
      [JsonProperty("remarks")]
      public string? Remarks { get; set; }                     // コメント
      [JsonProperty("giftCheckFlag")]
      public int GiftCheckFlag { get; set; }                   // ギフト配送希望フラグ
      [JsonProperty("severalSenderFlag")]
      public int SeveralSenderFlag { get; set; }               // 複数送付先フラグ
      [JsonProperty("equalSenderFlag")]
      public int EqualSenderFlag { get; set; }                 // 送付先一致フラグ
      [JsonProperty("isolatedIslandFlag")]
      public int IsolatedIslandFlag { get; set; }              // 離島フラグ
      [JsonProperty("rakutenMemberFlag")]
      public int RakutenMemberFlag { get; set; }               // 楽天会員フラグ
      [JsonProperty("carrierCode")]
      public int CarrierCode { get; set; }                     // 利用端末
      [JsonProperty("emailCarrierCode")]
      public int EmailCarrierCode { get; set; }                // メールキャリアコード
      [JsonProperty("orderType")]
      public int OrderType { get; set; }                       // 注文種別
      [JsonProperty("reserveNumber")]
      public string? ReserveNumber { get; set; }               // 申込番号
      [JsonProperty("reserveDeliveryCount")]
      public int? ReserveDeliveryCount { get; set; }           // 申込お届け回数
      [JsonProperty("cautionDisplayType")]
      public int CautionDisplayType { get; set; }              // 警告表示タイプ
      [JsonProperty("cautionDisplayDetailType")]
      public int? CautionDisplayDetailType { get; set; }       // 警告表示タイプ詳細
      [JsonProperty("rakutenConfirmFlag")]
      public int RakutenConfirmFlag { get; set; }              // 楽天確認中フラグ
      [JsonProperty("goodsPrice")]
      public int GoodsPrice { get; set; }                      // 商品合計金額
      [JsonProperty("goodsTax")]
      public int GoodsTax { get; set; }                        // 外税合計
      [JsonProperty("postagePrice")]
      public int PostagePrice { get; set; }                    // 送料合計
      [JsonProperty("deliveryPrice")]
      public int DeliveryPrice { get; set; }                   // 代引料合計
      [JsonProperty("paymentCharge")]
      public int PaymentCharge { get; set; }                   // 決済手数料合計
      [JsonProperty("paymentChargeTaxRate")]
      public double PaymentChargeTaxRate { get; set; }         // 決済手続税率
      [JsonProperty("totalPrice")]
      public int TotalPrice { get; set; }                      // 合計金額
      [JsonProperty("requestPrice")]
      public int RequestPrice { get; set; }                    // 請求金額
      [JsonProperty("couponAllTotalPrice")]
      public int CouponAllTotalPrice { get; set; }             // クーポン利用総額
      [JsonProperty("couponShopPrice")]
      public int CouponShopPrice { get; set; }                 // 店舗発行クーポン利用額
      [JsonProperty("couponOtherPrice")]
      public int CouponOtherPrice { get; set; }                // 楽天発行クーポン利用額
      [JsonProperty("additionalFeeOccurAmountToUser")]
      public int AdditionalFeeOccurAmountToUser { get; set; }  // 注文者負担金合計
      [JsonProperty("additionalFeeOccurAmountToShop")]
      public int AdditionalFeeOccurAmountToShop { get; set; }  // 店舗負担金合計
      [JsonProperty("asurakuFlag")]
      public int AsurakuFlag { get; set; }                     // あす楽希望フラグ
      [JsonProperty("drugFlag")]
      public int DrugFlag { get; set; }                        // 医薬品受注フラグ
      [JsonProperty("dealFlag")]
      public int DealFlag { get; set; }                        // 楽天スーパーDEAL商品受注フラグ
      [JsonProperty("membershipType")]
      public int MembershipType { get; set; }                  // メンバーシッププログラム受注タイプ
      [JsonProperty("memo")]
      public string? Memo { get; set; }                        // ひとことメモ
      [JsonProperty("operator")]
      public string? Operator { get; set; }                    // 担当者
      [JsonProperty("mailPlugSentence")]
      public string? MailPlugSentence { get; set; }            // メール差込文
      [JsonProperty("modifyFlag")]
      public int ModifyFlag { get; set; }                      // 購入履歴修正有無フラグ
      [JsonProperty("receiptIssueCount")]
      public long ReceiptIssueCount { get; set; }              // 領収書発行回数
      [JsonProperty("receiptIssueHistoryList")]
      public List<DateTime>? ReceiptIssueHistoryList { get; set; }      // 領収書発行履歴リスト
      [JsonProperty("ordererModel")]
      public RakutenGetOrderOrdererModelLv3 OrdererModel { get; set; }  // 注文者モデル
      [JsonProperty("settlementModel")]
      public RakutenGetOrderSettlementModel? SettlementModel { get; set; } // 支払方法モデル
      [JsonProperty("deliveryModel")]
      public RakutenGetOrderDeliveryModel DeliveryModel { get; set; }   // 配送方法モデル
      [JsonProperty("pointModel")]
      public RakutenGetOrderPointModel? PointModel { get; set; }        // ポイントモデル
      [JsonProperty("wrappingModel1")]
      public RakutenGetOrderWrappingModel? WrappingModel1 { get; set; } // ラッピングモデル1
      [JsonProperty("wrappingModel2")]
      public RakutenGetOrderWrappingModel? WrappingModel2 { get; set; } // ラッピングモデル2
      [JsonProperty("packageModelList")]
      public List<RakutenGetOrderPackageModel> PackageModelList { get; set; } // 送付先モデルリスト
      [JsonProperty("couponModelList")]
      public List<RakutenGetOrderCouponModel>? CouponModelList { get; set; }  // クーポンモデルリスト
      [JsonProperty("changeReasonModelList")]
      public List<RakutenGetOrderChangeReasonModel>? ChangeReasonModelList { get; set; } // 変更・キャンセルモデルリスト
      [JsonProperty("taxSummaryModelList")]
      public List<RakutenGetOrderTaxSummaryModel>? TaxSummaryModelList { get; set; }     // 税情報モデルリスト
      [JsonProperty("dueDateModelList")]
      public List<RakutenGetOrderDueDateModel>? DueDateModelList { get; set; }           // 期限日モデルリスト
      [JsonProperty("deliveryCertPrgFlag")]
      public int DeliveryCertPrgFlag { get; set; }            // 最強翌日配送フラグ
      [JsonProperty("oneDayOperationFlag")]
      public int OneDayOperationFlag { get; set; }            // 注文当日出荷フラグ
   }

   public class RakutenGetOrderOrdererModelLv3
   {
      [JsonProperty("zipCode1")]
      public string ZipCode1 { get; set; }                     // 郵便番号1
      [JsonProperty("zipCode2")]
      public string ZipCode2 { get; set; }                     // 郵便番号2
      [JsonProperty("prefecture")]
      public string Prefecture { get; set; }                   // 都道府県
      [JsonProperty("city")]
      public string City { get; set; }                         // 郡市区
      [JsonProperty("subAddress")]
      public string SubAddress { get; set; }                   // それ以降の住所
      [JsonProperty("familyName")]
      public string FamilyName { get; set; }                   // 姓
      [JsonProperty("firstName")]
      public string FirstName { get; set; }                    // 名
      [JsonProperty("familyNameKana")]
      public string? FamilyNameKana { get; set; }              // 姓カナ
      [JsonProperty("firstNameKana")]
      public string? FirstNameKana { get; set; }               // 名カナ
      [JsonProperty("phoneNumber1")]
      public string? PhoneNumber1 { get; set; }                // 電話番号1
      [JsonProperty("phoneNumber2")]
      public string? PhoneNumber2 { get; set; }                // 電話番号2
      [JsonProperty("phoneNumber3")]
      public string? PhoneNumber3 { get; set; }                // 電話番号3
      [JsonProperty("emailAddress")]
      public string EmailAddress { get; set; }                 // メールアドレス
      [JsonProperty("sex")]
      public string? Sex { get; set; }                         // 性別
      [JsonProperty("birthYear")]
      public int? BirthYear { get; set; }                      // 誕生日(年)
      [JsonProperty("birthMonth")]
      public int? BirthMonth { get; set; }                     // 誕生日(月)
      [JsonProperty("birthDay")]
      public int? BirthDay { get; set; }                       // 誕生日(日)
   }

   public class RakutenGetOrderSettlementModel
   {
      [JsonProperty("settlementMethodCode")]
      public int SettlementMethodCode { get; set; }            // 支払方法コード
      [JsonProperty("settlementMethod")]
      public string SettlementMethod { get; set; }             // 支払方法名
      [JsonProperty("rpaySettlementFlag")]
      public int RpaySettlementFlag { get; set; }              // 楽天市場の共通決済手段フラグ
      [JsonProperty("cardName")]
      public string? CardName { get; set; }                    // クレジットカード種類
      [JsonProperty("cardNumber")]
      public string? CardNumber { get; set; }                  // クレジットカード番号
      [JsonProperty("cardOwner")]
      public string? CardOwner { get; set; }                   // クレジットカード名義人
      [JsonProperty("cardYm")]
      public string? CardYm { get; set; }                      // クレジットカード有効期限
      [JsonProperty("cardPayType")]
      public int? CardPayType { get; set; }                    // クレジットカード支払い方法
      [JsonProperty("cardInstallmentDesc")]
      public string? CardInstallmentDesc { get; set; }         // クレジットカード支払い回数
   }

   public class RakutenGetOrderDeliveryModel
   {
      [JsonProperty("deliveryName")]
      public string DeliveryName { get; set; }                 // 配送方法
      [JsonProperty("deliveryClass")]
      public int? DeliveryClass { get; set; }                  // 配送区分
   }

   public class RakutenGetOrderPointModel
   {
      [JsonProperty("usedPoint")]
      public int UsedPoint { get; set; }                       // ポイント利用額
   }

   public class RakutenGetOrderWrappingModel
   {
      [JsonProperty("title")]
      public int Title { get; set; }                           // ラッピングタイトル
      [JsonProperty("name")]
      public string Name { get; set; }                         // ラッピング名
      [JsonProperty("price")]
      public int? Price { get; set; }                          // 料金
      [JsonProperty("includeTaxFlag")]
      public int IncludeTaxFlag { get; set; }                  // 税込別
      [JsonProperty("deleteWrappingFlag")]
      public int DeleteWrappingFlag { get; set; }              // ラッピング削除フラグ
      [JsonProperty("taxRate")]
      public double TaxRate { get; set; }                      // ラッピング税率
      [JsonProperty("taxPrice")]
      public int TaxPrice { get; set; }                        // ラッピング税額
   }

   public class RakutenGetOrderPackageModel
   {
      [JsonProperty("basketId")]
      public string BasketId { get; set; }                     // 送付先ID
      [JsonProperty("postagePrice")]
      public int PostagePrice { get; set; }                    // 送料
      [JsonProperty("postageTaxRate")]
      public double PostageTaxRate { get; set; }               // 送料税率
      [JsonProperty("deliveryPrice")]
      public int DeliveryPrice { get; set; }                   // 代引料
      [JsonProperty("deliveryTaxRate")]
      public double DeliveryTaxRate { get; set; }              // 代引料税率
      [JsonProperty("goodsTax")]
      public int GoodsTax { get; set; }                        // 送付先外税合計
      [JsonProperty("goodsPrice")]
      public int GoodsPrice { get; set; }                      // 商品合計金額
      [JsonProperty("totalPrice")]
      public int TotalPrice { get; set; }                      // 合計金額
      [JsonProperty("noshi")]
      public string? Noshi { get; set; }                       // のし
      [JsonProperty("packageDeleteFlag")]
      public int PackageDeleteFlag { get; set; }               // 送付先モデル削除フラグ
      [JsonProperty("senderModel")]
      public RakutenGetOrderSenderModel SenderModel { get; set; } // 送付者モデル
      [JsonProperty("itemModelList")]
      public List<RakutenGetOrderItemModel> ItemModelList { get; set; } // 商品モデルリスト
      [JsonProperty("shippingModelList")]
      public List<RakutenGetOrderShippingModel>? ShippingModelList { get; set; } // 発送モデルリスト
      [JsonProperty("deliveryCvsModel")]
      public RakutenGetOrderDeliveryCVSModel? DeliveryCvsModel { get; set; } // コンビニ配送モデル
      [JsonProperty("defaultDeliveryCompanyCode")]
      public string DefaultDeliveryCompanyCode { get; set; }   // 購入時配送会社
      [JsonProperty("dropOffFlag")]
      public int DropOffFlag { get; set; }                     // 置き配フラグ
      [JsonProperty("dropOffLocation")]
      public string? DropOffLocation { get; set; }             // 置き配場所
   }

   public class RakutenGetOrderSenderModel
   {
      [JsonProperty("zipCode1")]
      public string ZipCode1 { get; set; }                     // 郵便番号1
      [JsonProperty("zipCode2")]
      public string ZipCode2 { get; set; }                     // 郵便番号2
      [JsonProperty("prefecture")]
      public string Prefecture { get; set; }                   // 都道府県
      [JsonProperty("city")]
      public string City { get; set; }                         // 郡市区
      [JsonProperty("subAddress")]
      public string SubAddress { get; set; }                   // それ以降の住所
      [JsonProperty("familyName")]
      public string FamilyName { get; set; }                   // 姓
      [JsonProperty("firstName")]
      public string? FirstName { get; set; }                   // 名
      [JsonProperty("familyNameKana")]
      public string? FamilyNameKana { get; set; }              // 姓カナ
      [JsonProperty("firstNameKana")]
      public string? FirstNameKana { get; set; }               // 名カナ
      [JsonProperty("phoneNumber1")]
      public string? PhoneNumber1 { get; set; }                // 電話番号1
      [JsonProperty("phoneNumber2")]
      public string? PhoneNumber2 { get; set; }                // 電話番号2
      [JsonProperty("phoneNumber3")]
      public string? PhoneNumber3 { get; set; }                // 電話番号3
      [JsonProperty("isolatedIslandFlag")]
      public int IsolatedIslandFlag { get; set; }              // 離島フラグ
   }

   public class RakutenGetOrderItemModel
   {
      [JsonProperty("itemDetailId")]
      public string ItemDetailId { get; set; }                 // 商品明細ID
      [JsonProperty("itemName")]
      public string ItemName { get; set; }                     // 商品名
      [JsonProperty("itemId")]
      public string ItemId { get; set; }                       // 商品ID
      [JsonProperty("itemNumber")]
      public string? ItemNumber { get; set; }                  // 商品番号
      [JsonProperty("manageNumber")]
      public string ManageNumber { get; set; }                 // 商品管理番号
      [JsonProperty("price")]
      public int Price { get; set; }                           // 単価
      [JsonProperty("units")]
      public int Units { get; set; }                           // 個数
      [JsonProperty("includePostageFlag")]
      public int IncludePostageFlag { get; set; }              // 送料込別
      [JsonProperty("includeTaxFlag")]
      public int IncludeTaxFlag { get; set; }                  // 税込別
      [JsonProperty("includeCashOnDeliveryPostageFlag")]
      public int IncludeCashOnDeliveryPostageFlag { get; set; } // 代引手数料込別
      [JsonProperty("selectedChoice")]
      public string? SelectedChoice { get; set; }              // 項目・選択肢
      [JsonProperty("pointRate")]
      public int PointRate { get; set; }                       // ポイント倍率
      [JsonProperty("pointType")]
      public int PointType { get; set; }                       // ポイントタイプ
      [JsonProperty("inventoryType")]
      public int InventoryType { get; set; }                   // 在庫タイプ
      [JsonProperty("delvdateInfo")]
      public string? DelvdateInfo { get; set; }                // 納期情報
      [JsonProperty("restoreInventoryFlag")]
      public int RestoreInventoryFlag { get; set; }            // 在庫連動オプション
      [JsonProperty("dealFlag")]
      public int DealFlag { get; set; }                        // 楽天スーパーDEAL商品フラグ
      [JsonProperty("drugFlag")]
      public int DrugFlag { get; set; }                        // 医薬品フラグ
      [JsonProperty("deleteItemFlag")]
      public int DeleteItemFlag { get; set; }                  // 商品削除フラグ
      [JsonProperty("taxRate")]
      public double TaxRate { get; set; }                      // 商品税率
      [JsonProperty("priceTaxIncl")]
      public int PriceTaxIncl { get; set; }                    // 商品毎税込価格
      [JsonProperty("isSingleItemShipping")]
      public int IsSingleItemShipping { get; set; }            // 単品配送フラグ
      [JsonProperty("skuModelList")]
      public List<RakutenGetOrderskuModel> SkuModelList { get; set; } // SKUモデルリスト
   }

   public class RakutenGetOrderShippingModel
   {
      [JsonProperty("shippingDetailId")]
      public string ShippingDetailId { get; set; }             // 発送明細ID
      [JsonProperty("shippingNumber")]
      public string? ShippingNumber { get; set; }              // お荷物伝票番号
      [JsonProperty("deliveryCompany")]
      public string? DeliveryCompany { get; set; }             // 配送会社
      [JsonProperty("deliveryCompanyName")]
      public string? DeliveryCompanyName { get; set; }         // 配送会社名
      [JsonProperty("shippingDate")]
      public DateTime? ShippingDate { get; set; }              // 発送日
   }

   public class RakutenGetOrderDeliveryCVSModel
   {
      [JsonProperty("cvsCode")]
      public int? CvsCode { get; set; }                        // コンビニコード
      [JsonProperty("storeGenreCode")]
      public string? StoreGenreCode { get; set; }              // ストア分類コード
      [JsonProperty("storeCode")]
      public string? StoreCode { get; set; }                   // ストアコード
      [JsonProperty("storeName")]
      public string? StoreName { get; set; }                   // ストア名称
      [JsonProperty("storeZip")]
      public string? StoreZip { get; set; }                    // 郵便番号
      [JsonProperty("storePrefecture")]
      public string? StorePrefecture { get; set; }             // 都道府県
      [JsonProperty("storeAddress")]
      public string? StoreAddress { get; set; }                // その他住所
      [JsonProperty("areaCode")]
      public string? AreaCode { get; set; }                    // 発注エリアコード
      [JsonProperty("depo")]
      public string? Depo { get; set; }                        // センターデポコード
      [JsonProperty("openTime")]
      public string? OpenTime { get; set; }                    // 開店時間
      [JsonProperty("closeTime")]
      public string? CloseTime { get; set; }                   // 閉店時間
      [JsonProperty("cvsRemarks")]
      public string? CvsRemarks { get; set; }                  // 特記事項
   }

   public class RakutenGetOrderCouponModel
   {
      [JsonProperty("couponCode")]
      public string CouponCode { get; set; }                   // クーポンコード
      [JsonProperty("itemId")]
      public int ItemId { get; set; }                          // クーポン対象の商品ID
      [JsonProperty("couponName")]
      public string CouponName { get; set; }                   // クーポン名
      [JsonProperty("couponSummary")]
      public string CouponSummary { get; set; }                // クーポン効果(サマリー)
      [JsonProperty("couponCapital")]
      public string CouponCapital { get; set; }                // クーポン原資
      [JsonProperty("couponCapitalCode")]
      public int CouponCapitalCode { get; set; }               // クーポン原資コード
      [JsonProperty("expiryDate")]
      public DateTime ExpiryDate { get; set; }                 // 有効期限
      [JsonProperty("couponPrice")]
      public int CouponPrice { get; set; }                     // クーポン割引単価
      [JsonProperty("couponUnit")]
      public int CouponUnit { get; set; }                      // クーポン利用数
      [JsonProperty("couponTotalPrice")]
      public int CouponTotalPrice { get; set; }                // クーポン利用金額
      [JsonProperty("itemDetailId")]
      public string ItemDetailId { get; set; }                 // 商品明細ID
   }

   public class RakutenGetOrderChangeReasonModel
   {
      [JsonProperty("changeId")]
      public string ChangeId { get; set; }                     // 変更ID
      [JsonProperty("changeType")]
      public int? ChangeType { get; set; }                     // 変更種別
      [JsonProperty("changeTypeDetail")]
      public int? ChangeTypeDetail { get; set; }               // 変更種別(詳細)
      [JsonProperty("changeReason")]
      public int? ChangeReason { get; set; }                   // 変更理由
      [JsonProperty("changeReasonDetail")]
      public int? ChangeReasonDetail { get; set; }             // 変更理由(小分類)
      [JsonProperty("changeApplyDatetime")]
      public DateTime? ChangeApplyDatetime { get; set; }       // 変更申請日
      [JsonProperty("changeFixDatetime")]
      public DateTime? ChangeFixDatetime { get; set; }         // 変更確定日
      [JsonProperty("changeCmplDatetime")]
      public DateTime? ChangeCmplDatetime { get; set; }        // 変更完了日
   }

   public class RakutenGetOrderTaxSummaryModel
   {
      [JsonProperty("taxRate")]
      public double TaxRate { get; set; }                      // 税率
      [JsonProperty("reqPrice")]
      public int ReqPrice { get; set; }                        // 請求金額
      [JsonProperty("reqPriceTax")]
      public int ReqPriceTax { get; set; }                     // 請求額に対する税額
      [JsonProperty("totalPrice")]
      public int TotalPrice { get; set; }                      // 合計金額
      [JsonProperty("paymentCharge")]
      public int PaymentCharge { get; set; }                   // 決済手数料
      [JsonProperty("couponPrice")]
      public int CouponPrice { get; set; }                     // クーポン割引額
      [JsonProperty("point")]
      public int Point { get; set; }                           // 利用ポイント数
   }

   public class RakutenGetOrderDueDateModel
   {
      [JsonProperty("dueDateType")]
      public int DueDateType { get; set; }                     // 期限日タイプ
      [JsonProperty("dueDate")]
      public DateTime DueDate { get; set; }                    // 期限日
   }

   public class RakutenGetOrderskuModel
   {
      [JsonProperty("variantId")]
      public string VariantId { get; set; }                    // SKU管理番号
      [JsonProperty("merchantDefinedSkuId")]
      public string? MerchantDefinedSkuId { get; set; }        // システム連携用SKU番号
      [JsonProperty("skuInfo")]
      public string? SkuInfo { get; set; }                     // SKU情報
   }
}
