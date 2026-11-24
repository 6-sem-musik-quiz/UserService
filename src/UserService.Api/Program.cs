using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using UserService.Api.Middleware;
using UserService.Repository.Data;
using UserService.Repository.Extensions;
using UserService.Service.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// /health is liveness (the process is up); /health/ready also checks
// that the database answers, so a pod without a DB gets no traffic.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres", tags: ["ready"]);

builder.Services.AddRepositoryLayer(
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured."));

builder.Services.AddServiceLayer();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();

// Configure JWT options via DI so WebApplicationFactory configuration overrides are applied
// (same pattern as the key validation below — both run after all config sources are merged).
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, config) =>
    {
        var key = config["Jwt:SigningKey"];
        if (string.IsNullOrEmpty(key))
            return; // startup validation below will throw before any requests arrive

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "slopster-userservice",
            ValidateAudience = true,
            ValidAudience = "slopster",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
        };
    });

var app = builder.Build();

// Validate after Build() so WebApplicationFactory configuration overrides are applied.
var jwtKey = app.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured. Set the Jwt__SigningKey environment variable.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");

// Bring the database schema up to date before taking traffic. Tests turn this off
// with Database:MigrateOnStartup=false when they have no Postgres.
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
    app.Services.MigrateDatabase();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

app.Run();

// Exposed so integration tests can use WebApplicationFactory<Program>.
public partial class Program;
