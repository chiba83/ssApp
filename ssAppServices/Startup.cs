using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ssAppModels.EFModels;
using ssAppCommon.Logging;
using ssAppServices;
using ssAppServices.Api;

namespace ssAppServices
{
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

            // ApiClient の登録
            services.AddHttpClient<ApiClientHandler>();

            // ErrorLogger の登録
            services.AddSingleton<ErrorLogger>();

            // ServiceErrHandler の登録
            services.AddSingleton<ServiceErrHandler>();

            // MallSettings の登録
            services.Configure<MallSettings>(_configuration.GetSection("MallSettings"));

            // YahooAuthenticationService の登録
            services.AddScoped<YahooAuthenticationService>();
        }
    }
}