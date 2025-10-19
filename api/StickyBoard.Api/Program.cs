using System.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// -------------------
// Database connection setup
// -------------------
var dbHost = configuration["DB_HOST"] ?? "localhost";
var dbUser = configuration["POSTGRES_USER"];
var dbPass = configuration["POSTGRES_PASSWORD"];
var dbName = configuration["POSTGRES_DB"];

var connectionString =
    configuration["DATABASE_URL"]
    ?? $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPass}";

// Register a scoped PostgreSQL connection for dependency injection
builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

// -------------------
// Middleware setup
// -------------------
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// -------------------
// App entry point
// -------------------
app.Run();