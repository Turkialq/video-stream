using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Services.RabbitManager;
using FileUploadService.Models;
using FileUploadService.Config;
using FileUploadService.Configuration;
using Hangfire;
using Hangfire.Mongo;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables(); // If you want overrides from env vars


builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
builder.Services.Configure<RabbitSettings>(builder.Configuration.GetSection("RabbitSettings"));
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("StorageSettings"));
builder.Services.Configure<HangfireSettings>(builder.Configuration.GetSection("HangfireSettings"));


builder.Services.AddHangfire((serviceProvider, config) =>
{
    var hangfireSettings = serviceProvider.GetRequiredService<
        IOptions<HangfireSettings>>().Value;

    var mongoSettings = serviceProvider.GetRequiredService<
        IOptions<MongoSettings>>().Value;

    config.UseMongoStorage(
        mongoSettings.ConnectionString, 
        hangfireSettings.DatabaseName   
    );
});



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
builder.Services.AddSingleton<IDatabaseApplicationContext, DatabaseApplicationContext>();

builder.Services.AddMongoCollectionFromContext<FileCollection>(ctx => ctx.Files);
builder.Services.AddMongoCollectionFromContext<OutboxMessage>(ctx => ctx.OutboxMessages);


// --------------------------------------------------
// 5) Register our custom services
// --------------------------------------------------
builder.Services.AddScoped<FileUploadService.Services.IFileUploadService, FileUploadService.Services.FileUploadService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapControllers();

app.Run();


