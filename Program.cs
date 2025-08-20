using BookHeaven.EpubManager;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using BookHeaven.Server.Components;
using BookHeaven.Server.Interfaces;
using BookHeaven.Server.Services;
using System.Text.Json.Serialization;
using MudBlazor;
using BookHeaven.Domain;
using BookHeaven.Domain.Abstractions;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Features.Profiles;
using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.Server.Abstractions;
using BookHeaven.Server.Endpoints;
using BookHeaven.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Scalar.AspNetCore;

namespace BookHeaven.Server;

public class Program
{
	private static readonly string AppDataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
	public static readonly string ImportPath = Path.Combine(Directory.GetCurrentDirectory(), "import");
	public static readonly string BooksPath = Path.Combine(AppDataPath, "books");
	public static readonly string CoversPath = Path.Combine(AppDataPath, "covers");
	public static readonly string DatabasePath = Path.Combine(AppDataPath, "database");
	public static readonly string FontsPath = Path.Combine(AppDataPath, "fonts");
	
	public static readonly MudTheme Theme = new()
	{
		PaletteDark = new()
		{
			AppbarBackground = "#1d202bcc",
			AppbarText = "#a8a8a8",
			Background = "#1d202b",
			DrawerBackground = "#1d202b",
			DrawerText = "#a8a8a8",
			DrawerIcon = "#a8a8a8",
			Surface = "#2c3041",
			TextPrimary = "#ffffff",
			TextSecondary = "#a8a8a8",
			Primary = "#56b4ff",
			Secondary = "#a8a8a8",
			Tertiary = "#3097f3",
			TextDisabled = "#595959",
			LinesDefault = "#515151c2",
			LinesInputs = "#56b4ff",
			ActionDefault = "#56b4ff",
			ActionDisabled = "#595959",
			HoverOpacity = 0.1,
			PrimaryContrastText = "#000000",
			TertiaryContrastText = "#FFFFFF",
			SecondaryContrastText = "#000000",
			WarningContrastText = "#000000",
			TableLines = "#4a5d6d"
		}
	};
		
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
			
		builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");
			
		Directory.CreateDirectory(BooksPath);
		Directory.CreateDirectory(CoversPath);
		Directory.CreateDirectory(DatabasePath);
		Directory.CreateDirectory(FontsPath);
		
		builder.Services.AddRazorComponents()
			.AddInteractiveServerComponents();
		
		builder.Services.AddDataProtection()
			.SetApplicationName("BookHeaven")
			.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppDataPath, "keys")))
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

		builder.Services.AddDomain(BooksPath, CoversPath, FontsPath, DatabasePath);
		builder.Services.AddEpubManager();

		builder.Services.AddTransient<IAlertService, AlertService>();
		builder.Services.AddScoped<ISettingsManagerService, SettingsManagerService>();
		builder.Services.AddScoped<IMetadataProviderService, GoogleBooksService>();
		// builder.Services.AddScoped<IMetadataProviderService, OpenLibraryService>();
		builder.Services.AddScoped<IFormatService<EpubBook>, EpubService>();
		builder.Services.AddScoped<ISessionService, SessionService>();

		builder.Services.AddHostedService<UdpBroadcastServer>();
			
		// Add endpoints
		builder.Services.AddEndpoints(typeof(Program).Assembly);

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
			FileProvider = new PhysicalFileProvider(BooksPath),
			ContentTypeProvider = provider,
			RequestPath = "/books"
		});
		app.UseStaticFiles(new StaticFileOptions
		{
			FileProvider = new PhysicalFileProvider(CoversPath),
			RequestPath = "/covers"
		});
		app.UseStaticFiles(new StaticFileOptions
		{
			FileProvider = new PhysicalFileProvider(FontsPath),
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

		if (app.Environment.IsDevelopment())
		{
			app.MapOpenApi();
			app.MapScalarApiReference();
		}

		await app.RunAsync();
	}
}