using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using DotNetEnv.Configuration;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Services;
using System.Data;

// ==========================================================
//  StickyBoard Worker Entry Point
// ==========================================================
await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Base project root (4 levels up from bin/)
        var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var envPath = Path.Combine(basePath, ".env");

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
        // 1. DATABASE CONNECTION (NpgsqlDataSource with Enum Mapping)
        // ==========================================================
        var dbHost = cfg["DB_HOST"] ?? "localhost";
        var dbUser = cfg["POSTGRES_USER"];
        var dbPass = cfg["POSTGRES_PASSWORD"];
        var dbName = cfg["POSTGRES_DB"];

        string connectionString;
        var dbUrl = cfg["DATABASE_URL"];

        if (!string.IsNullOrEmpty(dbUrl) && dbUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            // Convert postgres://user:pass@host:port/dbname → Npgsql format
            var uri = new Uri(dbUrl);
            var userInfo = uri.UserInfo.Split(':');
            var user = userInfo.Length > 0 ? userInfo[0] : string.Empty;
            var pass = userInfo.Length > 1 ? userInfo[1] : string.Empty;
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={pass};SSL Mode=Prefer;Trust Server Certificate=true";
        }
        else
        {
            connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPass};SSL Mode=Prefer;Trust Server Certificate=true";
        }

        Console.WriteLine($"[Worker] Using connection: {connectionString}");

        // Create DataSourceBuilder and register all PostgreSQL ENUM mappings
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        // --- User & Authorization
        dataSourceBuilder.MapEnum<UserRole>("user_role");
        dataSourceBuilder.MapEnum<BoardRole>("board_role");
        dataSourceBuilder.MapEnum<OrgRole>("org_role");

        // --- Visibility & Structure
        dataSourceBuilder.MapEnum<BoardVisibility>("board_visibility");
        dataSourceBuilder.MapEnum<TabScope>("tab_scope");

        // --- Cards, Links & Clusters
        dataSourceBuilder.MapEnum<CardType>("card_type");
        dataSourceBuilder.MapEnum<CardStatus>("card_status");
        dataSourceBuilder.MapEnum<LinkType>("link_type");
        dataSourceBuilder.MapEnum<ClusterType>("cluster_type");

        // --- Activity & Entity Types
        dataSourceBuilder.MapEnum<ActivityType>("activity_type");
        dataSourceBuilder.MapEnum<EntityType>("entity_type");

        // --- Worker / Job Queue
        dataSourceBuilder.MapEnum<JobKind>("job_kind");
        dataSourceBuilder.MapEnum<JobStatus>("job_status");

        // --- Messaging & Social
        dataSourceBuilder.MapEnum<MessageType>("message_type");
        dataSourceBuilder.MapEnum<RelationStatus>("relation_status");

        // Build DataSource
        var dataSource = dataSourceBuilder.Build();

        // Register in DI
        services.AddSingleton(dataSource);
        services.AddScoped<IDbConnection>(_ => dataSource.CreateConnection());

        // ==========================================================
        // 2. REPOSITORIES & SERVICES
        // ==========================================================
        services.AddScoped<WorkerJobRepository>();
        services.AddScoped<WorkerJobAttemptRepository>();
        services.AddScoped<WorkerJobDeadletterRepository>();
        services.AddScoped<WorkerJobService>();

        // Reuse any core services your worker may call
        services.AddScoped<BoardRepository>();
        services.AddScoped<BoardService>();
        services.AddScoped<CardRepository>();
        services.AddScoped<CardService>();
        services.AddScoped<MessageRepository>();
        services.AddScoped<MessageService>();
        services.AddScoped<UserRepository>();
        services.AddScoped<UserService>();
        services.AddScoped<SyncService>();
        // ...add more from API as needed for job execution

        // ==========================================================
        // 3. BACKGROUND WORKER LOOP
        // ==========================================================
        services.AddHostedService<WorkerLoop>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .RunConsoleAsync();
