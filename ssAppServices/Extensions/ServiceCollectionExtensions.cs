using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ssAppModels.EFModels;
using ssAppCommon.Logging;
using ssAppServices.Api;
using ssAppServices.Api.Yahoo;
using ssAppServices.Apps;
using ssAppServices.Api.Rakuten;

namespace ssAppServices.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddProjectDependencies(this IServiceCollection services, IConfiguration configuration)
  {
    // DbContext の登録（appsettings.jsonの接続文字列を利用）
    // 環境判定（Production 以外ならテスト用とする）
    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
    var connectionName = isProduction ? "ssAppDBContext" : "ssAppDBContextTest";
    var connectionString = configuration.GetConnectionString(connectionName); 
    services.AddDbContext<ssAppDBContext>(x => x.UseSqlServer(connectionString));
    // MallSettings の登録
    services.Configure<MallSettings>(configuration.GetSection("MallSettings"));
    // ロガーの登録
    services.AddScoped<ErrorLogger>();
    // ServiceErrHandler の登録（ポリシー管理を一元化）
    services.AddScoped<ServiceErrHandler>();
    // HTTPリクエスト用のハンドラー
    services.AddScoped<ApiRequestHandler>();
    // Yahoo認証サービス
    services.AddScoped<YahooAuthenticationService>();
    // YahooAPI実行サービス
    services.AddScoped<YahooApiExecute>();
    // Yahoo注文情報検索サービス
    services.AddScoped<YahooOrderList>();
    // Yahoo注文詳細サービス
    services.AddScoped<YahooOrderInfo>();
    // RakutenAPI実行サービス
    services.AddScoped<RakutenApiExecute>();
    // Rakuten注文情報検索サービス
    services.AddScoped<RakutenSearchOrder>();
    // Rakuten注文詳細ービス
    services.AddScoped<RakutenGetOrder>();
    // DailyOrderNews更新サービス
    services.AddScoped<SetDailyOrderNews>();

    return services;
   }
}
