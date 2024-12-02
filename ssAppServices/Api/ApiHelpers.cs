using System;
using System.Linq;
using Polly;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using ssAppModels.ApiModels;
using ssAppModels.EFModels;
using ssAppCommon.Extensions;

namespace ssAppServices.Api
{
   public static class ApiHelpers
   {
      /// <summary>
      /// 指定されたShopCodeに対応するShopTokenを取得
      /// </summary>
      public static ShopToken GetShopToken(ssAppDBContext dbContext, YahooShop shopCode)
      {
         return dbContext.ShopTokens.FirstOrDefault(st => st.ShopCode == shopCode.ToString())
             ?? throw new Exception($"指定されたShopCode（{shopCode.ToString()}）に対応するShopTokenが見つかりません。");
      }

      public static void AreAllFieldsValid(List<string> outputFields, Dictionary<string, Type> allFields)
      {
         // 無効なフィールドを特定
         var invalidFields = outputFields.Where(field => !allFields.ContainsKey(field)).ToList();
         if (invalidFields.Any())
            throw new ArgumentException($"次の選択フィールドは存在しません: {string.Join(", ", invalidFields)}");
      }

      /// <summary>
      /// 公開キーをHTTPリクエスト仕様に暗号化
      /// </summary>
      public static string? GetPublicKey(ShopToken shopToken)
      {
         Guard.AgainstNull(shopToken, nameof(shopToken));
         // 公開鍵が未設定の場合はnullを返却
         if (shopToken.PublicKey == null) return null; 
         // 有効期限が10分以内の場合はnullを返却
         if (shopToken.PkexpiresAt < DateTime.UtcNow.AddMinutes(10) 
            || shopToken.PkexpiresAt == null) return null;

         // 整形処理
         var formattedKey = shopToken.PublicKey
             .Replace("-----BEGIN PUBLIC KEY-----", "")
             .Replace("-----END PUBLIC KEY-----", "")
             .Replace("\n", "").Replace("\r", "").Trim();

         // 認証情報の生成
         string authenticationValue = $"{shopToken.SellerId}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

         // 公開鍵を用いた暗号化
         using var rsa = RSA.Create();
         rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(formattedKey), out _);
         var encryptedAuthenticationValue = rsa.Encrypt(Encoding.UTF8.GetBytes(authenticationValue), RSAEncryptionPadding.Pkcs1);

         return Convert.ToBase64String(encryptedAuthenticationValue);
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
      /// HTTPリクエストヘッダーを設定
      /// </summary>
      public static HttpRequestMessage SetRequestHeaders(HttpRequestMessage requestMessage,string accessToken, string? encodedPublicKey)
      {
         Guard.AgainstNull(requestMessage, nameof(requestMessage));
         Guard.AgainstNullOrWhiteSpace(accessToken, nameof(accessToken));
         
         //デフォルトリクエストヘッダーを設定
         requestMessage.Headers.Add("Host", "circus.shopping.yahooapis.jp");
         requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
         if (encodedPublicKey == null) return requestMessage;

         //公開キー使用のリクエストヘッダーを追加設定
         requestMessage.Headers.Add("X-sws-signature", encodedPublicKey); // 暗号化された認証情報を追加
         requestMessage.Headers.Add("X-sws-signature-version", "1"); // バージョンを固定
         return requestMessage;
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
