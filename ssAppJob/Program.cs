using Hangfire;
using Hangfire.SqlServer;
using ssAppServices.Apps;
using ssAppServices.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from ssAppService
try
{
   var basePath = AppContext.BaseDirectory;
   var logPath = "C:\\temp\\debug_log.txt";

   // Log base directory for debugging
   File.AppendAllText(logPath, $"Base Directory: {basePath}\n");

   builder.Configuration
       .SetBasePath(basePath)
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

   File.AppendAllText(logPath, "Configuration loaded successfully.\n");
}
catch (Exception ex)
{
   File.AppendAllText("C:\\temp\\debug_log.txt", $"Error loading configuration: {ex.Message}\n");
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

   File.AppendAllText("C:\\temp\\debug_log.txt", "Services configured successfully.\n");
}
catch (Exception ex)
{
   File.AppendAllText("C:\\temp\\debug_log.txt", $"Error configuring services: {ex.Message}\n");
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
   Authorization = new[] { new AllowAllAuthorizationFilter() }
});

try
{
   // Add the recurring job
   RecurringJob.AddOrUpdate<SetDailyOrderNews>(
       "daily-order-news-workflow",
       x => x.RunDailyOrderNewsWorkflow(),
       "0,30 * * * *" // –ˆŽž 0•ª‚Æ30•ª
   );

   BackgroundJob.Enqueue<SetDailyOrderNews>(x => x.RunDailyOrderNewsWorkflow()); // Immediate execution

   File.AppendAllText("C:\\temp\\debug_log.txt", "Jobs configured successfully.\n");
}
catch (Exception ex)
{
   File.AppendAllText("C:\\temp\\debug_log.txt", $"Error configuring jobs: {ex.Message}\n");
   throw;
}

// Root path message
app.MapGet("/", async context =>
{
   context.Response.ContentType = "text/html";
   await context.Response.WriteAsync("<h1>Hangfire is running!</h1><p>Visit <a href='/job/hangfire'>Hangfire Dashboard</a></p>");
});

app.Run();

public class AllowAllAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
   public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
