using System.Threading.RateLimiting;
using Masroof.Api.Endpoints;
using Masroof.Api.Health;
using Masroof.Api.Identity;
using Masroof.Api.Infrastructure;
using Masroof.Application;
using Masroof.Application.Abstractions;
using Masroof.Infrastructure;
using Masroof.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

var config = builder.Configuration;

// --- Application + Infrastructure ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddMasroofApplication();
builder.Services.AddMasroofInfrastructure(config);
builder.Services.AddScoped<EnsureUserFilter>();

// --- ProblemDetails + exception handling ---
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// --- AuthN/Z ---
var devBypass = config.GetValue("Auth:DevBypass", false);
if (devBypass)
{
    builder.Services.AddAuthentication(DevAuthOptions.Scheme)
        .AddScheme<DevAuthOptions, DevAuthenticationHandler>(DevAuthOptions.Scheme, o =>
        {
            config.GetSection("Auth:DevUser").Bind(o);
        });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = config["Auth:Authority"];
            options.Audience = config["Auth:Audience"];
            options.RequireHttpsMetadata = config.GetValue("Auth:RequireHttpsMetadata", true);
            options.TokenValidationParameters.NameClaimType = "name";
        });
}
builder.Services.AddAuthorization();

// --- CORS for the SPA ---
var corsOrigins = config.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddPolicy("spa", p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// --- Rate limiting (per-user) on the LLM-backed endpoints ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("llm", httpContext =>
    {
        var key = httpContext.User.FindFirst("sub")?.Value
                  ?? httpContext.Connection.RemoteIpAddress?.ToString()
                  ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = config.GetValue("RateLimit:LlmPerMinute", 20),
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

// --- Health, OpenAPI ---
builder.Services.AddHealthChecks()
    .AddCheck<SqlHealthCheck>("sql", tags: ["ready"])
    .AddCheck<LlmHealthCheck>("llm", tags: ["ready"]);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    if (config.GetValue("Database:AutoMigrate", false))
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<MasroofDbContext>().Database.MigrateAsync();
    }
}

app.UseCors("spa");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// --- Health probes ---
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = hc => hc.Tags.Contains("ready") });

// --- API surface (authenticated + user-provisioned) ---
var api = app.MapGroup("/api")
    .RequireAuthorization()
    .AddEndpointFilter<EnsureUserFilter>();

api.MapTransactionEndpoints();
api.MapReportEndpoints();
api.MapAskEndpoints();
api.MapRuleEndpoints();
api.MapCategoryEndpoints();

app.Run();

/// <summary>Exposed so integration tests can reference the API host.</summary>
public partial class Program;
