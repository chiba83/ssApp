using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;

/**************************************************************/
/*        RakutenSearchOrderリクエスト・レスポンスのモデル       */
/**************************************************************/
// 仕様
// https://webservice.rms.rakuten.co.jp/merchant-portal/view/ja/common/1-1_service_index/rakutenpayorderapi/searchorder

namespace ssAppServices.Api.Rakuten
{
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
      /// Rakuten注文検索APIを呼び出し、必要なデータのみ返す（本番用）
      /// </summary>
      public RakutenSearchOrderResponse GetSearchOrder(
         RakutenSearchOrderRequest requestParameter, RakutenShop rakutenShop)
      {
         // HTTPレスポンスを無視し、パース結果のみ返す
         var (_, parsedData) = GetSearchOrderWithResponse(requestParameter, rakutenShop);
         return parsedData;
      }

      /// <summary>
      /// Rakuten注文検索APIを呼び出し、HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (HttpResponseMessage, RakutenSearchOrderResponse) GetSearchOrderWithResponse(
         RakutenSearchOrderRequest requestParameter, RakutenShop rakutenShop)
      {
         // リクエストパラメータのバリデーション
         requestParameter = ValidateRequestParameters(requestParameter);
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

      // リクエストパラメータのバリデーション
      private RakutenSearchOrderRequest ValidateRequestParameters(RakutenSearchOrderRequest parameter)
      {
         // Rakuten API用に日時フォーマット補正
         parameter.StartDatetime 
            = DateTime.Parse(parameter.StartDatetime).ToString("yyyy-MM-ddTHH:mm:ss+0900");
         parameter.EndDatetime 
            = DateTime.Parse(parameter.EndDatetime).ToString("yyyy-MM-ddTHH:mm:ss+0900");
         return parameter;
      }

      // レスポンスボディのデシリアライズ
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
}
