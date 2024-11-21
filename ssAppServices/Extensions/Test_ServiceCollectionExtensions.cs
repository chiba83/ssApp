using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Net.Http;
using ssAppModels.EFModels;
using ssAppCommon.Logging;
using ssAppServices.Api;
using ssAppServices.Api.Yahoo;
using Microsoft.Extensions.Configuration;

namespace ssAppServices.Extensions
{
    public static class Test_ServiceCollectionExtensions
    {
        public static IServiceCollection AddTestDependencies(this IServiceCollection services)
        {
            // IConfiguration の構築と登録
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // DB Context In-Memory Database の設定
            services.AddDbContext<ssAppDBContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));

            // MallSettings の登録
            services.Configure<MallSettings>(configuration.GetSection("MallSettings"));

            // ロガーの登録
            services.AddSingleton<ErrorLogger>();

            // テスト用のモックHTTPリトライポリシーの登録
            var noOpPolicy = Policy.NoOpAsync<HttpResponseMessage>();
            services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(noOpPolicy);

            // HTTPリクエスト用のハンドラー
            services.AddScoped<ApiRequestHandler>();

            // Yahoo認証サービスの登録
            services.AddScoped<YahooAuthenticationService>();

            return services;
        }
    }
}
