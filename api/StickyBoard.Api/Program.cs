using System.Data;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using StickyBoard.Api.Auth;
using StickyBoard.Api.Middleware;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Services;
using StickyBoard.Api.Models.Enums;
using StickyBoard.Api.Repositories.FilesAndOps;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ==========================================================
// 1. DATABASE CONNECTION (NpgsqlDataSource with Enum Mapping)
// ==========================================================
var dbHost = configuration["DB_HOST"] ?? "localhost";
var dbUser = configuration["POSTGRES_USER"];
var dbPass = configuration["POSTGRES_PASSWORD"];
var dbName = configuration["POSTGRES_DB"];

var connectionString =
    configuration["DATABASE_URL"]
    ?? $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPass}";

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

// Build the Npgsql DataSource
var dataSource = dataSourceBuilder.Build();

// Register in DI
builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IDbConnection>(_ => dataSource.CreateConnection());

// ==========================================================
// 2. REPOSITORIES & SERVICES
// ==========================================================

// --- Core Authentication & Users
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RefreshTokenRepository>();
builder.Services.AddScoped<AuthUserRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// --- Messaging & Relations
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<InviteRepository>();
builder.Services.AddScoped<InviteService>();
builder.Services.AddScoped<UserRelationRepository>();
builder.Services.AddScoped<UserRelationService>();

// --- Boards & Permissions
builder.Services.AddScoped<BoardRepository>();
builder.Services.AddScoped<BoardService>();
builder.Services.AddScoped<PermissionRepository>();
builder.Services.AddScoped<PermissionService>();

// --- Sections & Tabs
builder.Services.AddScoped<SectionRepository>();
builder.Services.AddScoped<SectionService>();
builder.Services.AddScoped<TabRepository>();
builder.Services.AddScoped<TabService>();

// --- Cards & Related Entities
builder.Services.AddScoped<CardRepository>();
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<TagRepository>();
builder.Services.AddScoped<CardTagRepository>();
builder.Services.AddScoped<LinkRepository>();
builder.Services.AddScoped<CardRelationsService>();

// --- Organizations
builder.Services.AddScoped<OrganizationRepository>();
builder.Services.AddScoped<OrganizationMemberRepository>();
builder.Services.AddScoped<OrganizationService>();

// --- Automation (Rules & Clusters)
builder.Services.AddScoped<RuleRepository>();
builder.Services.AddScoped<RuleService>();
builder.Services.AddScoped<ClusterRepository>();
builder.Services.AddScoped<ClusterService>();

// --- Activities (Audit Log)
builder.Services.AddScoped<ActivityRepository>();
builder.Services.AddScoped<ActivityService>();

// --- Files & Operations
builder.Services.AddScoped<FileRepository>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<OperationRepository>();
builder.Services.AddScoped<OperationService>();


// --- Background Worker (Job Queue)
builder.Services.AddScoped<WorkerJobRepository>();
builder.Services.AddScoped<WorkerJobAttemptRepository>();
builder.Services.AddScoped<WorkerJobDeadletterRepository>();
builder.Services.AddScoped<WorkerJobService>();
builder.Services.AddScoped<SyncService>();



// ==========================================================
// 3. AUTHENTICATION & AUTHORIZATION (JWT Configuration)
// ==========================================================
var jwtSection = configuration.GetSection("Jwt");
var jwt = jwtSection.Get<JwtOptions>() ?? throw new Exception("JWT configuration missing.");
builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true, // Enforce token expiry
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
});

// ==========================================================
// 4. CONTROLLERS + SWAGGER CONFIGURATION
// ==========================================================
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StickyBoard API", Version = "v1",
        Description = "REST API for StickyBoard cross-platform workspace (Academic Project)",
        Contact = new OpenApiContact
        {
            Name = "Alexandre Emond",
            Url = new Uri("https://aedev.pro")
        }
    });

    // JWT Auth in Swagger
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Paste your access token (no 'Bearer ' prefix).",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
    options.OperationFilter<StickyBoard.Api.Swagger.DefaultResponsesOperationFilter>();
    options.OperationFilter<StickyBoard.Api.Common.Filters.ForceJsonContentTypeFilter>();

});

// ==========================================================
// 5. BUILD APP
// ==========================================================
var app = builder.Build();

// ==========================================================
// 6. OPTIONAL CACHE-BUSTING + SWAGGER SETUP
// ==========================================================

app.UseMiddleware<ErrorHandlingMiddleware>(); // Global error handling

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/swagger/v1/swagger.json"))
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    string cacheBuster = app.Environment.IsDevelopment()
        ? DateTime.UtcNow.Ticks.ToString()
        : typeof(Program).Assembly.GetName().Version?.ToString() ?? "stable";

    options.SwaggerEndpoint($"/swagger/v1/swagger.json?v={cacheBuster}", "StickyBoard API v1");
    options.RoutePrefix = "api/swagger";
    options.DocumentTitle = "StickyBoard API – Swagger Explorer";
    options.DisplayRequestDuration();
});

// ==========================================================
// 7. MIDDLEWARE PIPELINE
// ==========================================================
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ==========================================================
// 8. DEBUG HELPER
// ==========================================================
if (app.Environment.IsDevelopment())
{
    var urls = app.Urls.FirstOrDefault() ?? "http://localhost:5000";
    var swaggerUrl = $"{urls.TrimEnd('/')}/api/swagger";

    Console.WriteLine($"\n[StickyBoard API] Running in DEVELOPMENT mode");
    Console.WriteLine($"Swagger UI available at: {swaggerUrl}\n");

    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = swaggerUrl,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not open browser automatically: {ex.Message}");
    }
}
else
{
    Console.WriteLine("\n[StickyBoard API] Running in PRODUCTION mode");
    Console.WriteLine("Swagger UI available at: /api/swagger (proxied via Apache)\n");
}

// --- Worker key log ---
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var workerKey = configuration["WORKER_API_KEY"];

if (string.IsNullOrEmpty(workerKey))
    logger.LogWarning("WORKER_API_KEY is not set. Worker authentication will fail.");
else
    logger.LogInformation("Worker key loaded successfully (base64 length: {len})", workerKey.Length);

app.Run();
