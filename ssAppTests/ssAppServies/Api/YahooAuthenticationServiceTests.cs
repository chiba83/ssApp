﻿#pragma warning disable CS8618, CS8629

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ssAppModels;
using ssAppModels.EFModels;
using ssAppServices;
using ssAppServices.Api.Yahoo;
using ssAppServices.Extensions;

namespace ssApptests.ssAppServies.Api
{
    [TestFixture]
    public class YahooAuthenticationServiceTests
    {
        private ServiceProvider _serviceProvider;
        private YahooAuthenticationService _yahooService;
        private ssAppDBContext _dbContext;
        private YahooShop _shopCode = YahooShop.Yahoo_Yours;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();

            // Startup クラスを使用して DI 設定を適用
            var startup = new Startup();
            startup.ConfigureServices(services);

            // ServiceProvider の作成
            _serviceProvider = services.BuildServiceProvider();

            // テスト対象サービスと依存サービスの取得
            _dbContext = _serviceProvider.GetRequiredService<ssAppDBContext>();
            _yahooService = _serviceProvider.GetRequiredService<YahooAuthenticationService>();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext?.Dispose();
            _serviceProvider?.Dispose();
        }

        [Test]
        public void Scenario1_NewTokenRetrieval()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());
            shopToken.AtexpiresAt = DateTime.Now.AddYears(-1); // 1年前
            shopToken.RtexpiresAt = DateTime.Now.AddYears(-1); // 1年前

            // 初期値を保持
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            _dbContext.SaveChanges();

            // テスト実行
            string newAccessToken = _yahooService.GetValidAccessToken(_shopCode); // 同期化

            // データを再取得して検証
            var updatedShopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());

            // テスト項目検証
            Assert.That(newAccessToken, Is.Not.Null, "新規トークンが取得できません。");
            Assert.That(newAccessToken, Is.Not.EqualTo(initialAccessToken), "トークンが更新されていません。");
            Assert.That(updatedShopToken.RefreshToken, Is.Not.EqualTo(initialRefreshToken), "リフレッシュトークンが更新されていません。");
            Assert.That(updatedShopToken.AtexpiresAt, Is.InRange(DateTime.Now.AddMinutes(58), DateTime.Now.AddMinutes(62)), "アクセストークンの有効期限が正しく設定されていません。");
            Assert.That(updatedShopToken.RtexpiresAt.Value.Date, Is.EqualTo(DateTime.Now.AddDays(28).Date), "リフレッシュトークンの有効期限が正しく設定されていません。");
        }

        [Test]
        public void Scenario2_TokenRefresh()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());
            shopToken.AtexpiresAt = DateTime.Now.AddHours(-3); // 3時間前
            shopToken.RtexpiresAt = DateTime.Now.AddMonths(1); // 1か月後

            // 初期値を保持
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            _dbContext.SaveChanges();

            // テスト実行
            string refreshedAccessToken = _yahooService.GetValidAccessToken(_shopCode); // 同期化

            // データを再取得して検証
            var updatedShopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());

            // テスト項目検証
            Assert.That(refreshedAccessToken, Is.Not.Null, "更新トークンが取得できません。");
            Assert.That(refreshedAccessToken, Is.Not.EqualTo(initialAccessToken), "トークンが更新されていません。");
            Assert.That(updatedShopToken.RefreshToken, Is.EqualTo(initialRefreshToken), "リフレッシュトークンが変更されています。");
            Assert.That(updatedShopToken.AtexpiresAt, Is.InRange(DateTime.Now.AddMinutes(58), DateTime.Now.AddMinutes(62)), "アクセストークンの有効期限が正しく設定されていません。");
            Assert.That(updatedShopToken.RtexpiresAt.Value.Date, Is.EqualTo(shopToken.RtexpiresAt.Value.Date), "リフレッシュトークンの有効期限が変更されています。");
        }

        [Test]
        public void Scenario3_ExistingTokenUsage()
        {
            // モック設定
            var shopToken = _dbContext.ShopTokens.First(st => st.ShopCode == _shopCode.ToString());
            shopToken.AtexpiresAt = DateTime.Now.AddMinutes(50); // 50分後
            shopToken.RtexpiresAt = DateTime.Now.AddMonths(1); // 1か月後

            // 初期値を保持
            var initialAccessToken = shopToken.AccessToken;
            var initialRefreshToken = shopToken.RefreshToken;

            _dbContext.Update(shopToken);
            _dbContext.SaveChanges();

            // テスト実行
            string currentAccessToken = _yahooService.GetValidAccessToken(_shopCode); // 同期化

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
