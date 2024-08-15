using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

Assembly assembly = typeof(Program).Assembly;
string redisConnectionString = builder.Configuration.GetConnectionString("Redis")!;
string dbConnectionString = builder.Configuration.GetConnectionString("Database")!;

// Add services to the container.
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

DependencyContextAssemblyCatalog dependencyContextAssemblyCatalog = new([assembly]);
builder.Services.AddCarter(dependencyContextAssemblyCatalog);

builder.Services.AddMarten(options =>
{
    options.Connection(dbConnectionString);
    options.Schema.For<ShoppingCart>().Identity(x => x.UserName);
}).UseLightweightSessions();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddScoped<IBasketRepository, BasketRepository>();
//builder.Services.AddScoped<IBasketRepository, CachedBasketRepository>();
/*
builder.Services.AddScoped<IBasketRepository>(provider =>
{
    var basketRepository = provider.GetRequiredService<IBasketRepository>();
    return new CachedBasketRepository(basketRepository, provider.GetRequiredService<IDistributedCache>());
});
*/
builder.Services.Decorate<IBasketRepository, CachedBasketRepository>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});

builder.Services.AddHealthChecks()
    .AddNpgSql(dbConnectionString)
    .AddRedis(redisConnectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket.API");
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
