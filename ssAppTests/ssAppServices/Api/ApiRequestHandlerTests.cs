#pragma warning disable CS8618, CS8629

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;
using System.Web;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api;
using ssAppModels.ApiModels;

namespace ssApptests.ssAppServices.Api
{
   [TestFixture]
   public class ApiRequestHandlerTests
   {
      private ServiceProvider _serviceProvider;
      private ApiRequestHandler _apiRequestHandler;
      private ssAppDBContext _dbContext;
      private IOptions<MallSettings> _mallSettings;

      [SetUp]
      public void Setup()
      {
         var services = new ServiceCollection();

         // appsettings.json を読み込み、StartupのConfigureServicesを流用
         var startup = new Startup();
         startup.ConfigureServices(services);

         _serviceProvider = services.BuildServiceProvider();

         // テスト対象サービスと依存サービスの取得
         _dbContext = _serviceProvider.GetRequiredService<ssAppDBContext>();
         _mallSettings = _serviceProvider.GetRequiredService<IOptions<MallSettings>>();
         _apiRequestHandler = _serviceProvider.GetRequiredService<ApiRequestHandler>();
      }

        [TearDown]
        public void TearDown()
        {
            _dbContext?.Dispose();
            _serviceProvider?.Dispose();
        }

        // 現状の成功シナリオ
        [Test]
        public async Task SendAsync_Success()
        {
            var shopToken = ssAppDBHelper.GetShopToken(_dbContext, YahooShop.Yahoo_Yours);
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", GrantType.refresh_token.ToString() },
                { "refresh_token", shopToken.RefreshToken }
            };
            var authHeader = shopToken.AppType == "server"
                ? Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopToken.ClientId}:{shopToken.Secret}"))
                : shopToken.ClientId;
            var _tokenEndpoint = _mallSettings.Value.Yahoo.Endpoints.AccessToken;
            var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(parameters)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

            // Pollyコンテキスト生成
            var pollyContext = ApiHelpers.CreatePollyContext("Yahoo", request, shopToken.ClientId);

            // テスト実行（通信成功を確認）
            var response = await _apiRequestHandler.SendAsync(request, pollyContext);

            // 結果を検証（通信成功）
            Assert.That(response.IsSuccessStatusCode, Is.True, "通信が成功しませんでした。");

            // レスポンス内容を検証
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.That(!string.IsNullOrWhiteSpace(responseBody), Is.True, "レスポンスボディが空です。");
        }

        // 通信失敗シナリオ（400エラーを発生）
        [Test]
        public async Task SendAsync_BadRequest()
        {
            var shopToken = ssAppDBHelper.GetShopToken(_dbContext, YahooShop.Yahoo_Yours);
            var grant_type = $"invalid_grant_{DateTime.Now.ToString()}";
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", grant_type },
                { "refresh_token", shopToken.RefreshToken }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, _mallSettings.Value.Yahoo.Endpoints.AccessToken)
            {
                Content = new FormUrlEncodedContent(parameters)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                shopToken.AppType == "server"
                    ? Convert.ToBase64String(Encoding.UTF8.GetBytes($"{shopToken.ClientId}:{shopToken.Secret}"))
                    : shopToken.ClientId
            );

            // Pollyコンテキスト生成
            var pollyContext = ApiHelpers.CreatePollyContext("Yahoo", request, shopToken.ClientId);

            // APIリクエストを送信
            var response = await _apiRequestHandler.SendAsync(request, pollyContext);

            // Assert: レスポンスの検証
            Assert.That(response, Is.Not.Null, "レスポンスが null です。");
            Assert.That((int)response.StatusCode, Is.EqualTo(400), "HTTP 400 が返されていません。");

            // Assert: ErrorLog の検証
            var encodedGrantType = HttpUtility.UrlEncode(grant_type); // grant_type を URL エンコード
            var containsGrantType = _dbContext.ErrorLogs
                .Any(log => log.ReqBody != null && log.ReqBody.Contains($"grant_type={encodedGrantType}"));

            Assert.That(containsGrantType, Is.True, $"ErrorLog に ReqBody '{encodedGrantType}' を含むレコードが見つかりませんでした。");

        }
    }
}
