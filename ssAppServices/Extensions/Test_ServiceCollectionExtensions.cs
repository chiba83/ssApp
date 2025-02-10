using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Net.Http;
using ssAppModels.EFModels;
using ssAppCommon.Logging;
using ssAppServices.Api;
using ssAppServices.Api.Yahoo;
using Microsoft.Extensions.Configuration;
using ssAppServices.Apps;
using ssAppServices.Api.Rakuten;
using System.Runtime.InteropServices;

namespace ssAppServices.Extensions;

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
      services.AddScoped<ErrorLogger>();
      // テスト用の ServiceErrHandler の登録
      services.AddScoped<ServiceErrHandler>();
      // Defaultポリシーの登録
      services.AddSingleton<IAsyncPolicy>(provider =>
         provider.GetRequiredService<ServiceErrHandler>()
            .BuildDefaultPolicy()); // BuildDefaultPolicy を直接呼び出し
      // HTTPリトライポリシーの登録
      services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(provider =>
         provider.GetRequiredService<ServiceErrHandler>()
            .BuildHttpPolicy()); // BuildHttpPolicy を直接呼び出し
      // HTTPリクエスト用のハンドラー
      services.AddScoped<ApiRequestHandler>();
      // Yahoo認証サービスの登録
      services.AddScoped<YahooAuthenticationService>();
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
