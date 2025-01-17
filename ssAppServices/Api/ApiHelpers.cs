using Polly;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using ssAppModels.EFModels;
using ssAppCommon.Extensions;
using Newtonsoft.Json;

namespace ssAppServices.Api
{
   public static class ApiHelpers
   {
      public static void AreAllFieldsValid(List<string> outputFields, Dictionary<string, Type> allFields)
      {
         // 無効なフィールドを特定
         var invalidFields = outputFields.Where(field => !allFields.ContainsKey(field)).ToList();
         if (invalidFields.Any())
            throw new ArgumentException($"次の選択フィールドは存在しません: {string.Join(", ", invalidFields)}");
      }

      /// <summary>
      /// Yahoo 公開キーをHTTPリクエスト仕様に暗号化
      /// </summary>
      public static (string? key, string? version) GetPublicKey(ShopToken shopToken)
      {
         Guard.AgainstNull(shopToken, nameof(shopToken));
         // 公開鍵が未設定の場合はnullを返却
         if (shopToken.PublicKey == null) return (null, null); 
         // 有効期限が10分以内の場合はnullを返却
         if (shopToken.PkexpiresAt < DateTime.UtcNow.AddMinutes(10) 
            || shopToken.PkexpiresAt == null) return (null, null);
         // 公開鍵versionが未設定の場合はnullを返却
         if (shopToken.PublicKeyVersion == null) return (null, null);

         // 公開キーの整形
         var formattedKey = shopToken.PublicKey
             .Replace("-----BEGIN PUBLIC KEY-----", "")
             .Replace("-----END PUBLIC KEY-----", "")
             .Replace("\n", "")
             .Replace("\r", "") // Windowsの改行にも対応
             .Trim();

         // 認証情報の生成
         string authenticationValue = $"{shopToken.SellerId}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

         // 公開鍵を用いた暗号化
         using var rsa = RSA.Create();
         rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(formattedKey), out _);
         var encryptedAuthenticationValue = rsa.Encrypt(Encoding.UTF8.GetBytes(authenticationValue), RSAEncryptionPadding.Pkcs1);

         return (Convert.ToBase64String(encryptedAuthenticationValue)
            , shopToken.PublicKeyVersion.ToString());
      }

      /// <summary>
      /// XMLシリアライズ
      /// </summary>
      public static string SerializeToXml<T>(T requestObject, string rootName)
      {
         var serializer = new XmlSerializer(typeof(T), new XmlRootAttribute(rootName));
         var emptyNamespaces = new XmlSerializerNamespaces();
         emptyNamespaces.Add("", ""); // 名前空間を削除

         var xmlSettings = new XmlWriterSettings
         {
            Encoding = Encoding.UTF8, // UTF-8 エンコーディング
            OmitXmlDeclaration = false, // XML宣言を追加
            Indent = true
         };

         using var stringWriter = new Utf8StringWriter();
         using var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings);
         serializer.Serialize(xmlWriter, requestObject, emptyNamespaces);
         return stringWriter.ToString();
      }

      /// <summary>
      /// Yahoo HTTPリクエストヘッダーを設定
      /// </summary>
      public static HttpRequestMessage SetRequestHeaders(HttpRequestMessage requestMessage,string accessToken, string? encodedPublicKey, string? KeyVersion)
      {
         Guard.AgainstNull(requestMessage, nameof(requestMessage));
         Guard.AgainstNullOrWhiteSpace(accessToken, nameof(accessToken));
         
         //デフォルトリクエストヘッダーを設定
         requestMessage.Headers.Add("Host", "circus.shopping.yahooapis.jp");
         requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
         if (encodedPublicKey == null) return requestMessage;

         //公開キー使用のリクエストヘッダーを追加設定
         requestMessage.Headers.Add("X-sws-signature", encodedPublicKey); // 暗号化された認証情報を追加
         requestMessage.Headers.Add("X-sws-signature-version", KeyVersion); // Keyバージョン
         return requestMessage;
      }

      /// <summary>
      /// Rakuten HTTPリクエストを設定
      /// </summary>
      public static HttpRequestMessage SetRakutenRequest(string apiEndpoint, object requestParameter, ShopToken shopToken)
      {
         Guard.AgainstNullOrWhiteSpace(apiEndpoint, nameof(apiEndpoint));
         Guard.AgainstNull(requestParameter, nameof(requestParameter));
         Guard.AgainstNull(shopToken, nameof(shopToken));

         var request = new HttpRequestMessage(HttpMethod.Post, apiEndpoint);

         // リクエストヘッダーを設定
         string credentials = $"{shopToken.Secret}:{shopToken.ClientId}";
         string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
         request.Headers.Add("Authorization", $"ESA {base64Credentials}");

         // リクエストパラメータを設定
         var jsonContent = JsonConvert.SerializeObject(requestParameter, new JsonSerializerSettings
         {
            NullValueHandling = NullValueHandling.Ignore
         });
         request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

         return request;
      }

      /// <summary>
      /// Polly.Context を生成するヘルパーメソッド
      /// </summary>
      public static Context CreatePollyContext(string vendor, HttpRequestMessage request, string userId)
      {
         if (request == null) throw new ArgumentNullException(nameof(request));

         return new Context
         {
            { "Vendor", vendor },
            { "ApiEndpoint", request.RequestUri?.ToString() },
            { "HttpMethod", request.Method.ToString() },
            { "UserId", userId },
            { "HttpRequest", request }
         };
      }
   }
}
