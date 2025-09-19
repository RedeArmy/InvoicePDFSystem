using System.Data;
using Dapper;
using Npgsql;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load .env variables
Env.Load("../../.env");

// Add environment variables (from system or docker)
builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Get connection string
var rawConnectionString = builder.Configuration.GetConnectionString("PostgresDb");

var connectionString = rawConnectionString?
    .Replace("${DB_HOST}", Environment.GetEnvironmentVariable("DB_HOST"))
    .Replace("${DB_PORT}", Environment.GetEnvironmentVariable("DB_PORT"))
    .Replace("${DB_NAME}", Environment.GetEnvironmentVariable("DB_NAME"))
    .Replace("${DB_USER}", Environment.GetEnvironmentVariable("DB_USER"))
    .Replace("${DB_PASSWORD}", Environment.GetEnvironmentVariable("DB_PASSWORD"));


if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The Postgres connection string is not configured correctly.");

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/test-db", async (IDbConnection db) =>
{
    if (db.State == ConnectionState.Closed)
        db.Open();

    var result = await db.QueryAsync<string>("SELECT version();");
    return Results.Ok(result);
});

app.Run();