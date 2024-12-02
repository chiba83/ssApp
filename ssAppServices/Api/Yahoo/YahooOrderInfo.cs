﻿using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using ssAppModels.EFModels;
using ssAppModels.ApiModels;
using ssAppCommon.Extensions;

/**************************************************************/
/*                        YahooOrderInfo                      */
/**************************************************************/
// YahooOrderInfo は、Yahooの注文詳細APIを呼び出すためのクラスです。
// 注文IDに応じた注文明細情報を返します。
// 注文IDはList<string>で複数指定可能です。
// 結果は一括で取得し、List<YahooOrderInfoResponse>で返します。
// 
// リクエスト仕様
// https://developer.yahoo.co.jp/webapi/shopping/orderInfo.html
// リクエスト仕様：取得情報選択（Field）
// https://developer.yahoo.co.jp/webapi/shopping/orderInfo.html/#field
// リクエスト仕様：配送会社コード／サンプルリクエスト
// https://developer.yahoo.co.jp/webapi/shopping/orderInfo.html/#shipcompanycode
// レスポンス仕様／サンプルレスポンス
// https://developer.yahoo.co.jp/webapi/shopping/orderInfo.html/#response
// 公開鍵認証の仕様（サンプルコードあり）
// https://developer.yahoo.co.jp/webapi/shopping/help/#aboutapipublickey/#aboutapipublickey

namespace ssAppServices.Api.Yahoo
{
   public class YahooOrderInfo
   {
      private readonly YahooAuthenticationService _authService;
      private readonly ApiRequestHandler _requestHandler;
      private readonly ssAppDBContext _dbContext;
      private readonly string _apiEndpoint;

      public YahooOrderInfo(
          YahooAuthenticationService authService,
          ApiRequestHandler requestHandler,
          ssAppDBContext dbContext,
          IOptions<MallSettings> mallSettings)
      {
         _authService = authService ?? throw new ArgumentNullException(nameof(authService));
         _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
         _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
         _apiEndpoint = mallSettings.Value.Yahoo.Endpoints.Order.OrderInfo
             ?? throw new ArgumentNullException(nameof(mallSettings), "Yahooの注文APIエンドポイントが設定されていません。");
      }

      /// <summary>
      /// 注文IDに応じた注文明細情報を返します。
      /// 注文IDはList<string>で複数指定可能です。
      /// 結果は一括で取得し、List<YahooOrderInfoResponse>で返します。
      /// </summary>
      public List<YahooOrderInfoResponse> GetOrderInfo(
         List<string> orderIds,
         List<string> outputFields,
         YahooShop shopCode)
      {
         // Validation Check
         Guard.AgainstNull(orderIds, nameof(orderIds));
         ApiHelpers.AreAllFieldsValid(outputFields, YahooOrderInfoFieldDefinitions.GetAllFields());
         // ShopToken 情報の取得
         var shopToken = ApiHelpers.GetShopToken(_dbContext, shopCode);

         // リクエストオブジェクトの作成
         var yahooOrderInfoResponses = new List<YahooOrderInfoResponse>();

         foreach (var orderId in orderIds)
         {
            var requestMessage = SetHttpRequest(orderId, outputFields, shopToken, shopCode);
            // PollyContext を生成
            var pollyContext = ApiHelpers.CreatePollyContext("Yahoo", requestMessage, shopToken.SellerId);

            // HTTPリクエストを送信
            var response = _requestHandler.SendAsync(requestMessage, pollyContext).Result;
            if (!response.IsSuccessStatusCode)
               throw new Exception($"Yahoo APIリクエストが失敗しました: {response.ReasonPhrase}");

            // レスポンスボディを取得
            var responseBody = response.Content.ReadAsStringAsync().Result;
            // レスポンスXMLをパース
            var parsedData = ParseResponseXml(responseBody);
            yahooOrderInfoResponses.Add(parsedData);
            Thread.Sleep(300); // 引数はミリ秒 (1秒 = 1000ミリ秒)
         }
         return yahooOrderInfoResponses;
      }

      private HttpRequestMessage SetHttpRequest(string orderId, List<string> outputFields, ShopToken shopToken, YahooShop shopCode)
      {
         var yahooOrderInfoRequestBody = new YahooOrderInfoRequestBody
         {
            Target = new YahooOrderInfoTarget
            {
               OrderId = orderId,
               Field = string.Join(",", outputFields)
            },
            SellerId = shopToken.SellerId
         };
         // XMLリクエストボディの作成
         var xmlBody = ApiHelpers.SerializeToXml(yahooOrderInfoRequestBody, "Req");
         // HTTPリクエストの作成
         var requestMessage = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint)
         {
            Content = new StringContent(xmlBody, Encoding.UTF8, "application/xml") // UTF-8を明示
         };
         // アクセストークンの取得
         var accessToken = _authService.GetValidAccessToken(shopCode);
         // リクエストヘッダの設定
         var encodedPublicKey = ApiHelpers.GetPublicKey(shopToken); // 公開鍵取得
         return ApiHelpers.SetRequestHeaders(requestMessage, accessToken, encodedPublicKey);
      }

