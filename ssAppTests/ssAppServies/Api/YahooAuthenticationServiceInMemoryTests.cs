using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ssAppModels.EFModels;
using ssAppModels;
using ssAppCommon.Logging;
using ssAppServices.Api;
using ssAppServices;

namespace ssApptests.ssAppServies.Api
{
    [TestFixture]
    public class YahooAuthenticationServiceInMemoryTests
    {
        private Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private ServiceProvider _serviceProvider;
        private YahooAuthenticationService _yahooService;
        private ssAppDBContext _dbContext;
        private YahooShop _shopCode = YahooShop.Yahoo_Yours;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();

            // In-Memory Database の設定
            services.AddDbContext<ssAppDBContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));

            // IConfiguration の登録
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(_configuration);

            // ApiClient の登録
            services.AddHttpClient<ApiClientHandler>();

            // ErrorLogger の登録
            services.AddSingleton<ErrorLogger>();

            // ServiceErrHandler の登録
            services.AddSingleton<ServiceErrHandler>();

            // MallSettings の登録
            services.Configure<MallSettings>(_configuration.GetSection("MallSettings"));

            // YahooAuthenticationService の登録
            services.AddScoped<YahooAuthenticationService>();

            _serviceProvider = services.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<ssAppDBContext>();

            // 本番DBからデータを取得して In-Memory Database に反映
            using (var productionDbContext = new ssAppDBContext(new DbContextOptionsBuilder<ssAppDBContext>()
                .UseSqlServer(_configuration.GetConnectionString("ssAppDBContext"))
                .Options))
            {
                var shopTokens = productionDbContext.ShopTokens.ToList();
                foreach (var token in shopTokens)
                {
                    _dbContext.ShopTokens.Add(new ShopToken
                    {
                        ShopCode = token.ShopCode,
                        AuthCode = token.AuthCode,
                        ClientId = token.ClientId,
                        Secret = token.Secret,
                        AppType = token.AppType,
                        CallbackUri = token.CallbackUri,
                        AtexpiresAt = token.AtexpiresAt,
                        RtexpiresAt = token.RtexpiresAt,
                        AccessToken = token.AccessToken,
                        RefreshToken = token.RefreshToken
                    });
                }
                _dbContext.SaveChanges();
            }

            _yahooService = _serviceProvider.GetRequiredService<YahooAuthenticationService>();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext?.Dispose();
            _serviceProvider?.Dispose();
        }

        [Test]
        public async Task Scenario1_NewTokenRetrieval()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());
            shopToken.AtexpiresAt = DateTime.Now.AddYears(-1); // 1年前
            shopToken.RtexpiresAt = DateTime.Now.AddYears(-1); // 1年前

            // 初期値をバックアップ
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            await _dbContext.SaveChangesAsync();

            // テスト実行
            string newAccessToken = await _yahooService.GetValidAccessTokenAsync(_shopCode);

            // データを再取得して検証
            var updatedShopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());

            // テスト項目検証
            Assert.That(newAccessToken, Is.Not.Null, "新規トークンが取得できません。");
            Assert.That(newAccessToken, Is.Not.EqualTo(initialAccessToken), "トークンが更新されていません。");
            Assert.That(updatedShopToken.AccessToken, Is.Not.EqualTo(initialAccessToken), "DB上のトークンが更新されていません。");
            Assert.That(updatedShopToken.RefreshToken, Is.Not.EqualTo(initialRefreshToken), "リフレッシュトークンが更新されていません。");
            Assert.That(updatedShopToken.AtexpiresAt, Is.InRange(DateTime.Now.AddMinutes(58), DateTime.Now.AddMinutes(62)), "アクセストークンの有効期限が正しく設定されていません。");
            Assert.That(updatedShopToken.RtexpiresAt.Value.Date, Is.EqualTo(DateTime.Now.AddDays(28).Date), "リフレッシュトークンの有効期限が正しく設定されていません。");
        }

        [Test]
        public async Task Scenario2_TokenRefresh()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());
            shopToken.AtexpiresAt = DateTime.Now.AddHours(-3); // 3時間前
            shopToken.RtexpiresAt = DateTime.Now.AddMonths(1); // 1か月後

            // 初期値をバックアップ
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            await _dbContext.SaveChangesAsync();

            // テスト実行
            string refreshedAccessToken = await _yahooService.GetValidAccessTokenAsync(_shopCode);

            // データを再取得して検証
            var updatedShopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());

            // テスト項目検証
            Assert.That(refreshedAccessToken, Is.Not.Null, "更新トークンが取得できません。");
            Assert.That(refreshedAccessToken, Is.Not.EqualTo(initialAccessToken), "トークンが更新されていません。");
            Assert.That(updatedShopToken.AccessToken, Is.Not.EqualTo(initialAccessToken), "DB上のトークンが更新されていません。");
            Assert.That(updatedShopToken.RefreshToken, Is.EqualTo(initialRefreshToken), "リフレッシュトークンが変更されています。");
            Assert.That(updatedShopToken.AtexpiresAt, Is.InRange(DateTime.Now.AddMinutes(58), DateTime.Now.AddMinutes(62)), "アクセストークンの有効期限が正しく設定されていません。");
            Assert.That(updatedShopToken.RtexpiresAt.Value.Date, Is.EqualTo(shopToken.RtexpiresAt.Value.Date), "リフレッシュトークンの有効期限が変更されています。");
        }

        [Test]
        public async Task Scenario3_ExistingTokenUsage()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());
            shopToken.AtexpiresAt = DateTime.Now.AddMinutes(50); // 50分後
            shopToken.RtexpiresAt = DateTime.Now.AddMonths(1); // 1か月後

            // 初期値をバックアップ
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            await _dbContext.SaveChangesAsync();

            // テスト実行
            string currentAccessToken = await _yahooService.GetValidAccessTokenAsync(_shopCode);

            // データを再取得して検証
            var updatedShopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());

            // テスト項目検証
            Assert.That(currentAccessToken, Is.EqualTo(initialAccessToken), "トークンが変更されています。");
            Assert.That(updatedShopToken.AccessToken, Is.EqualTo(initialAccessToken), "DB上のトークンが変更されています。");
            Assert.That(updatedShopToken.RefreshToken, Is.EqualTo(initialRefreshToken), "リフレッシュトークンが変更されています。");
            Assert.That(updatedShopToken.AtexpiresAt, Is.EqualTo(shopToken.AtexpiresAt), "アクセストークンの有効期限が変更されています。");
            Assert.That(updatedShopToken.RtexpiresAt, Is.EqualTo(shopToken.RtexpiresAt), "リフレッシュトークンの有効期限が変更されています。");
        }
    }
}
