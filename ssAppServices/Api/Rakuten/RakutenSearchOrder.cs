#pragma warning disable CS8602
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;

/**************************************************************/
/*                     RakutenSearchOrder                     */
/**************************************************************/
// 仕様
// https://webservice.rms.rakuten.co.jp/merchant-portal/view/ja/common/1-1_service_index/rakutenpayorderapi/searchorder
// ●重要なAPI仕様
// 1リクエスト（1ページあたりの取得数）で最大 1000 件の注文番号を取得します。

namespace ssAppServices.Api.Rakuten;

public class RakutenSearchOrder
{
   private readonly ApiRequestHandler _requestHandler;
   private readonly ssAppDBContext _dbContext;
   private readonly string _apiEndpoint;

   public RakutenSearchOrder(
      ApiRequestHandler requestHandler,
      ssAppDBContext dbContext,
      IOptions<MallSettings> mallSettings)
   {
      _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
      _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
      _apiEndpoint = mallSettings.Value.Rakuten.Endpoints.Order.SearchOrder
         ?? throw new ArgumentNullException(nameof(mallSettings), "Rakuten API SearchOrderエンドポイントが設定されていません。");
   }

   /// <summary>
   /// RakutenSearchOrder API ページング処理（全ページデータ取得）
   /// </summary>
   public RakutenSearchOrderResponse RunGetSearchOrder(
       RakutenSearchOrderRequest requestParameter, RakutenShop rakutenShop)
   {
      // 初回リクエスト
      var response = GetSearchOrder(requestParameter, rakutenShop);
      if (response.OrderNumberList == null)
         return response;

      // OrderNumberListを蓄積
      var orderNumbers = new List<string>(response.OrderNumberList);

      // ページング処理
      var totalPages = response.PaginationResponse?.TotalPages ?? 1;
      for (int currentPage = 2; currentPage <= totalPages; currentPage++)
      {
         // リクエストページを更新
         requestParameter.PaginationRequestModel.RequestPage = currentPage;

         // 次のページのレスポンスを取得
         var nextPageResponse = GetSearchOrder(requestParameter, rakutenShop);
         if (nextPageResponse?.OrderNumberList != null)
            orderNumbers.AddRange(nextPageResponse.OrderNumberList);
      }
      
      response.OrderNumberList = orderNumbers;
      return response;
   }

   /// <summary>
   /// RakutenSearchOrder APIから必要なデータのみ返す（本番用）
   /// </summary>
   public RakutenSearchOrderResponse GetSearchOrder(
      RakutenSearchOrderRequest requestParameter, RakutenShop rakutenShop)
   {
      // HTTPレスポンスを無視し、パース結果のみ返す
      var (_, parsedData) = GetSearchOrderWithResponse(requestParameter, rakutenShop);
      return parsedData;
   }

   /// <summary>
   /// RakutenSearchOrder APIから、HTTPレスポンスも返す（テスト用）
   /// </summary>
   public (HttpResponseMessage, RakutenSearchOrderResponse) GetSearchOrderWithResponse(
      RakutenSearchOrderRequest requestParameter, RakutenShop rakutenShop)
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

   // RakutenSearchOrderレスポンスボディのデシリアライズ
   public RakutenSearchOrderResponse ParseResponse(string responseBody)
   {
      try
      {
         // レスポンスボディをデシリアライズ
         var response = JsonConvert.DeserializeObject<RakutenSearchOrderResponse>(responseBody);
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
