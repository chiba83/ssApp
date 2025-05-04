using Hangfire;
using Hangfire.SqlServer;
using ssAppServices.Extensions;
using ssAppJob.Logging;
using ssAppJob;

var builder = WebApplication.CreateBuilder(args);

// ログ出力の共通メソッドは LogHelper に移行済み

// Load configuration from ssAppService
try
{
  var basePath = AppContext.BaseDirectory;

  builder.Configuration
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.job.json", optional: false, reloadOnChange: true);

  LogHelper.Log("Configuration loaded successfully.");
}
catch (Exception ex)
{
  LogHelper.Log($"Error loading configuration: {ex.Message}");
  throw;
}

// Configure services
try
{
  builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireDBContext"), new SqlServerStorageOptions
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

  LogHelper.Log("Services configured successfully.");
}
catch (Exception ex)
{
  LogHelper.Log($"Error configuring services: {ex.Message}");
  throw;
}

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();

// Hangfire Dashboard configuration - official path base style
app.UsePathBase("/job");
app.UseHangfireDashboard("", new DashboardOptions
{
  Authorization = [new AllowAllAuthorizationFilter()]
});

// Add event for job performance logging
GlobalJobFilters.Filters.Add(new LogJobFilter());

try
{
  // Add the recurring jobs
  JobRegistrar.RegisterAllJobs();
  LogHelper.Log("Jobs configured successfully.");
}
catch (Exception ex)
{
  LogHelper.Log($"Error configuring jobs: {ex.Message}");
  throw;
}

app.Run();

// Filter for logging job performance
public class AllowAllAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
  public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
