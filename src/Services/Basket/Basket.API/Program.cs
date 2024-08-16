using Discount.Grpc;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

Assembly assembly = typeof(Program).Assembly;
string redisConnectionString = builder.Configuration.GetConnectionString("Redis")!;
string dbConnectionString = builder.Configuration.GetConnectionString("Database")!;

//Application Services
DependencyContextAssemblyCatalog dependencyContextAssemblyCatalog = new([assembly]);
builder.Services.AddCarter(dependencyContextAssemblyCatalog);
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});


//Data Services
builder.Services.AddMarten(options =>
{
    options.Connection(dbConnectionString);
    options.Schema.For<ShoppingCart>().Identity(x => x.UserName);
}).UseLightweightSessions();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});

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

// Swagger (Open API) services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Grpc Services
builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["GrpcSettings:DiscountUrl"]!);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler
    {
        // Don't use in production
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    return handler;
});


//Cross-cutting Services
builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
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
