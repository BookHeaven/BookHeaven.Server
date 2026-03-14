using BookHeaven.EbookManager;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using BookHeaven.Server.Components;
using BookHeaven.Server.Services;
using System.Text.Json.Serialization;
using MudBlazor;
using BookHeaven.Domain;
using BookHeaven.Domain.Abstractions;
using BookHeaven.Server.Features.Api.DependencyInjection;
using BookHeaven.Server.Features.Api.Endpoints;
using BookHeaven.Server.Features.Discovery.Services;
using BookHeaven.Server.Features.Files.Abstractions;
using BookHeaven.Server.Features.Files.Services;
using BookHeaven.Server.Features.Metadata.Abstractions;
using BookHeaven.Server.Features.Metadata.DependencyInjection;
using BookHeaven.Server.Features.Session.Abstractions;
using BookHeaven.Server.Features.Session.Services;
using BookHeaven.Server.Features.Settings.Abstractions;
using BookHeaven.Server.Features.Settings.Services;
using Microsoft.AspNetCore.DataProtection;
using DuckDuckGoCoverProvider = BookHeaven.Server.Features.Metadata.Services.DuckDuckGoCoverProvider;

var appDataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");

Environment.SetEnvironmentVariable("TOOLBELT_BLAZOR_VIEWTRANSITION_JSCACHEBUSTING", "0");
		
var builder = WebApplication.CreateBuilder(args);
	
builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddDataProtection()
	.SetApplicationName("BookHeaven")
	.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(appDataPath, "keys")))
	.SetDefaultKeyLifetime(TimeSpan.FromDays(14));

builder.Services.AddControllers().AddJsonOptions(x =>
{
	x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddMudServices(config =>
{
	config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
	config.SnackbarConfiguration.HideTransitionDuration = 500;
	config.SnackbarConfiguration.ShowTransitionDuration = 500;
	config.SnackbarConfiguration.PreventDuplicates = false;
});

builder.Services.AddDomain(options =>
{
	options.BooksPath = Path.Combine(appDataPath, "books");
	options.CoversPath = Path.Combine(appDataPath, "covers");
	options.FontsPath = Path.Combine(appDataPath, "fonts");
	options.DatabasePath = Path.Combine(appDataPath, "database");
});
builder.Services.AddEbookManager();

builder.Services.AddTransient<ICoverProvider, DuckDuckGoCoverProvider>();
builder.Services.AddTransient<IAlertService, AlertService>();
builder.Services.AddTransient<IEbookFileLoader, EbookFileLoader>();
builder.Services.AddScoped<ISettingsManagerService, SettingsManagerService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddSingleton<IEbookLoadNotifier, EbookLoadNotifier>();

builder.Services.AddMetadataProviders();

// Background services
builder.Services.AddHostedService<UdpBroadcastServer>();
builder.Services.AddHostedService<ImportFolderWatcher>();
	
// Add endpoints
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddOpds();

builder.Services.AddOpenApi();

var app = builder.Build();
	
var supportedCultures = new[]{ "en-US", "es-ES" };
var localizationOptions = new RequestLocalizationOptions()
	.SetDefaultCulture(supportedCultures[0])
	.AddSupportedCultures(supportedCultures)
	.AddSupportedUICultures(supportedCultures);
	
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	/*app.UseHsts();
	app.UseHttpsRedirection();*/
}

FileExtensionContentTypeProvider provider = new()
{
	Mappings =
	{
		[".epub"] = "application/epub+zip"
	}
};

app.MapStaticAssets();
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(DomainGlobals.BooksPath),
	ContentTypeProvider = provider,
	RequestPath = "/books",
	OnPrepareResponse = ctx =>
	{
		ctx.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
		ctx.Context.Response.Headers.Pragma = "no-cache";
		ctx.Context.Response.Headers.Expires = "0";
	}
});
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(DomainGlobals.CoversPath),
	RequestPath = "/covers",
	OnPrepareResponse = ctx =>
	{
		ctx.Context.Response.Headers.CacheControl = "public,immutable,max-age=" + (int)TimeSpan.FromDays(30).TotalSeconds;
	}
});
app.UseStaticFiles(new StaticFileOptions
{
	FileProvider = new PhysicalFileProvider(DomainGlobals.FontsPath),
	RequestPath = "/fonts"
});
	
app.UseAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Use(async (context, next) =>
{
	if (context.Request.Path == "/")
	{
		context.Response.Redirect("/shelf");
	}
	else
	{
		await next();
	}
});

app.MapEndpoints();
app.MapOpds();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

await app.RunAsync();