using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;

/**************************************************************/
/*          RakutenGetOrderリクエスト・レスポンスのモデル        */
/**************************************************************/
// 仕様
// https://webservice.rms.rakuten.co.jp/merchant-portal/view/ja/common/1-1_service_index/rakutenpayorderapi/getorder

namespace ssAppServices.Api.Rakuten
{
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
      /// Rakuten注文情報APIを呼び出し、必要なデータのみ返す（本番用）
      /// </summary>
      public RakutenGetOrderResponse GetOrder(
         RakutenGetOrderRequest requestParameter, RakutenShop rakutenShop)
      {
         // HTTPレスポンスを無視し、パース結果のみ返す
         var (_, parsedData) = GetOrderWithResponse(requestParameter, rakutenShop);
         return parsedData;
      }

      /// <summary>
      /// Rakuten注文情報APIを呼び出し、HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (HttpResponseMessage, RakutenGetOrderResponse) GetOrderWithResponse(
         RakutenGetOrderRequest requestParameter, RakutenShop rakutenShop)
      {
         // リクエストパラメータのバリデーション
         //requestParameter = ValidateRequestParameters(requestParameter);
         // ShopToken 情報の取得
         var shopToken = ssAppDBHelper.GetShopToken(_dbContext, rakutenShop.ToString());
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

      // レスポンスボディのデシリアライズ
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
}
