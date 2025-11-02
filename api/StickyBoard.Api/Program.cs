using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using StickyBoard.Api.Auth;
using StickyBoard.Api.Common.Filters;
using StickyBoard.Api.Middleware;
using StickyBoard.Api.Models;
using StickyBoard.Api.Services;
using StickyBoard.Api.Repositories.BoardsAndCards;
using StickyBoard.Api.Repositories.Organizations;
using StickyBoard.Api.Repositories.SocialAndMessaging;
using StickyBoard.Api.Repositories.UsersAndAuth;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ==========================================================
// 1. DATABASE CONNECTION (NpgsqlDataSource with Enum Mapping)
// ==========================================================
var dbHost = configuration["DB_HOST"] ?? "localhost";
var dbUser = configuration["POSTGRES_USER"];
var dbPass = configuration["POSTGRES_PASSWORD"];
var dbName = configuration["POSTGRES_DB"];

string connectionString;

var dbUrl = configuration["DATABASE_URL"];
if (!string.IsNullOrEmpty(dbUrl) && dbUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
{
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

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<UserRole>("user_role");
dataSourceBuilder.MapEnum<BoardRole>("board_role");
dataSourceBuilder.MapEnum<OrgRole>("org_role");
dataSourceBuilder.MapEnum<BoardVisibility>("board_visibility");
dataSourceBuilder.MapEnum<TabScope>("tab_scope");
dataSourceBuilder.MapEnum<TabType>("tab_type");
dataSourceBuilder.MapEnum<CardType>("card_type");
dataSourceBuilder.MapEnum<CardStatus>("card_status");
dataSourceBuilder.MapEnum<MessageType>("message_type");
dataSourceBuilder.MapEnum<MessageStatus>("message_status");
dataSourceBuilder.MapEnum<RelationStatus>("relation_status");


var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IDbConnection>(_ => dataSource.CreateConnection());

// ==========================================================
// 2. REPOSITORIES & SERVICES
// ==========================================================

// ----------------------------------------------------------
// Core Auth / Users
// ----------------------------------------------------------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthUserRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<RefreshTokenRepository>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ----------------------------------------------------------
// Boards / Folders / Permissions / Tabs / Sections / Cards
// ----------------------------------------------------------
builder.Services.AddScoped<BoardRepository>();
builder.Services.AddScoped<BoardService>();

builder.Services.AddScoped<BoardFolderRepository>();
builder.Services.AddScoped<BoardFolderService>();

builder.Services.AddScoped<PermissionRepository>();
builder.Services.AddScoped<PermissionService>();

builder.Services.AddScoped<TabRepository>();
builder.Services.AddScoped<TabService>();

builder.Services.AddScoped<SectionRepository>();
builder.Services.AddScoped<SectionService>();

builder.Services.AddScoped<CardRepository>();
builder.Services.AddScoped<CardService>();

// ----------------------------------------------------------
// Card Comments & Board Chat Messages
// ----------------------------------------------------------
builder.Services.AddScoped<CardCommentRepository>();
builder.Services.AddScoped<CardCommentService>();

builder.Services.AddScoped<BoardMessageRepository>();
builder.Services.AddScoped<BoardMessageService>();

// ----------------------------------------------------------
// Organizations
// ----------------------------------------------------------
builder.Services.AddScoped<OrganizationRepository>();
builder.Services.AddScoped<OrganizationMemberRepository>();
builder.Services.AddScoped<OrganizationService>();

// ----------------------------------------------------------
// Messaging / Invites / User Relations
// ----------------------------------------------------------
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<MessageService>();

builder.Services.AddScoped<InviteRepository>();
builder.Services.AddScoped<InviteService>();

builder.Services.AddScoped<UserRelationRepository>();
builder.Services.AddScoped<UserRelationService>();

// ==========================================================
// 3. AUTHENTICATION & AUTHORIZATION (JWT + API KEY)
// ==========================================================
var jwtSection = configuration.GetSection("Jwt");
var jwt = jwtSection.Get<JwtOptions>() ?? throw new Exception("JWT configuration missing.");
builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
})
.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("WorkerOrAdmin", policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ApiKey")
              .RequireAuthenticatedUser()
              .RequireRole("worker", "admin"));
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
        Contact = new OpenApiContact { Name = "Alexandre Emond", Url = new Uri("https://aedev.pro") }
    });

    // JWT
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "JWT Authorization (use: Bearer <token>)",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    // API Key
    var apiKeyScheme = new OpenApiSecurityScheme
    {
        Description = "Worker API Key (use: ApiKey <your_key>)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey",
        Reference = new OpenApiReference
        {
            Id = "ApiKey",
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(apiKeyScheme.Reference.Id, apiKeyScheme);

    // Require both to appear in Swagger
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, new string[] {} },
        { apiKeyScheme, new string[] {} }
    });

    options.OperationFilter<DefaultResponsesOperationFilter>();
    options.OperationFilter<StickyBoard.Api.Common.Filters.ForceJsonContentTypeFilter>();
});

// ==========================================================
// 5. BUILD APP
// ==========================================================
var app = builder.Build();

// ==========================================================
// 6. SWAGGER + MIDDLEWARE
// ==========================================================
app.UseMiddleware<ErrorHandlingMiddleware>();
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
// 7. PIPELINE
// ==========================================================
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ==========================================================
// 8. LOGGING
// ==========================================================
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var workerKey = configuration["WORKER_API_KEY"];

if (string.IsNullOrEmpty(workerKey))
    logger.LogWarning("WORKER_API_KEY is not set. Worker authentication will fail.");
else
    logger.LogInformation("Worker key loaded successfully (length: {len})", workerKey.Length);

app.Run();
