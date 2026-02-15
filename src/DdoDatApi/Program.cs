using DdoDatApi.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using System.IO;

namespace DdoDatApi;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers()
            .AddNewtonsoftJson();

        builder.Services.RegisterSingletonService<ICacheBuilderService, CacheBuilderService>();

        builder.Services.AddSwaggerGen(c =>
        {
            var filePath = Path.Combine(System.AppContext.BaseDirectory, "DdoDatApi.xml");
            c.IncludeXmlComments(filePath);

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "DDO Dat Reader API",
                Description = "A web service designed to be used by an MCP server for reading objects from the dat files of Dungeons and Dragons Online",
            });
        });

        var app = builder.Build();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.MapControllers();

        DatSource.Load();
        IndexLoader.LoadIndexCache();

        app.Run();
    }

    public static IServiceCollection RegisterSingletonService<T1, T2>(this IServiceCollection serviceCollection)
        where T1 : class
        where T2 : class, T1, IHostedService
    {
        serviceCollection.AddSingleton<T1, T2>();
        serviceCollection.AddHostedService(p => (T2)p.GetRequiredService<T1>());
        return serviceCollection;
    }
}