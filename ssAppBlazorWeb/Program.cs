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

// DbContext を DI 登録（SQL Server 用）
var connectionString = builder.Configuration.GetConnectionString("ssAppDBContext");
builder.Services.AddDbContext<ssAppDBContext>(options => options.UseSqlServer(connectionString));

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
