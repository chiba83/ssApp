using MudBlazor.Services;
using ssAppBlazorWeb.Components;
using Microsoft.EntityFrameworkCore;
using ssAppModels;
using Microsoft.Extensions.Configuration;
using ssAppModels.EFModels;
using ssAppBlazorWeb.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Syncfusionサービス登録（ライセンス登録）
//Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzgwMjQzNkAzMjM5MmUzMDJlMzAzYjMyMzkzYmJyL3RSVTZveElGcEhVUkladCtHOURDV2VtOU5jNFo1SkNXVHlZWnc4Z2s9");
//builder.Services.AddSyncfusionBlazor();

// JSON構成ファイル（ssAppBlazorWeb.json）を追加読み込み
builder.Configuration.AddJsonFile("ssAppBlazorWeb.json", optional: false, reloadOnChange: true);

// DbContext DI登録（"= Production"本番環境、"<> Production"テスト環境）
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var isProduction = string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
var connectionName = isProduction ? "ssAppDBContext" : "ssAppDBContextTest";
var connectionString = builder.Configuration.GetConnectionString(connectionName);
builder.Services.AddDbContext<ssAppDBContext>(x => x.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddAutoMapper(typeof(MappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
