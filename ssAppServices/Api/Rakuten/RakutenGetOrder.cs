#pragma warning disable CS8604
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;

/**************************************************************/
/*          RakutenGetOrderリクエスト・レスポンスのモデル        */
/**************************************************************/
// 仕様
// https://webservice.rms.rakuten.co.jp/merchant-portal/view/ja/common/1-1_service_index/rakutenpayorderapi/getorder
// ●重要なAPI仕様
// 1リクエストで最大 100 件の注文情報を取得します。

namespace ssAppServices.Api.Rakuten;

public class RakutenGetOrder
{
   private readonly ApiRequestHandler _requestHandler;
   private readonly ssAppDBContext _dbContext;
   private readonly string _apiEndpoint;

   public RakutenGetOrder(
      ApiRequestHandler requestHandler,
      ssAppDBContext dbContext,
      IOptions<MallSettings> mallSettings)
   {
      _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
      _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
      _apiEndpoint = mallSettings.Value.Rakuten.Endpoints.Order.GetOrder
         ?? throw new ArgumentNullException(nameof(mallSettings), "Rakuten API GetOrderエンドポイントが設定されていません。");
   }

   /// <summary>
   /// ページング処理を実行し、注文リストの全注文明細を取得する。
   /// ただし、取得件数上限（注文リスト件数）を2,000件とする。
   /// </summary>
   /// <param name="searchOrder">注文リスト</param>
   /// <param name="getOrderRequest">リクエストパラメータリスト</param>
   /// <param name="rakutenShop">楽天ショップ</param>
   /// <returns name="getOrderResponse">注文明細リスト</returns>
   public RakutenGetOrderResponse? GetAllOrdersFromSearch(
      RakutenSearchOrderResponse searchOrder,
      RakutenGetOrderRequest getOrderRequest, 
      RakutenShop rakutenShop)
   {
      if (searchOrder.OrderNumberList?.Count > 2000)
         throw new Exception("楽天注文リストの取得件数が2,000件を超えています。");

      // 注文リストが取得できない場合は処理を終了
      if (searchOrder.PaginationResponse?.TotalRecordsAmount.GetValueOrDefault() == 0
         || searchOrder.OrderNumberList?.Any() != true)
         return null;

      var getOrderResponse = new RakutenGetOrderResponse();

      for (int page = 1; page <= searchOrder.PaginationResponse!.TotalPages; page++)
      {
         // GetOrder リクエストパラメータ
         var getOrderParameter = RakutenGetOrderRequestFactory.LatestVersionRequest(
            searchOrder.OrderNumberList.Skip((page - 1) * 100).Take(100).ToList());

         // GetOrder 実行。Max 100件/Request
         var response = GetOrder(getOrderParameter, rakutenShop);
         getOrderResponse.MessageModelList.AddRange(response.MessageModelList);
         getOrderResponse.OrderModelList?.AddRange(response.OrderModelList);
      }
      return getOrderResponse;
   }

   /// <summary>
   /// RakutenGetOrder APIから必要なデータのみ返す（本番用）
   /// </summary>
   public RakutenGetOrderResponse GetOrder(
      RakutenGetOrderRequest requestParameter, RakutenShop rakutenShop)
   {
      // HTTPレスポンスを無視し、パース結果のみ返す
      var (_, parsedData) = GetOrderWithResponse(requestParameter, rakutenShop);
      return parsedData;
   }

   /// <summary>
   /// RakutenGetOrder APIから必要なデータのみ返す（テスト用）
   /// </summary>
   public (HttpResponseMessage, RakutenGetOrderResponse) GetOrderWithResponse(
      RakutenGetOrderRequest requestParameter, RakutenShop rakutenShop)
   {
      // ShopToken 情報の取得
      var shopToken = ApiHelpers.GetShopToken(_dbContext, rakutenShop.ToString());
      // リクエストオブジェクトの作成
      var requestMessage = ApiHelpers.SetRakutenRequest(_apiEndpoint, requestParameter, shopToken);
      // PollyContext を生成
      var pollyContext = ApiHelpers.CreatePollyContext("Rakuten", requestMessage, shopToken.SellerId);
      // HTTPリクエストを送信
      var response = _requestHandler.SendAsync(requestMessage, pollyContext).Result;

      // レスポンス処理
      if (!response.IsSuccessStatusCode)
         throw new Exception($"Rakuten APIリクエストが失敗しました: {response.ReasonPhrase}");

      var responseBody = response.Content.ReadAsStringAsync().Result;
      var parsedData = ParseResponse(responseBody);

      return (response, parsedData);
   }

   // RakutenGetOrder レスポンスボディのデシリアライズ
   public RakutenGetOrderResponse ParseResponse(string responseBody)
   {
      try
      {
         // レスポンスボディをデシリアライズ
         var response = JsonConvert.DeserializeObject<RakutenGetOrderResponse>(responseBody);
         if (response == null)
            throw new Exception("レスポンスのデシリアライズ結果がnullです。");

         return response;
      }
      catch (JsonException ex)
      {
         throw new Exception("レスポンスのデシリアライズ中にエラーが発生しました。", ex);
      }
   }
}
