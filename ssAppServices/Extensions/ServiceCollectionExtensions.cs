﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ssAppModels.EFModels;
using ssAppCommon.Logging;
using ssAppServices.Api;
using ssAppServices.Api.Yahoo;
using ssAppServices.Apps;

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
         services.AddScoped<ErrorLogger>();
         // ServiceErrHandler の登録（ポリシー管理を一元化）
         services.AddScoped<ServiceErrHandler>();
         // HTTPリクエスト用のハンドラー
         services.AddScoped<ApiRequestHandler>();
         // Yahoo認証サービス
         services.AddScoped<YahooAuthenticationService>();
         // Yahoo注文情報検索サービス
         services.AddScoped<YahooOrderList>();
         // Yahoo注文詳細サービス
         services.AddScoped<YahooOrderInfo>();
         // DailyOrderNews更新サービス
         services.AddScoped<SetDailyOrderNews>();

         return services;
      }
   }
}
