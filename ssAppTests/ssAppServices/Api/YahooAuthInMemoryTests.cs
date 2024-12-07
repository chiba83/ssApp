#pragma warning disable CS8618, CS8629

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ssAppModels.EFModels;
using ssAppServices.Api.Yahoo;
using ssAppServices.Extensions;
using ssAppModels.ApiModels;

namespace ssApptests.ssAppServices.Api
{
   [TestFixture]
    public class YahooAuthInMemoryTests
    {
        private ServiceProvider _serviceProvider;
        private YahooAuthenticationService _yahooService;
        private ssAppDBContext _dbContext;
        private IConfiguration _configuration;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            // Test_ServiceCollectionExtensions を使用してテスト用依存関係を設定
            services.AddTestDependencies();

            // IConfiguration の登録（本番DBの接続文字列取得用）
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            services.AddSingleton<IConfiguration>(_configuration);

            // ServiceProvider の作成
            _serviceProvider = services.BuildServiceProvider();

            // テスト対象サービスの取得
            _dbContext = _serviceProvider.GetRequiredService<ssAppDBContext>();
            _yahooService = _serviceProvider.GetRequiredService<YahooAuthenticationService>();

            // In-Memory Database をリセットしてデータをシード
            _dbContext.Database.EnsureDeleted(); // データベースの削除
            _dbContext.Database.EnsureCreated(); // データベースの再作成

            // In-Memory Database に本番データを反映
            SeedInMemoryDatabase();
        }

        // テストデータのセットアップ 本番DBデータをIn-Memoryにコピー
        private void SeedInMemoryDatabase()
        {
            // 本番DB Context作成
            using (var productionDbContext = new ssAppDBContext(new DbContextOptionsBuilder<ssAppDBContext>()
                .UseSqlServer(_configuration.GetConnectionString("ssAppDBContext"))
                .Options))
            {
                // 全データを一括で In-Memory Database に設定
                var shopTokens = productionDbContext.ShopTokens.ToList();
                _dbContext.ShopTokens.AddRange(shopTokens);
                _dbContext.SaveChanges();
            }
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext?.Dispose();
            _serviceProvider?.Dispose();
        }

        [Test]
        public void T01_NewTokenRetrieval()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == "Yahoo_Yours");
            shopToken.AtexpiresAt = DateTime.Now.AddYears(-1); // 1年前
            shopToken.RtexpiresAt = DateTime.Now.AddYears(-1); // 1年前

            // 初期値を保持
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            _dbContext.SaveChanges();

            // テスト実行
            string newAccessToken = _yahooService.GetValidAccessToken(YahooShop.Yahoo_Yours); // 非同期 → 同期化

            // データを再取得して検証
            var updatedShopToken = _dbContext.ShopTokens.First(st => st.ShopCode == "Yahoo_Yours");

            // テスト項目検証
            Assert.That(newAccessToken, Is.Not.Null, "新規トークンが取得できません。");
            Assert.That(newAccessToken, Is.Not.EqualTo(initialAccessToken), "トークンが更新されていません。");
            Assert.That(updatedShopToken.AccessToken, Is.Not.EqualTo(initialAccessToken), "DB上のトークンが更新されていません。");
            Assert.That(updatedShopToken.RefreshToken, Is.Not.EqualTo(initialRefreshToken), "リフレッシュトークンが更新されていません。");
            Assert.That(updatedShopToken.AtexpiresAt, Is.InRange(DateTime.Now.AddMinutes(58), DateTime.Now.AddMinutes(62)), "アクセストークンの有効期限が正しく設定されていません。");
            Assert.That(updatedShopToken.RtexpiresAt.Value.Date, Is.EqualTo(DateTime.Now.AddDays(28).Date), "リフレッシュトークンの有効期限が正しく設定されていません。");
        }

        [Test]
        public void T02_TokenRefresh()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == "Yahoo_Yours");
            shopToken.AtexpiresAt = DateTime.Now.AddHours(-3); // 3時間前
            shopToken.RtexpiresAt = DateTime.Now.AddMonths(1); // 1か月後

            // 初期値を保持
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            _dbContext.SaveChanges();

            // テスト実行
            string refreshedAccessToken = _yahooService.GetValidAccessToken(YahooShop.Yahoo_Yours); // 非同期 → 同期化

            // データを再取得して検証
            var updatedShopToken = _dbContext.ShopTokens.First(st => st.ShopCode == "Yahoo_Yours");

            // テスト項目検証
            Assert.That(refreshedAccessToken, Is.Not.Null, "更新トークンが取得できません。");
            Assert.That(refreshedAccessToken, Is.Not.EqualTo(initialAccessToken), "トークンが更新されていません。");
            Assert.That(updatedShopToken.AccessToken, Is.Not.EqualTo(initialAccessToken), "DB上のトークンが更新されていません。");
            Assert.That(updatedShopToken.RefreshToken, Is.EqualTo(initialRefreshToken), "リフレッシュトークンが変更されています。");
            Assert.That(updatedShopToken.AtexpiresAt, Is.InRange(DateTime.Now.AddMinutes(58), DateTime.Now.AddMinutes(62)), "アクセストークンの有効期限が正しく設定されていません。");
            Assert.That(updatedShopToken.RtexpiresAt.Value.Date, Is.EqualTo(shopToken.RtexpiresAt.Value.Date), "リフレッシュトークンの有効期限が変更されています。");
        }

        [Test]
        public void T03_ExistingTokenUsage()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == "Yahoo_Yours");
            shopToken.AtexpiresAt = DateTime.Now.AddMinutes(50); // 50分後
            shopToken.RtexpiresAt = DateTime.Now.AddMonths(1); // 1か月後

            // 初期値を保持
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            _dbContext.SaveChanges();

            // テスト実行
            string currentAccessToken = _yahooService.GetValidAccessToken(YahooShop.Yahoo_Yours); // 非同期 → 同期化

            // データを再取得して検証
            var updatedShopToken = _dbContext.ShopTokens.First(st => st.ShopCode == "Yahoo_Yours");

            // テスト項目検証
            Assert.That(currentAccessToken, Is.EqualTo(initialAccessToken), "トークンが変更されています。");
            Assert.That(updatedShopToken.AccessToken, Is.EqualTo(initialAccessToken), "DB上のトークンが変更されています。");
            Assert.That(updatedShopToken.RefreshToken, Is.EqualTo(initialRefreshToken), "リフレッシュトークンが変更されています。");
            Assert.That(updatedShopToken.AtexpiresAt, Is.EqualTo(shopToken.AtexpiresAt), "アクセストークンの有効期限が変更されています。");
            Assert.That(updatedShopToken.RtexpiresAt, Is.EqualTo(shopToken.RtexpiresAt), "リフレッシュトークンの有効期限が変更されています。");
        }
    }
}
