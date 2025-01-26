using Hangfire;
using Hangfire.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false);
        config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        var mongoConnectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://root:example@localhost:27017";
        var databaseName = configuration.GetValue<string>("HangfireDatabaseName") ?? "HangfireDb";

        // Configure Hangfire with MongoDB
        services.AddHangfire(config =>
        {
            var mongoUrlBuilder = new MongoUrlBuilder(mongoConnectionString);
            var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());

            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMongoStorage(mongoClient, databaseName, new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionBackupStrategy()
                    },
                    Prefix = "hangfire.mongo",
                    CheckConnection = true
                });
        });

        // Add Hangfire Server
        services.AddHangfireServer(options =>
        {
            options.ServerName = $"Hangfire.Server.{Environment.MachineName}.{Guid.NewGuid()}";
            options.WorkerCount = Environment.ProcessorCount * 5; // Adjust based on your needs
        });
    });

var host = builder.Build();

// Add some logging
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Hangfire Server at: {time}", DateTimeOffset.Now);

// Run the application
await host.RunAsync();
