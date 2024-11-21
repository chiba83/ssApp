using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Polly;
using ssAppModels.EFModels;
using ssAppCommon.Logging;
using ssAppServices.Api;
using ssAppServices.Api.Yahoo;

namespace ssAppServices.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddProjectDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext の登録（appsettings.jsonの接続文字列を利用）
            var connectionString = configuration.GetConnectionString("ssAppDBContext");
            services.AddDbContext<ssAppDBContext>(options =>
                options.UseSqlServer(connectionString));

            // MallSettings の登録
            services.Configure<MallSettings>(configuration.GetSection("MallSettings"));

            // ロガーの登録
            services.AddSingleton<ErrorLogger>();

            // エラーハンドラーの登録）
            services.AddSingleton<ServiceErrHandler>();

            // HTTPリトライポリシーの登録
            services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(provider =>
            {
                var errHandler = provider.GetRequiredService<ServiceErrHandler>();
                return errHandler.BuildHttpPolicy();
            });

            // HTTPリクエスト用のハンドラー
            services.AddScoped<ApiRequestHandler>();

            // Yahoo認証サービス
            services.AddScoped<YahooAuthenticationService>();

            return services;
        }
    }
}
