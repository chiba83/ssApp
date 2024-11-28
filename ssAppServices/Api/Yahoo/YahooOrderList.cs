using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using ssAppModels.EFModels;
using ssAppModels.ApiModels;

namespace ssAppServices.Api.Yahoo
{
   public class YahooOrderList
   {
      private readonly YahooAuthenticationService _authService;
      private readonly ApiRequestHandler _requestHandler;
      private readonly ssAppDBContext _dbContext;
      private readonly string _apiEndpoint;

      public YahooOrderList(
          YahooAuthenticationService authService,
          ApiRequestHandler requestHandler,
          ssAppDBContext dbContext,
          IOptions<MallSettings> mallSettings)
      {
         _authService = authService ?? throw new ArgumentNullException(nameof(authService));
         _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
         _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
         _apiEndpoint = mallSettings.Value.Yahoo.Endpoints.Order.OrderList
             ?? throw new ArgumentNullException(nameof(mallSettings), "Yahooの注文APIエンドポイントが設定されていません。");
      }

      /// <summary>
      /// Yahoo注文検索APIを呼び出し、必要なデータのみ返す（本番用）
      /// </summary>
      public List<Dictionary<string, object?>> GetOrderSearch(
         YahooOrderListCondition searchCondition,
         List<string> outputFields,
         YahooShop shopCode,
         int? resultLimit = null,
         int? startIndex = null)
      {
         // HTTPレスポンスを無視し、パース結果のみ返す
         var (_, parsedData) = GetOrderSearchWithResponse(searchCondition, outputFields, shopCode, resultLimit, startIndex);
         return parsedData;
      }

      /// <summary>
      /// Yahoo注文検索APIを呼び出し、HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (HttpResponseMessage httpResponse, List<Dictionary<string, object?>> parsedData) GetOrderSearchWithResponse(
         YahooOrderListCondition searchCondition,
         List<string> outputFields,
         YahooShop shopCode,
         int? resultLimit = null,
         int? startIndex = null)
      {
         // アクセストークンの取得
         var accessToken = _authService.GetValidAccessToken(shopCode);

         // ShopToken 情報の取得
         var shopToken = ApiHelpers.GetShopToken(_dbContext, shopCode);

         // リクエストオブジェクトの作成
         var requestBody = new YahooOrderListRequestBody
         {
            Search = new YahooOrderListCriteria
            {
               Result = resultLimit ?? 2000,
               Start = startIndex ?? 1,
               Condition = searchCondition,
               Field = string.Join(",", outputFields)
            },
            SellerId = shopToken.SellerId
         };

         // XMLリクエストボディの作成
         var xmlBody = ApiHelpers.SerializeToXml(requestBody, "Req");

         // HTTPリクエストの作成
         var requestMessage = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
         {
            Content = new StringContent(xmlBody, Encoding.UTF8, "application/xml") // UTF-8を明示
         };

         // リクエストヘッダの設定
         var encodedPublicKey = ApiHelpers.GetPublicKey(shopToken); // 公開鍵取得
         requestMessage = ApiHelpers.SetRequestHeaders(requestMessage, accessToken, encodedPublicKey);

         // PollyContext を生成
         var pollyContext = ApiHelpers.CreatePollyContext("Yahoo", requestMessage, shopToken.SellerId);

         // HTTPリクエストを送信
         var response = _requestHandler.SendAsync(requestMessage, pollyContext).Result;

         // レスポンス処理
         if (!response.IsSuccessStatusCode)
            throw new Exception($"Yahoo APIリクエストが失敗しました: {response.ReasonPhrase}");

         // レスポンスボディを取得
         var responseBody = response.Content.ReadAsStringAsync().Result;

         // レスポンスXMLをパース
         var parsedData = ParseResponseXml(responseBody, outputFields);
         return (response, parsedData);
      }

      /// <summary>
      /// レスポンスXMLをパースして動的コレクションを生成
      /// </summary>
      private List<Dictionary<string, object?>> ParseResponseXml(string responseXml, List<string> outputFields)
      {
         // OrderInfo の要素を取得
         var xDocument = XDocument.Parse(responseXml);
         var orderElements = xDocument.Descendants("OrderInfo");

         // フィールドの型情報を準備
         var validFields = outputFields
             .Where(field => OrderInfo.FieldDefinitions.ContainsKey(field)) // 定義済みのフィールドのみ処理
             .ToList();

         // OrderInfo を辞書のリストに変換
         var results = orderElements
             .Select(orderElement => validFields
                 .ToDictionary(
                     field => field,
                     field =>
                     {
                        var elementValue = orderElement.Element(field)?.Value;
                        return elementValue != null
                             ? Convert.ChangeType(elementValue, OrderInfo.FieldDefinitions[field]) // 型変換
                             : null; // 値がない場合は null
                     }
                 )
             ).ToList();

         return results;
      }
   }
}
