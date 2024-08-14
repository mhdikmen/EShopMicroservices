using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var assembly = typeof(Program).Assembly;

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

DependencyContextAssemblyCatalog dependencyContextAssemblyCatalog = new([assembly]);
builder.Services.AddCarter(dependencyContextAssemblyCatalog);
string connectionString = builder.Configuration.GetConnectionString("Database")!;

builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
}).UseLightweightSessions();

if (builder.Environment.IsDevelopment())
    builder.Services.InitializeMartenWith<CatalogInitialData>();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.API");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the root
});

app.MapCarter();

app.UseExceptionHandler(options => { });

app.UseHealthChecks("/health",
    new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });


app.Run();
