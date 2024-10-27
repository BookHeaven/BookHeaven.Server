using System.Globalization;
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

namespace BookHeaven.Server
{
	public class Program
	{
		private static readonly string AppDataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
		public static readonly string ImportPath = Path.Combine(Directory.GetCurrentDirectory(), "import");
		public static readonly string BooksPath = Path.Combine(AppDataPath, "books");
		public static readonly string CoversPath = Path.Combine(AppDataPath, "covers");
		private static readonly string DatabasePath = Path.Combine(AppDataPath, "database");
		public static Profile? SelectedProfile { get; set; }
		
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			
			builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");
			
			Directory.CreateDirectory(BooksPath);
			Directory.CreateDirectory(CoversPath);
			Directory.CreateDirectory(DatabasePath);

			// Add services to the container.
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
			builder.Services.AddTransient<IMetadataProviderService, OpenlibraryService>();
			builder.Services.AddTransient<IFormatService<EpubBook>, EpubService>();	

			var app = builder.Build();
			app.UseRequestLocalization(Environment.GetEnvironmentVariable("LANG") ?? CultureInfo.CurrentCulture.Name);

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
				app.UseHttpsRedirection();
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
				var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

				if(!context.Profiles.Any())
				{
					context.Profiles.Add(new Profile
					{
						Name = "Default",
						IsSelected = true,
					});
					context.SaveChanges();
				}
				
				SelectedProfile = context.Profiles.First(x => x.IsSelected);
			}
			app.MapControllers();

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

			app.Run();
		}
	}
}
