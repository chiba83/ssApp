﻿using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using ssAppModels.EFModels;
using ssAppModels.ApiModels;

namespace ssAppServices.Api.Yahoo;

public class YahooAuthenticationService
{
   private readonly ApiRequestHandler _requestHandler;
   private readonly ssAppDBContext _dbContext;
   private readonly string _tokenEndpoint;
   private const int RefreshTokenExpiryDays = 28;
   private const int BufferMinutes = 5; // バッファ期間（5分）

   public YahooAuthenticationService(
      ApiRequestHandler requestHandler,
      ssAppDBContext dbContext,
      IOptions<MallSettings> mallSettings)
   {
      _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
      _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
      _tokenEndpoint = mallSettings.Value.Yahoo.Endpoints.AccessToken ??
            throw new ArgumentNullException(nameof(mallSettings), "Yahooのアクセストークンエンドポイントが設定されていません。");
   }

   public string GetValidAccessToken(YahooShop shopCode)
   {
      var shopToken = ApiHelpers.GetShopToken(_dbContext, shopCode.ToString());

      if (shopToken.RtexpiresAt <= DateTime.Now.AddMinutes(BufferMinutes))
      {
         if (string.IsNullOrEmpty(shopToken.AuthCode))
            throw new Exception("リフレッシュトークンが期限切れで、許可コードが設定されていません。");

         return Authorize(shopToken) ?? throw new Exception("リフレッシュトークンが期限切れで、新しいトークンの取得にも失敗しました。許可コードを再設定してください。");
      }

      if (shopToken.AtexpiresAt <= DateTime.Now.AddMinutes(BufferMinutes))
         return RefreshAccessToken(shopToken);

      return shopToken.AccessToken;
   }

   private string Authorize(ShopToken shopToken)
   {
      var parameters = new Dictionary<string, string>
      {
         { "grant_type", GrantType.authorization_code.ToString() },
         { "code", shopToken.AuthCode.TrimEnd() },
         { "redirect_uri", shopToken.CallbackUri }
      };
      var request = CreateTokenRequest(shopToken, parameters);
      return ExecuteRequestAndHandleResponse(request, shopToken, isRefresh: false);
   }

   private string RefreshAccessToken(ShopToken shopToken)
   {
      var parameters = new Dictionary<string, string>
      {
         { "grant_type", GrantType.refresh_token.ToString() },
         { "refresh_token", shopToken.RefreshToken }
      };
      var request = CreateTokenRequest(shopToken, parameters);
      return ExecuteRequestAndHandleResponse(request, shopToken, isRefresh: true);
   }

   private HttpRequestMessage CreateTokenRequest(ShopToken shopToken, Dictionary<string, string> parameters)
   {
      var authHeader = shopToken.AppType == "server"
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopToken.ClientId}:{shopToken.Secret}"))
            : Convert.ToBase64String(Encoding.UTF8.GetBytes(shopToken.ClientId));

      var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
      {
         Content = new FormUrlEncodedContent(parameters)
      };
      request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

      return request;
   }

   private string ExecuteRequestAndHandleResponse(HttpRequestMessage request, ShopToken shopToken, bool isRefresh)
   {
      // pollyContext をメソッド内で生成
      var pollyContext = ApiHelpers.CreatePollyContext("Yahoo", request, shopToken.ClientId);

      // 非同期リクエスト部分（同期で待機）
      var response = _requestHandler.SendAsync(request, pollyContext).Result;

      if (!response.IsSuccessStatusCode)
         throw new Exception($"トークン取得リクエストに失敗しました。HTTPステータスコード: {response.StatusCode}。レスポンス内容: {response.Content.ReadAsStringAsync().Result}");

      var tokenData = JObject.Parse(response.Content.ReadAsStringAsync().Result);
      string accessToken = tokenData["access_token"]?.ToString()
            ?? throw new Exception("レスポンスにアクセストークンが含まれていません。");
      string? refreshToken = isRefresh ? null : tokenData["refresh_token"]?.ToString();

      if (!isRefresh && string.IsNullOrEmpty(refreshToken))
         throw new Exception("レスポンスにリフレッシュトークンが含まれていません。");

      int expiresIn = tokenData["expires_in"]?.ToObject<int>() ?? 3600;

      UpdateTokensInDatabase(shopToken, accessToken, refreshToken, expiresIn, isRefresh);
      return accessToken;
   }

   private void UpdateTokensInDatabase(ShopToken shopToken, string accessToken, string? refreshToken, int expiresInSeconds, bool isRefresh)
   {
      shopToken.AccessToken = accessToken;
      shopToken.AtexpiresAt = DateTime.Now.AddSeconds(expiresInSeconds);
      shopToken.UpdatedAt = DateTime.Now;

      if (!isRefresh)
      {
            shopToken.RefreshToken = refreshToken;
            shopToken.RtexpiresAt = DateTime.Now.AddDays(RefreshTokenExpiryDays);
      }

      _dbContext.ShopTokens.Update(shopToken);
      _dbContext.SaveChanges();
   }
}
