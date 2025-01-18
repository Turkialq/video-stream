using Microsoft.Extensions.Options;
using Models.MongoSettings;
using Models.RabbitSettings;
using MongoDB.Driver;
using Services.RabbitManager;
using VideoUploadService.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables(); // If you want overrides from env vars

// --------------------------------------------------
// 2) Bind our Settings classes to Configuration
// --------------------------------------------------
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
builder.Services.Configure<RabbitSettings>(builder.Configuration.GetSection("RabbitSettings"));

// --------------------------------------------------
// 3) Setup MongoDB
// --------------------------------------------------
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var mongoSettings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return new MongoClient(mongoSettings.ConnectionString);
});

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var mongoSettings = sp.GetRequiredService<IOptions<MongoSettings>>().Value;
    return client.GetDatabase(mongoSettings.DatabaseName);
});

// --------------------------------------------------
// 4) Setup RabbitMQ via RabbitManager
// --------------------------------------------------
builder.Services.AddSingleton<IRabbitManager, RabbitManager>();

// --------------------------------------------------
// 5) Register our custom services
// --------------------------------------------------
builder.Services.AddScoped<IFileUploadService, FileUploadService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapControllers();

app.Run();


