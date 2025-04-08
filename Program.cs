using EpubManager;
using EpubManager.Entities;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Services;
using BookHeaven.Server.Components;
using BookHeaven.Server.Interfaces;
using BookHeaven.Server.Services;
using System.Text.Json.Serialization;
using MudBlazor;
using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Features.Profiles;
using BookHeaven.Server.Abstractions;
using BookHeaven.Server.Endpoints;
using BookHeaven.Server.Extensions;
using MediatR;

namespace BookHeaven.Server;

public class Program
{
	private static readonly string AppDataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
	public static readonly string ImportPath = Path.Combine(Directory.GetCurrentDirectory(), "import");
	public static readonly string BooksPath = Path.Combine(AppDataPath, "books");
	public static readonly string CoversPath = Path.Combine(AppDataPath, "covers");
	public static readonly string DatabasePath = Path.Combine(AppDataPath, "database");
	public static readonly string FontsPath = Path.Combine(AppDataPath, "fonts");
	public static Profile? SelectedProfile { get; set; }
		
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

		builder.Services.AddDomain(DatabasePath);
		builder.Services.AddEpubManager();
			
		builder.Services.AddScoped<ISettingsManagerService, SettingsManagerService>();
		builder.Services.AddScoped<IMetadataProviderService, GoogleBooksService>();
		// builder.Services.AddScoped<IMetadataProviderService, OpenLibraryService>();
		builder.Services.AddScoped<IFormatService<EpubBook>, EpubService>();

		builder.Services.AddSingleton<UdpBroadcastServer>();
			
		// Add endpoints
		builder.Services.AddEndpoints(typeof(Program).Assembly);

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
			app.UseExceptionHandler("/Error");
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
			FileProvider = new PhysicalFileProvider(AppDataPath),
			ContentTypeProvider = provider
		});
			
		app.UseAntiforgery();

		app.MapRazorComponents<App>()
			.AddInteractiveServerRenderMode();

		using (var scope = app.Services.CreateScope())
		{
			var udpBroadcastServer = scope.ServiceProvider.GetRequiredService<UdpBroadcastServer>();
			_ = udpBroadcastServer.StartAsync();
		}

		using (var scope = app.Services.CreateScope())
		{
			// Create default profile if it doesn't exist
			Profile defaultProfile;
			var sender = scope.ServiceProvider.GetRequiredService<ISender>();

			var profileQuery = await sender.Send(new GetDefaultProfileQuery());
			if(profileQuery.IsFailure)
			{
				var createProfile = await sender.Send(new CreateProfileCommand("Default"));
				if (createProfile.IsFailure)
				{
					throw new Exception(createProfile.Error.Description);
				}
				defaultProfile = createProfile.Value;
			}
			else
			{
				defaultProfile = profileQuery.Value;
			}
				
			// To be changed when the user is able to pick a profile
			SelectedProfile = defaultProfile;
		}

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
			
		app.MapCultureEndpoint();
			
		var mapGroup = app.MapGroup("/api");
		mapGroup.MapGet("ping", () => Results.Ok());
		app.MapEndpoints(mapGroup);

		await app.RunAsync();
	}
}