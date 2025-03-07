using Hangfire;
using Hangfire.SqlServer;
using Hangfire.Common;
using ssAppServices.Apps;
using ssAppServices.Extensions;
using ssAppModels.ApiModels;
using Hangfire.Server;

var builder = WebApplication.CreateBuilder(args);

// ログ出力の共通メソッド
static void Log(string message)
{
   var logPath = "C:\\inetpub\\logs\\hangfire\\hangfire_log.txt";
   File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
}

// Load configuration from ssAppService
try
{
   var basePath = AppContext.BaseDirectory;

   builder.Configuration
       .SetBasePath(basePath)
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

   Log("Configuration loaded successfully.");
}
catch (Exception ex)
{
   Log($"Error loading configuration: {ex.Message}");
   throw;
}

// Configure services
try
{
   builder.Services.AddHangfire(config => config
      .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
      .UseSimpleAssemblyNameTypeSerializer()
      .UseRecommendedSerializerSettings()
      .UseSqlServerStorage(builder.Configuration.GetConnectionString("ssAppDBContext"), new SqlServerStorageOptions
      {
         CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
         SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
         QueuePollInterval = TimeSpan.Zero,
         UseRecommendedIsolationLevel = true,
         DisableGlobalLocks = true
      })
      .WithJobExpirationTimeout(TimeSpan.FromDays(1))
   );

   builder.Services.AddProjectDependencies(builder.Configuration);
   builder.Services.AddHangfireServer();

   Log("Services configured successfully.");
}
catch (Exception ex)
{
   Log($"Error configuring services: {ex.Message}");
   throw;
}

var app = builder.Build();

// Base path for the application
app.UsePathBase("/job");

// Configure the HTTP request pipeline
app.UseRouting();

// Hangfire Dashboard configuration
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
   Authorization = [new AllowAllAuthorizationFilter()]
});

// Add event for job performance logging
GlobalJobFilters.Filters.Add(new LogJobFilter(Log));

try
{
   // Add the recurring jobs
   RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "NewOrders - Yahoo_LARAL",
      x => x.FetchDailyOrderFromYahoo(YahooShop.Yahoo_LARAL, OrderStatus.NewOrder, null, null, true, UpdateMode.Replace),
      "0,30 5-23 * * *",
      new RecurringJobOptions
      {
         TimeZone = TimeZoneInfo.Local
      }
   );
   RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "NewOrders - Yahoo_Yours",
      x => x.FetchDailyOrderFromYahoo(YahooShop.Yahoo_Yours, OrderStatus.NewOrder, null, null, true, UpdateMode.Replace),
      "0,30 5-23 * * *",
      new RecurringJobOptions
      {
         TimeZone = TimeZoneInfo.Local
      }
   );
   RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "NewOrders - Rakuten_ENZO",
      x => x.FetchDailyOrderFromRakuten(RakutenShop.Rakuten_ENZO, OrderStatus.NewOrder, null, null, true, UpdateMode.Replace),
      "0,30 5-23 * * *",
      new RecurringJobOptions
      {
         TimeZone = TimeZoneInfo.Local
      }
   );
   RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "Packing - Yahoo_LARAL",
      x => x.FetchDailyOrderFromYahoo(YahooShop.Yahoo_LARAL, OrderStatus.Packing, null, null, true, UpdateMode.Replace),
      Cron.Never // スケジュール実行なし（手動専用）
   );
   RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "Packing - Yahoo_Yours",
      x => x.FetchDailyOrderFromYahoo(YahooShop.Yahoo_Yours, OrderStatus.Packing, null, null, true, UpdateMode.Replace),
      Cron.Never // スケジュール実行なし（手動専用）
   );
   RecurringJob.AddOrUpdate<SetDailyOrderNews>(
      "Packing - Rakuten_ENZO",
      x => x.FetchDailyOrderFromRakuten(RakutenShop.Rakuten_ENZO, OrderStatus.Packing, null, null, true, UpdateMode.Replace),
      Cron.Never // スケジュール実行なし（手動専用）
   );

   Log("Jobs configured successfully.");
}
catch (Exception ex)
{
   Log($"Error configuring jobs: {ex.Message}");
   throw;
}

// Root path message
app.MapGet("/", async context =>
{
   context.Response.ContentType = "text/html";
   await context.Response.WriteAsync("<h1>Hangfire is running!</h1><p>Visit <a href='/job/hangfire'>Hangfire Dashboard</a></p>");
});

app.Run();

// Filter for logging job performance
public class LogJobFilter(Action<string> log) : JobFilterAttribute, IServerFilter
{
   private readonly Action<string> _log = log;

   public void OnPerforming(PerformingContext context)
   {
      var jobId = context.BackgroundJob.Id;
      var jobName = context.BackgroundJob.Job.Method.Name;
      _log($"Starting job ID: {jobId}, Name: {jobName}");
   }

   public void OnPerformed(PerformedContext context)
   {
      var jobId = context.BackgroundJob.Id;
      var jobName = context.BackgroundJob.Job.Method.Name;

      if (context.Exception == null)
      {
         _log($"Completed job ID: {jobId}, Name: {jobName}, Status: Succeeded");
      }
      else
      {
         _log($"Completed job ID: {jobId}, Name: {jobName}, Status: Failed, Exception: {context.Exception.Message}");
      }
   }
}

public class AllowAllAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
   public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
