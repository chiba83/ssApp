using MudBlazor.Services;
using ssAppBlazorWeb.Components;
using Microsoft.EntityFrameworkCore;
using ssAppModels;
using Microsoft.Extensions.Configuration;
using ssAppModels.EFModels;
using ssAppBlazorWeb.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Syncfusion�T�[�r�X�o�^�i���C�Z���X�o�^�j
//Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzgwMjQzNkAzMjM5MmUzMDJlMzAzYjMyMzkzYmJyL3RSVTZveElGcEhVUkladCtHOURDV2VtOU5jNFo1SkNXVHlZWnc4Z2s9");
//builder.Services.AddSyncfusionBlazor();

// JSON�\���t�@�C���issAppBlazorWeb.json�j��ǉ��ǂݍ���
builder.Configuration.AddJsonFile("ssAppBlazorWeb.json", optional: false, reloadOnChange: true);

// DbContext DI�o�^�i"= Production"�{�Ԋ��A"<> Production"�e�X�g���j
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
