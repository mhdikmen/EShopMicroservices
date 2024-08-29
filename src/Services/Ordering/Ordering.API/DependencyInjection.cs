using System.Reflection;

namespace Ordering.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        Assembly assembly = typeof(Program).Assembly;

        DependencyContextAssemblyCatalog dependencyContextAssemblyCatalog = new([assembly]);
        services.AddCarter(dependencyContextAssemblyCatalog);

        //services.AddHealthChecks()
        //    .AddNpgSql(connectionString);

        // Swagger (Open API) services
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }

    public static WebApplication UseApiServices(this WebApplication app)
    {
        app.MapCarter();
        app.UseExceptionHandler(options => { });

        //app.UseHealthChecks("/health",
        //    new HealthCheckOptions
        //    {
        //        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        //    });

        // Swagger (Open API) services
        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordering.API");
            c.RoutePrefix = string.Empty; // Set Swagger UI at the root
        });

        return app;
    }
}
