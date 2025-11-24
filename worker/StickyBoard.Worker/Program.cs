using System.Data;
using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyBoard.Core.Infrastructure.Db;

// ==========================================================
//  StickyBoard Worker Entry Point (Refactored)
// ==========================================================
await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var envPath  = Path.Combine(basePath, ".env");

        config.SetBasePath(basePath);
        config.AddEnvironmentVariables();

        if (File.Exists(envPath))
        {
            config.AddDotNetEnv(envPath);
            Console.WriteLine($"Loaded .env from {envPath}");
        }
        else
        {
            Console.WriteLine($"WARNING: .env not found at {envPath}");
        }
    })
    .ConfigureServices((context, services) =>
    {
        var cfg = context.Configuration;

        // ==========================================================
        // DATABASE (centralized Core logic)
        // ==========================================================
        var connectionString = ConnectionStringBuilder.Build(cfg);
        Console.WriteLine($"[Worker] Using connection: {connectionString}");

        var dataSource = DataSourceFactory.Create(connectionString);

        services.AddSingleton(dataSource);
        services.AddScoped<IDbConnection>(_ => dataSource.CreateConnection());

        // ==========================================================
        // REPOSITORIES & WORKERS
        // ==========================================================
        // services.AddScoped<IXXXRepository, XXXRepository>();
        // services.AddHostedService<WorkerLoop>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .RunConsoleAsync();