      private YahooOrderInfoResponse ParseResponseXml(string responseXml)
      {
         var document = XDocument.Parse(responseXml);

         // ResultSetノードを解析
         var resultSetElement = document.Root;
         if (resultSetElement == null || resultSetElement.Name.LocalName != "ResultSet")
            throw new InvalidOperationException("ResultSetノードがレスポンスXMLに存在しません。");

         var response = new YahooOrderInfoResponse
         {
            ResultSet = new YahooOrderInfoResultSet
            {
               TotalResultsAvailable = int.Parse(resultSetElement.Attribute("totalResultsAvailable")?.Value ?? "0"),
               TotalResultsReturned = int.Parse(resultSetElement.Attribute("totalResultsReturned")?.Value ?? "0"),
               FirstResultPosition = int.Parse(resultSetElement.Attribute("firstResultPosition")?.Value ?? "0"),
               Result = new YahooOrderInfoResult
               {
                  Status = resultSetElement.Element("Result")?.Element("Status")?.Value,
                  OrderInfo = ParseOrderInfo(resultSetElement.Element("Result")?.Element("OrderInfo"))
               }
            }
         };
         return response;
      }

      private YahooOrderInfoDynamic ParseOrderInfo(XElement orderInfoElement)
      {
         if (orderInfoElement == null)
            return null;

         var orderInfo = new YahooOrderInfoDynamic();

         // Orderグループの動的フィールド
         var orderFields = orderInfoElement.Elements()
             .Where(e => YahooOrderInfoFieldDefinitions.Order.ContainsKey(e.Name.LocalName))
             .ToDictionary(
                 e => e.Name.LocalName,
                 e => Convert.ChangeType(e.Value, YahooOrderInfoFieldDefinitions.Order[e.Name.LocalName])
             );
         if (orderFields.Any())
            orderInfo.Order = orderFields;

         // Payノードの解析 (1:1)
         var payElement = orderInfoElement.Element("Pay");
         if (payElement != null)
            orderInfo.Pay = ParseDynamicNode(payElement, YahooOrderInfoFieldDefinitions.Pay);

         // Shipノードの解析 (1:1)
         var shipElement = orderInfoElement.Element("Ship");
         if (shipElement != null)
            orderInfo.Ship = ParseDynamicNode(shipElement, YahooOrderInfoFieldDefinitions.Ship);

         // Sellerノードの解析 (1:1)
         var sellerElement = orderInfoElement.Element("Seller");
         if (sellerElement != null)
            orderInfo.Seller = ParseDynamicNode(sellerElement, YahooOrderInfoFieldDefinitions.Seller);

         // Buyerノードの解析 (1:1)
         var buyerElement = orderInfoElement.Element("Buyer");
         if (buyerElement != null)
            orderInfo.Buyer = ParseDynamicNode(buyerElement, YahooOrderInfoFieldDefinitions.Buyer);

         // Detailノードの解析 (1:1)
         var detailElement = orderInfoElement.Element("Detail");
         if (detailElement != null)
            orderInfo.Detail = ParseDynamicNode(detailElement, YahooOrderInfoFieldDefinitions.Detail);

         // Itemノードの解析 (1:N)
         var itemElements = orderInfoElement.Elements("Item");
         foreach (var itemElement in itemElements)
         {
            orderInfo.Items.Add(ParseItem(itemElement));
         }
         return orderInfo;
      }

      private Dictionary<string, object> ParseDynamicNode(XElement node, Dictionary<string, Type> fieldDefinitions)
      {
         var fields = new Dictionary<string, object>();

         foreach (var field in node.Elements())
         {
            var fieldName = field.Name.LocalName;
            if (fieldDefinitions.TryGetValue(fieldName, out var fieldType))
               fields[fieldName] = Convert.ChangeType(field.Value, fieldType);
         }
         return fields;
      }

      private YahooOrderInfoItem ParseItem(XElement itemElement)
      {
         var item = new YahooOrderInfoItem
         {
            Item = itemElement.Elements()
               .Where(e => e.Name.LocalName != "ItemOption" && e.Name.LocalName != "Inscription")
               .ToDictionary(
                  e => e.Name.LocalName,
                  e =>
                  {
                     var fieldType = YahooOrderInfoFieldDefinitions.GetAllFields().GetValueOrDefault(e.Name.LocalName);
                     return fieldType != null ? Convert.ChangeType(e.Value, fieldType) : e.Value;
                  }
               )
         };
         // ItemOptionノードの解析 (1:N)
         item.ItemOptions = itemElement.Elements("ItemOption").Select(option => new YahooOrderInfoItemOption
         {
            Index = int.Parse(option.Element("Index")?.Value ?? "0"),
            Name = option.Element("Name")?.Value,
            Value = option.Element("Value")?.Value,
            Price = int.Parse(option.Element("Price")?.Value ?? "0")
         }).ToList();

         // Inscriptionノードの解析 (1:1)
         var inscriptionElement = itemElement.Element("Inscription");
         if (inscriptionElement != null)
         {
            item.Inscription = new YahooOrderInfoInscription
            {
               Index = int.Parse(inscriptionElement.Element("Index")?.Value ?? "0"),
               Name = inscriptionElement.Element("Name")?.Value,
               Value = inscriptionElement.Element("Value")?.Value
            };
         }
         return item;
      }

   }
}