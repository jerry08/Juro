using Juro.Clients;
using Juro.Core.Models;
using Juro.Utils;
using Juro.WebApi.Controllers.Anime;
using Juro.WebApi.Controllers.Manga;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Juro.WebApi;

public class Program
{
    public static List<Provider> Providers { get; set; } = [];

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddMvc(c => c.Conventions.Add(new ApiExplorerConvention()));

        //builder.Services.AddSingleton<Gogoanime>();

        //var gg = typeof(Gogoanime);
        AssemblyEx.LoadReferencedAssemblies();

        //var tt1 = AppDomain.CurrentDomain.GetAssemblies().ToList();
        //var tt2 = tt1.Where(x =>
        //        x.GetCustomAttributes(typeof(PluginAssemblyAttribute), false).Length > 0
        //    )
        //    .ToList();

        var animeClient = new AnimeClient();

        Providers.AddRange(
            animeClient
                .GetProviders()
                .Select(provider => new Provider()
                {
                    Key = provider.Key,
                    Name = provider.Name,
                    Language = provider.Language,
                    Type = ProviderType.Anime,
                })
        );

        var animeProviderTypes = animeClient.GetProviderTypes();

        foreach (var type in animeProviderTypes)
        {
            builder.Services.Add(
                //new ServiceDescriptor(typeof(IAnimeProvider), type, ServiceLifetime.Singleton)
                new ServiceDescriptor(type, type, ServiceLifetime.Singleton)
            );
        }

        var mangaClient = new MangaClient();

        Providers.AddRange(
            mangaClient
                .GetProviders()
                .Select(provider => new Provider()
                {
                    Key = provider.Key,
                    Name = provider.Name,
                    Language = provider.Language,
                    Type = ProviderType.Manga,
                })
        );

        var mangaProviderTypes = mangaClient.GetProviderTypes();

        foreach (var type in mangaProviderTypes)
        {
            builder.Services.Add(new ServiceDescriptor(type, type, ServiceLifetime.Singleton));
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

#if DEBUG
        app.MapGet(
            "/",
            () =>
                Results.Content(
                    """
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Juro API</title>
                    </head>
                    <body>
                        <h1>Welcome to Juro API</h1>
                        <p><a href="/swagger">View API Documentation</a></p>
                    </body>
                    </html>
                    """,
                    "text/html"
                )
        );
#else
        app.MapGet(
            "/",
            () =>
                Results.Content(
                    """
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Juro API</title>
                    </head>
                    <body>
                        <h1>Welcome to Juro API</h1>
                    </body>
                    </html>
                    """,
                    "text/html"
                )
        );
#endif

        app.Run();
    }
}

public class ApiExplorerConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        //action.ApiExplorer.IsVisible =
        //    action.Controller.ControllerType.BaseType == typeof(ControllerBase);

        action.ApiExplorer.IsVisible =
            action.Controller.ControllerType != typeof(AnimeBaseController)
            && action.Controller.ControllerType != typeof(MangaBaseController);
    }
}
