#pragma warning disable CS8618
using Newtonsoft.Json;

/**************************************************************/
/*        RakutenSearchOrderリクエスト・レスポンスのモデル       */
/**************************************************************/
// 仕様
// https://webservice.rms.rakuten.co.jp/merchant-portal/view/ja/common/1-1_service_index/rakutenpayorderapi/searchorder

namespace ssAppModels.ApiModels;

/**************************************************************/
/*          RakutenSearchOrderリクエストのモデルCLASS           */
/**************************************************************/

public class RakutenSearchOrderRequest
{
   [JsonProperty("orderProgressList")]
   public List<int>? OrderProgressList { get; set; }   // ステータスリスト
   [JsonProperty("subStatusIdList")]
   public List<int>? SubStatusIdList { get; set; }     // サブステータスIDリスト
   [JsonProperty("dateType")]
   public int DateType { get; set; }                   // 期間検索種別
   [JsonProperty("startDatetime")]
   public string StartDatetime { get; set; }           // 期間検索開始日時
   [JsonProperty("endDatetime")]
   public string EndDatetime { get; set; }             // 期間検索終了日時
   [JsonProperty("orderTypeList")]
   public List<int>? OrderTypeList { get; set; }       // 販売種別リスト
   [JsonProperty("settlementMethod")]
   public int? SettlementMethod { get; set; }          // 支払方法名
   [JsonProperty("deliveryName")]
   public string? DeliveryName { get; set; }           // 配送方法
   [JsonProperty("shippingDateBlankFlag")]
   public int? ShippingDateBlankFlag { get; set; }     // 発送日未指定有無フラグ
   [JsonProperty("shippingNumberBlankFlag")]
   public int? ShippingNumberBlankFlag { get; set; }   // お荷物伝票番号未指定有無フラグ
   [JsonProperty("searchKeywordType")]
   public int? SearchKeywordType { get; set; }         // 検索キーワード種別
   [JsonProperty("searchKeyword")]
   public string? SearchKeyword { get; set; }          // 検索キーワード
   [JsonProperty("mailSendType")]
   public int? MailSendType { get; set; }              // 注文メールアドレス種別
   [JsonProperty("ordererMailAddress")]
   public string? OrdererMailAddress { get; set; }     // 注文者メールアドレス
   [JsonProperty("phoneNumberType")]
   public int? PhoneNumberType { get; set; }           // 電話番号種別
   [JsonProperty("phoneNumber")]
   public string? PhoneNumber { get; set; }            // 電話番号
   [JsonProperty("reserveNumber")]
   public string? ReserveNumber { get; set; }          // 申込番号
   [JsonProperty("purchaseSiteType")]
   public int? PurchaseSiteType { get; set; }          // 購入サイトリスト
   [JsonProperty("asurakuFlag")]
   public int? AsurakuFlag { get; set; }               // あす楽希望フラグ
   [JsonProperty("couponUseFlag")]
   public int? CouponUseFlag { get; set; }             // クーポン利用有無フラグ
   [JsonProperty("drugFlag")]
   public int? DrugFlag { get; set; }                  // 医薬品受注フラグ
   [JsonProperty("overseasFlag")]
   public int? OverseasFlag { get; set; }              // 海外カゴ注文フラグ
   [JsonProperty("PaginationRequestModel")]
   public RakutenSearchOrderPaginationRequestModel? 
      PaginationRequestModel { get; set; }             // ページングリクエストモデル
   [JsonProperty("oneDayOperationFlag")]
   public int? OneDayOperationFlag { get; set; }       // 注文当日出荷フラグ
}

public class RakutenSearchOrderPaginationRequestModel
{
   [JsonProperty("requestRecordsAmount")]
   public int RequestRecordsAmount { get; set; }       // 1ページあたりの取得結果数
   [JsonProperty("requestPage")]
   public int RequestPage { get; set; }                // リクエストページ番号
   [JsonProperty("SortModelList")]
   public List<RakutenSearchOrderSortModel>? SortModelList { get; set; } // 並び替えモデルリスト
}

public class RakutenSearchOrderSortModel
{
   [JsonProperty("sortColumn")]
   public int SortColumn { get; set; }                 // 並び替え項目
   [JsonProperty("sortDirection")]
   public int SortDirection { get; set; }              // 並び替え方法
}

