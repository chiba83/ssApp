#pragma warning disable CS8602, CS8620
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using ssAppModels.EFModels;
using ssAppModels.ApiModels;
using ssAppCommon.Extensions;

/**************************************************************/
/*                        YahooOrderList                      */
/**************************************************************/
// YahooOrderList は、Yahooの注文検索APIを呼び出すためのクラスです。
// 検索条件に応じた注文ヘッダー情報を返します。（注文明細は返しません）
// 注文明細を取得する前段階で使用するAPIです。主に注文ID一覧を取得するために使用します。
// 
// リクエスト仕様
// https://developer.yahoo.co.jp/webapi/shopping/orderList.html
// リクエスト仕様：検索条件（Condition）
// https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#condition
// リクエスト仕様：取得情報選択（Field）
// https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#field
// リクエスト仕様：配送会社コード／サンプルリクエスト
// https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#shipcompanycode
// レスポンス仕様／サンプルレスポンス
// https://developer.yahoo.co.jp/webapi/shopping/orderList.html/#response
// 公開鍵認証の仕様（サンプルコードあり）
// https://developer.yahoo.co.jp/webapi/shopping/help/#aboutapipublickey/#aboutapipublickey

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
      /// YahooOrderList API ページング処理（全ページデータ取得）
      /// ページング処理制約事項：出力フィールドは、"OrderID" のみ
      /// </summary>
      public (int, List<string>) RunGetOrderSearch(
         YahooOrderListRequest yahooOrderListRequest, YahooShop shopCode)
      {
         // 出力フィールドを "OrderID" に制限
         yahooOrderListRequest.Req.Search.Field = "OrderId";

         // 初回リクエスト
         var response = GetOrderSearch(yahooOrderListRequest, shopCode);
         var totalCount = response.Search.TotalCount;
         if (totalCount == 0)
            return (totalCount, new List<string>());

         // OrderIDを蓄積
         var OrderIds = new List<string>(response.Search.OrderInfo
            .Select(x => x.Fields["OrderId"].ToString()));

         // ページング処理
         var totalPages = totalCount / 2000 + 1;
         for (int currentPage = 2; currentPage <= totalPages; currentPage++)
         {
            // リクエストページ（スタートデータ位置）を更新
            yahooOrderListRequest.Req.Search.Start = (currentPage - 1) * 2000 + 1;

            // 次のページのレスポンスを取得
            var nextPageResponse = GetOrderSearch(yahooOrderListRequest, shopCode);
            if (nextPageResponse?.Search.OrderInfo != null)
               OrderIds.AddRange(nextPageResponse.Search.OrderInfo
                  .Select(x => x.Fields["OrderId"].ToString()));
         }
         return (totalCount, OrderIds);
      }


      /// <summary>
      /// Yahoo注文検索APIを呼び出し、必要なデータのみ返す（本番用）
      /// </summary>
      public YahooOrderListResult GetOrderSearch(
         YahooOrderListRequest yahooOrderListRequest, YahooShop shopCode)
      {
         // HTTPレスポンスを無視し、パース結果のみ返す
         var (_, parsedData) = GetOrderSearchWithResponse(yahooOrderListRequest, shopCode);
         return parsedData;
      }

      /// <summary>
      /// Yahoo注文検索APIを呼び出し、HTTPレスポンスも返す（テスト用）
      /// </summary>
      public (HttpResponseMessage, YahooOrderListResult) GetOrderSearchWithResponse(
         YahooOrderListRequest yahooOrderListRequest, YahooShop shopCode)
      {
         var outputFields = yahooOrderListRequest.Req.Search.Field.Split(',').ToList();
         // Validation Check
         ApiHelpers.AreAllFieldsValid(outputFields, YahooOrderListFieldDefinitions.FieldDefinitions);
         // ShopToken 情報の取得
         var shopToken = ApiHelpers.GetShopToken(_dbContext, shopCode.ToString());
         // リクエストオブジェクトの作成
         var requestMessage = SetHttpRequest(yahooOrderListRequest, shopToken, shopCode);
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

      // HTTPリクエストの作成
      private HttpRequestMessage SetHttpRequest(YahooOrderListRequest yahooOrderListRequest, ShopToken shopToken, YahooShop shopCode)
      {
         // XMLリクエストボディの作成
         var xmlBody = ApiHelpers.SerializeToXml(yahooOrderListRequest.Req, "Req");
         // HTTPリクエストの作成
         var requestMessage = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
         {
            Content = new StringContent(xmlBody, Encoding.UTF8, "application/xml") // UTF-8を明示
         };
         // アクセストークンの取得
         var accessToken = _authService.GetValidAccessToken(shopCode);
         // リクエストヘッダの設定
         var (encodedPublicKey, KeyVersion) = ApiHelpers.GetPublicKey(shopToken); // 公開鍵取得
         return ApiHelpers.SetRequestHeaders(requestMessage, accessToken, encodedPublicKey, KeyVersion);
      }

      // レスポンスXMLをパースして動的コレクションを生成
      private YahooOrderListResult ParseResponseXml(string responseXml, List<string> outputFields)
      {
         // OrderInfo の要素を取得
         var document = XDocument.Parse(responseXml);
         // ResultSetノードを解析
         var resultSetElement = document.Root;
         if (resultSetElement == null || resultSetElement.Name.LocalName != "Result")
            throw new InvalidOperationException("ResultノードがレスポンスXMLに存在しません。");

         var result = new YahooOrderListResult
         {
            Status = resultSetElement.Element("Status")?.Value
               ?? throw new InvalidOperationException("StatusノードがレスポンスXMLに存在しません。"),
            Search = new YahooOrderListSearch
            {
               TotalCount = int.Parse(resultSetElement.Descendants("TotalCount").First().Value),
               OrderInfo = ParseOrderInfo(resultSetElement, outputFields)
            }
         };
         return result;
      }

      // OrderInfo 動的フィールドをパース
      private List<YahooOrderListOrderInfo> ParseOrderInfo(XElement resultSetElement, List<string> outputFields)
      {
         var node = resultSetElement.Descendants("OrderInfo");
         var hasItems = node.Elements("Item").Any();
         var excludeNames = new HashSet<string> { "Item", "Index", "UsePointType" };

         return node.Select(orderInfo => new YahooOrderListOrderInfo()
            {
               Index = int.Parse(orderInfo.Element("Index")?.Value ?? "0"),
               Fields = SetFields(orderInfo.Elements().Where(e => !excludeNames.Contains(e.Name.LocalName))),
               Items = hasItems ? SetFields(orderInfo.Descendants("Item").Elements()) : null
            }
         ).ToList();
      }

      private Dictionary<string, object> SetFields(IEnumerable<XElement> orderInfo)
      {
         return orderInfo.ToDictionary(
            e => e.Name.LocalName,
            e =>
            {
               var fieldType = YahooOrderListFieldDefinitions.FieldDefinitions.GetValueOrDefault(e.Name.LocalName);
               return Reflection.CreateInstance(fieldType, e.Value);
            }
         );
      }
   }
}
