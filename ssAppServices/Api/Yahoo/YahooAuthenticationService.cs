using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using ssApp.Models;
using ssAppModels.EFModels;
using System.Net.Http.Headers;
using System.Text;

public class YahooAuthenticationService
{
    private readonly ssAppDBContext _dbContext;
    private readonly ApiClientHandler _apiClienthHandler;
    private readonly string tokenEndpoint;
    private const int RefreshTokenExpiryDays = 28;
    private const int BufferMinutes = 5; // バッファ期間（5分）

    public YahooAuthenticationService(ssAppDBContext dbContext, IConfiguration configuration, ApiClientHandler apiClienthHandler)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _apiClienthHandler = apiClienthHandler ?? throw new ArgumentNullException(nameof(apiClienthHandler));
        tokenEndpoint = configuration["MallSettings:Yahoo:Endpoints:AccessToken"]
            ?? throw new Exception("エンドポイント設定エラー。URLを設定してください。");
    }

    public async Task<string> GetValidAccessTokenAsync(YahooShop shopCode)
    {
        // ShopToken を取得
        var shopToken = await GetShopTokenAsync(shopCode);

        // リフレッシュトークン期限切れ判定
        if (shopToken.RtexpiresAt <= DateTime.Now.AddMinutes(BufferMinutes))
        {
            if (string.IsNullOrEmpty(shopToken.AuthCode))
                throw new Exception("リフレッシュトークンが期限切れ、かつ新規トークン取得のための許可コードがNullです。");

            return await AuthorizeAsync(shopToken)
                ?? throw new Exception("リフレッシュトークンが期限切れ、かつ新規トークン取得にも失敗しました。許可コードを再取得してください。");
        }

        // アクセストークン期限切れ判定
        if (shopToken.AtexpiresAt <= DateTime.Now.AddMinutes(BufferMinutes))
            return await RefreshAccessTokenAsync(shopToken);

        return shopToken.AccessToken;
    }

    private async Task<ShopToken> GetShopTokenAsync(YahooShop shopCode)
    {
        return await _dbContext.ShopTokens
            .FirstOrDefaultAsync(st => st.ShopCode == shopCode.ToString())
            ?? throw new Exception("指定された ShopCode に対応する ShopToken が見つかりません。");
    }

    private async Task<string> AuthorizeAsync(ShopToken shopToken)
    {
        var parameters = new Dictionary<string, string>
        {
            { "grant_type", GrantType.authorization_code.ToString() },
            { "code", shopToken.AuthCode.TrimEnd() },
            { "redirect_uri", shopToken.CallbackUri }
        };

        return await RequestAccessTokenAsync(parameters, shopToken, isRefresh: false);
    }

    private async Task<string> RefreshAccessTokenAsync(ShopToken shopToken)
    {
        var parameters = new Dictionary<string, string>
        {
            { "grant_type", GrantType.refresh_token.ToString() },
            { "refresh_token", shopToken.RefreshToken }
        };

        return await RequestAccessTokenAsync(parameters, shopToken, isRefresh: true);
    }

    private async Task<string> RequestAccessTokenAsync(Dictionary<string, string> parameters, ShopToken shopToken, bool isRefresh)
    {
        string authHeader = shopToken.AppType == "server"
            ? Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopToken.ClientId}:{shopToken.Secret}"))
            : shopToken.ClientId;

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        requestMessage.Content = new FormUrlEncodedContent(parameters);
        var response = await _apiClienthHandler.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"HTTP Status Code: {response.StatusCode}");
        var tokenData = JObject.Parse(await response.Content.ReadAsStringAsync());

        string accessToken = tokenData["access_token"]?.ToString()
            ?? throw new Exception("リクエストは成功していますが、Access Tokenが取得できません。");

        string? refreshToken = isRefresh ? null : tokenData["refresh_token"]?.ToString();
        if (!isRefresh && string.IsNullOrEmpty(refreshToken))
            throw new Exception("リクエストは成功していますが、Refresh Tokenが取得できません。");
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
