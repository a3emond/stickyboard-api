using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StickyBoard.Api.Common;
using StickyBoard.Api.Common.Filters;
using StickyBoard.Api.Middleware;
using StickyBoard.Core.Auth;
using StickyBoard.Core.Infrastructure.Db;
using StickyBoard.Core.Repositories.Attachments;
using StickyBoard.Core.Repositories.Attachments.Contracts;
using StickyBoard.Core.Repositories.Automation.Jobs;
using StickyBoard.Core.Repositories.BoardsAndCards;
using StickyBoard.Core.Repositories.BoardsAndCards.Contracts;
using StickyBoard.Core.Repositories.SocialAndMessaging;
using StickyBoard.Core.Repositories.SocialAndMessaging.Contracts;
using StickyBoard.Core.Repositories.UsersAndAuth;
using StickyBoard.Core.Repositories.UsersAndAuth.Contracts;
using StickyBoard.Core.Services.Attachments;
using StickyBoard.Core.Services.Automation.Workers;
using StickyBoard.Core.Services.BoardsAndCards;
using StickyBoard.Core.Services.BoardsAndCards.Contracts;
using StickyBoard.Core.Services.SocialAndMessaging;
using StickyBoard.Core.Services.SocialAndMessaging.Contracts;
using StickyBoard.Core.Services.UsersAndAuth;
using StickyBoard.Core.Services.UsersAndAuth.Contracts;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ==========================================================
// 1. DATABASE CONNECTION (NpgsqlDataSource with Enum Mapping)
// ==========================================================

var connectionString = ConnectionStringBuilder.Build(configuration);
var dataSource       = DataSourceFactory.Create(connectionString);

builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<IDbConnection>(_ => dataSource.CreateConnection());

// ==========================================================
// 2. REPOSITORIES & SERVICES
// ==========================================================
builder.Services.AddHttpClient(); // TODO: verify if added properly
// ----------------------------------------------------------
// Attachments
// ----------------------------------------------------------
builder.Services.AddScoped<IAttachmentManager, AttachmentManager>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IAttachmentVariantRepository, AttachmentVariantRepository>();
builder.Services.AddScoped<IFileTokenRepository, FileTokenRepository>();

// ----------------------------------------------------------
// Automation / Workers
// ----------------------------------------------------------
builder.Services.AddScoped<WorkerQueueService>();
builder.Services.AddScoped<IWorkerJobRepository, WorkerJobRepository>();
builder.Services.AddScoped<IWorkerJobAttemptRepository, WorkerJobAttemptRepository>();

// ----------------------------------------------------------
// Boards & Cards
// ----------------------------------------------------------
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IViewService, ViewService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();

builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IBoardMemberRepository, BoardMemberRepository>();
builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<ICardReadRepository, CardReadRepository>();
builder.Services.AddScoped<IViewRepository, ViewRepository>();
builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
builder.Services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();

// ----------------------------------------------------------
// Social & Messaging
// ----------------------------------------------------------
builder.Services.AddScoped<ICardCommentService, CardCommentService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IInboxMessageService, InboxMessageService>();
builder.Services.AddScoped<IInviteService, InviteService>();
builder.Services.AddScoped<IMentionService, MentionService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<ICardCommentRepository, CardCommentRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();
builder.Services.AddScoped<IInviteRepository, InviteRepository>();
builder.Services.AddScoped<IMentionRepository, MentionRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// ----------------------------------------------------------
// Users & Auth (interfaces + implementations)
// ----------------------------------------------------------
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthUserRepository, AuthUserRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();



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
    options.OperationFilter<ForceJsonContentTypeFilter>();
    options.OperationFilter<FileUploadOperationFilter>();

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
