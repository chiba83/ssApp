using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ssAppModels.EFModels;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // DbContext の登録
        services.AddDbContext<ssAppDBContext>(options =>
            options.UseSqlServer(_configuration.GetConnectionString("ssAppDBContext")));

        // ErrorLogger の登録
        services.AddSingleton<ErrorLogger>();

        // ApiClient の登録
        services.AddHttpClient<ApiClientHandler>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(_configuration["ApiSettings:BaseUrl"]);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        // YahooAuthenticationService の登録
        services.AddScoped<YahooAuthenticationService>(provider =>
        {
            var dbContext = provider.GetRequiredService<ssAppDBContext>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            var apiClienthHandler = provider.GetRequiredService<ApiClientHandler>();
            var errorLogger = provider.GetRequiredService<ErrorLogger>();
            return new YahooAuthenticationService(dbContext, configuration, apiClienthHandler, errorLogger);
        });
    }

    public void Configure(IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        });
    }
}
