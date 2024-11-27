using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Options;
using ssAppModels.EFModels;
using ssAppModels.ApiModels;
using ssAppCommon.Extensions;
using System.Xml.Linq;

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
         _authService = authService
             ?? throw new ArgumentNullException(nameof(authService));
         _requestHandler = requestHandler
             ?? throw new ArgumentNullException(nameof(requestHandler));
         _dbContext = dbContext
             ?? throw new ArgumentNullException(nameof(dbContext));
         _apiEndpoint = mallSettings.Value.Yahoo.Endpoints.Order.OrderList
             ?? throw new ArgumentNullException(nameof(mallSettings), "Yahooの注文APIエンドポイントが設定されていません。");
      }

      /// <summary>
      /// Yahoo注文検索APIを呼び出す
      /// </summary>
      public HttpResponseMessage GetOrderSearch(
//      public (HttpResponseMessage, List<Dictionary<string, object>>) GetOrderSearch(
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
         return _requestHandler.SendAsync(requestMessage, pollyContext).Result;
      }
   }
}
