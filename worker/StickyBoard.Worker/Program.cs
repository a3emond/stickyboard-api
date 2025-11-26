using System.Data;
using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StickyBoard.Core.Infrastructure.Db;
using StickyBoard.Core.Repositories.Attachments;
using StickyBoard.Core.Repositories.Attachments.Contracts;
using StickyBoard.Core.Repositories.Automation.Jobs;
using StickyBoard.Core.Services.Attachments;
using StickyBoard.Core.Services.Automation.Workers;
using StickyBoard.Worker;
using StickyBoard.Worker.Workers;

// ==========================================================
//  StickyBoard Worker Entry Point 
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
        services.AddScoped<IWorkerJobRepository, WorkerJobRepository>();
        services.AddScoped<IWorkerJobAttemptRepository, WorkerJobAttemptRepository>();
        services.AddScoped<WorkerQueueService>();

        services.AddScoped<IAttachmentManager, AttachmentManager>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IAttachmentVariantRepository, AttachmentVariantRepository>();
        services.AddScoped<IFileTokenRepository, FileTokenRepository>();

        services.AddScoped<IWorkerHandler, AssetVariantWorker>();
        services.AddScoped<WorkerDispatcher>();

        services.AddHttpClient();

        services.AddHostedService<WorkerLoop>();


    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .RunConsoleAsync();