// orderProgressList ステータスリスト
//    100: 注文確認待ち 200: 楽天処理中 300: 発送待ち 400: 変更確定待ち 500: 発送済 600: 支払手続き中 700: 支払手続き済
// subStatusIdList サブステータスIDリスト
//    -1 サブステータスなし 273015 処理中 268493 出荷完了 261488 出荷保留
// dateType 期間検索種別：1: 注文日 2: 注文確認日 3: 注文確定日 4: 発送日 5: 発送完了報告日
// startDatetime 期間検索開始日時：過去 730 日(2年)以内の注文を指定可能
// endDatetime   期間検索終了日時：startDatetime から 63 日以内の範囲を指定可能
public class RakutenSearchOrderRequestFactory
{
   // 共通リクエスト生成メソッド
   private static RakutenSearchOrderRequest CreateRequest(
      List<int> orderProgressList, List<int> subStatusIdList, int dateType,
      int startDaysOffset, DateTime? startDatetime, DateTime? endDatetime,
      int? requestPage)
   {
      DateTime endDT = endDatetime == null ? DateTime.Now : (DateTime)endDatetime;
      DateTime startDT = startDatetime == null 
         ? endDT.AddDays(startDaysOffset) : (DateTime)startDatetime;

      return new RakutenSearchOrderRequest
      {
         OrderProgressList = orderProgressList,
         SubStatusIdList = subStatusIdList,
         DateType = dateType,
         StartDatetime = startDT.ToString("yyyy-MM-ddT00:00:00+0900"),
         EndDatetime = endDT.ToString("yyyy-MM-ddT23:59:59+0900"),
         PaginationRequestModel = new RakutenSearchOrderPaginationRequestModel
         {
            RequestRecordsAmount = 1000,
            RequestPage = requestPage ?? 1
         }
      };
   }
   // 注文確認中リクエスト
   public static RakutenSearchOrderRequest ConfirmingOrderRequest(
      DateTime? startDatetime, DateTime? endDatetime, int? requestPage)
   {
      return CreateRequest(
         new List<int> { 200 }, new List<int> { -1 },
         1, -30, startDatetime, endDatetime, requestPage
      );
   }
   // 新規注文リクエスト
   public static RakutenSearchOrderRequest NewOrderRequest(
      DateTime? startDatetime, DateTime? endDatetime, int? requestPage)
   {
      return CreateRequest(
         new List<int> { 300 }, new List<int> { -1 },
         1, -30, startDatetime, endDatetime, requestPage
      );
   }
   // 発送処理中リクエスト
   public static RakutenSearchOrderRequest ShippingProcessRequest(
      DateTime? startDatetime, DateTime? endDatetime, int? requestPage)
   {
      return CreateRequest(
         new List<int> { 300 }, new List<int> { 273015 },
         1, -30, startDatetime, endDatetime, requestPage
      );
   }
   // 出荷完了リクエスト
   public static RakutenSearchOrderRequest ShippedOrderRequest(
      DateTime? startDatetime, DateTime? endDatetime, int? requestPage)
   {
      return CreateRequest(
         new List<int> { 400, 500, 600, 700 }, new List<int> { },
         1, -30, startDatetime, endDatetime, requestPage
      );
   }
   // プレゼント対象注文リクエスト
   public static RakutenSearchOrderRequest PresentTargetOrderRequest(
      DateTime? startDatetime, DateTime? endDatetime, int? requestPage)
   {
      return CreateRequest(
         new List<int> { 400, 500, 600, 700 }, new List<int> { },
         1, -60, startDatetime, endDatetime, requestPage
      );
   }
}

/**************************************************************/
/*          RakutenSearchOrderレスポンスのモデルCLASS           */
/**************************************************************/

public class RakutenSearchOrderResponse
{
   [JsonProperty("MessageModelList")]
   public List<RakutenSearchOrderMessageModel> MessageModelList { get; set; } // メッセージモデルリスト
   [JsonProperty("orderNumberList")]
   public List<string>? OrderNumberList { get; set; }  // 注文番号リスト
   [JsonProperty("PaginationResponseModel")]
   public RakutenSearchOrderPaginationResponseModel? PaginationResponse { get; set; } // ページングレスポンスモデル
}

public class RakutenSearchOrderMessageModel
{
   [JsonProperty("messageType")]
   public string MessageType { get; set; }             // メッセージ種別
   [JsonProperty("messageCode")]
   public string MessageCode { get; set; }             // メッセージコード
   [JsonProperty("message")]
   public string Message { get; set; }                 // メッセージ
}

public class RakutenSearchOrderPaginationResponseModel
{
   [JsonProperty("totalRecordsAmount")]
   public int? TotalRecordsAmount { get; set; }        // 総結果数
   [JsonProperty("totalPages")]
   public int? TotalPages { get; set; }                // 総ページ数
   [JsonProperty("requestPage")]
   public int? RequestPage { get; set; }               // リクエストページ番号
}