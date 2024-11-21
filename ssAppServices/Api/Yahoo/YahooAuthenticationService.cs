using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using ssAppModels;
using ssAppModels.EFModels;
using ssAppServices.Api;

namespace ssAppServices.Api.Yahoo
{
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
            if (mallSettings?.Value?.Yahoo?.Endpoints?.AccessToken == null)
                throw new ArgumentNullException(nameof(mallSettings), "Yahooのアクセストークンエンドポイントが設定されていません。");
            _tokenEndpoint = mallSettings.Value.Yahoo.Endpoints.AccessToken;
        }

        public async Task<string> GetValidAccessTokenAsync(YahooShop shopCode)
        {
            var shopToken = await GetShopTokenAsync(shopCode);

            if (shopToken.RtexpiresAt <= DateTime.Now.AddMinutes(BufferMinutes))
            {
                if (string.IsNullOrEmpty(shopToken.AuthCode))
                    throw new Exception("リフレッシュトークンが期限切れで、許可コードが設定されていません。");

                return await AuthorizeAsync(shopToken)
                    ?? throw new Exception("リフレッシュトークンが期限切れで、新しいトークンの取得にも失敗しました。許可コードを再設定してください。");
            }

            if (shopToken.AtexpiresAt <= DateTime.Now.AddMinutes(BufferMinutes))
                return await RefreshAccessTokenAsync(shopToken);

            return shopToken.AccessToken;
        }

        private async Task<ShopToken> GetShopTokenAsync(YahooShop shopCode)
        {
            return await _dbContext.ShopTokens
                .FirstOrDefaultAsync(st => st.ShopCode == shopCode.ToString())
                ?? throw new Exception($"指定されたShopCode（{shopCode}）に対応するShopTokenが見つかりません。");
        }

        private async Task<string> AuthorizeAsync(ShopToken shopToken)
        {
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", GrantType.authorization_code.ToString() },
                { "code", shopToken.AuthCode.TrimEnd() },
                { "redirect_uri", shopToken.CallbackUri }
            };

            var request = CreateTokenRequest(shopToken, parameters);
            return await ExecuteRequestAndHandleResponse(request, shopToken, isRefresh: false);
        }

        private async Task<string> RefreshAccessTokenAsync(ShopToken shopToken)
        {
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", GrantType.refresh_token.ToString() },
                { "refresh_token", shopToken.RefreshToken }
            };

            var request = CreateTokenRequest(shopToken, parameters);
            return await ExecuteRequestAndHandleResponse(request, shopToken, isRefresh: true);
        }

        private HttpRequestMessage CreateTokenRequest(ShopToken shopToken, Dictionary<string, string> parameters)
        {
            var authHeader = shopToken.AppType == "server"
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopToken.ClientId}:{shopToken.Secret}"))
                : shopToken.ClientId;

            var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(parameters)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            return request;
        }

        private async Task<string> ExecuteRequestAndHandleResponse(HttpRequestMessage request, ShopToken shopToken, bool isRefresh)
        {
            var context = new Polly.Context
            {
                { "Vendor", "Yahoo" },
                { "ApiEndpoint", request.RequestUri.ToString() },
                { "HttpMethod", request.Method.ToString() },
                { "UserId", shopToken.ClientId }
            };

            var response = await _requestHandler.SendAsync(request, context);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"トークン取得リクエストに失敗しました。HTTPステータスコード: {response.StatusCode}");

            var tokenData = JObject.Parse(await response.Content.ReadAsStringAsync());
            string accessToken = tokenData["access_token"]?.ToString()
                ?? throw new Exception("レスポンスにアクセストークンが含まれていません。");
            string? refreshToken = isRefresh ? null : tokenData["refresh_token"]?.ToString();

            if (!isRefresh && string.IsNullOrEmpty(refreshToken))
                throw new Exception("レスポンスにリフレッシュトークンが含まれていません。");
            int expiresIn = tokenData["expires_in"]?.ToObject<int>() ?? 3600;

            await UpdateTokensInDatabase(shopToken, accessToken, refreshToken, expiresIn, isRefresh);
            return accessToken;
        }

        private async Task UpdateTokensInDatabase(ShopToken shopToken, string accessToken, string? refreshToken, int expiresInSeconds, bool isRefresh)
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
            await _dbContext.SaveChangesAsync();
        }
    }
}
