using System.Data;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using StickyBoard.Api.Auth;
using StickyBoard.Api.Repositories;
using StickyBoard.Api.Services;
using StickyBoard.Api.Models.Enums;

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

// Create a DataSourceBuilder and register all enums
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<BoardVisibility>("board_visibility");
dataSourceBuilder.MapEnum<BoardRole>("board_role");
dataSourceBuilder.MapEnum<TabScope>("tab_scope");
dataSourceBuilder.MapEnum<CardType>("card_type");
dataSourceBuilder.MapEnum<CardStatus>("card_status");
dataSourceBuilder.MapEnum<LinkType>("link_type");
dataSourceBuilder.MapEnum<ClusterType>("cluster_type");
dataSourceBuilder.MapEnum<ActivityType>("activity_type");
dataSourceBuilder.MapEnum<EntityType>("entity_type");
dataSourceBuilder.MapEnum<JobKind>("job_kind");
dataSourceBuilder.MapEnum<JobStatus>("job_status");

// Build a single shared DataSource instance
var dataSource = dataSourceBuilder.Build();

// Register it for dependency injection
builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IDbConnection>(_ => dataSource.CreateConnection());

// ==========================================================
// 2. REPOSITORIES & SERVICES
// ==========================================================
// Repositories now use NpgsqlDataSource (no need for string constructor)
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<AuthUserRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// ==========================================================
// 3. AUTHENTICATION & AUTHORIZATION
// ==========================================================
var jwtSection = configuration.GetSection("Jwt");
var jwt = jwtSection.Get<JwtOptions>() ?? throw new Exception("JWT configuration missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

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
        Title = "StickyBoard API",
        Version = "v1",
        Description = "REST API for StickyBoard cross-platform workspace (Academic Project)",
        Contact = new OpenApiContact
        {
            Name = "Alexandre Emond",
            Url = new Uri("https://aedev.pro")
        }
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Paste your JWT token here (without 'Bearer ' prefix).",
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
});

// ==========================================================
// 5. BUILD APP
// ==========================================================
var app = builder.Build();

// ==========================================================
// 6. OPTIONAL CACHE-BUSTING + SWAGGER SETUP
// ==========================================================
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

app.Run();
