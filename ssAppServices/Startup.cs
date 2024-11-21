using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ssAppServices.Extensions;

namespace ssAppServices
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup()
        {
            // appsettings.json を読み込む
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // DI 設定を一元化（テストのDI登録簡略用）
            services.AddProjectDependencies(Configuration);
        }
    }
}
