using System.Text;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RecruitmentPlatform.API.Extensions;
using RecruitmentPlatform.API.Middleware;
using RecruitmentPlatform.Application;
using RecruitmentPlatform.Application.Common.Interfaces;
using RecruitmentPlatform.Application.Common.Models;
using RecruitmentPlatform.Application.Interfaces.Infrastructure;
using RecruitmentPlatform.Infrastructure;
using RecruitmentPlatform.Infrastructure.Data;
using RecruitmentPlatform.Infrastructure.Data.Seed;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging via Serilog (configuration-driven).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Layered service registration.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as their string names for a friendlier API contract.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Recruitment Platform API",
        Version = "v1",
        Description = "REST API for the AI-powered recruitment and talent management platform."
    });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter the JWT access token (without the 'Bearer ' prefix).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = Array.Empty<string>() });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, "RecruitmentPlatform.API.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Authentication & authorization.
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");

builder.Services
    .AddAuthentication(options =>
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
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Transport security.
// HTTPS is enforced outside Development by default. It stays off in Development so the local
// http://localhost flow keeps working, and can be explicitly toggled per environment via
// Security:RequireHttps (e.g. set it false for an http-only container demo behind no TLS).
var requireHttps = builder.Configuration.GetValue("Security:RequireHttps", !builder.Environment.IsDevelopment());

// Honour X-Forwarded-Proto/-For from a TLS-terminating reverse proxy (nginx, a cloud LB) so the
// app sees the original https scheme rather than the plain http it receives inside the network.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // The proxy runs on an arbitrary container IP, so we can't pin it; clearing these accepts the
    // forwarded headers from the immediate proxy. Acceptable because the app sits behind that proxy.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// HSTS: a year, including subdomains. Never enabled in Development — it would pin localhost in the
// browser's HSTS cache and break plain http for every other local project.
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// CORS for the React client.
const string CorsPolicy = "SpaClient";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

// Apply migrations and seed baseline data on startup.
// The migrator opens its own connection before the request pipeline exists, so a transient
// failure here — chiefly the intermittent DNS resolution of the Neon pooler host — would crash the
// process on boot. Retry with backoff so a momentary blip doesn't take the app down at startup.
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    const int maxAttempts = 10;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            await DatabaseSeeder.SeedAsync(context, passwordHasher);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts && IsTransientStartupError(ex))
        {
            var delay = TimeSpan.FromSeconds(Math.Min(30, attempt * 3));
            startupLogger.LogWarning(ex,
                "Database not reachable on startup (attempt {Attempt}/{Max}); retrying in {Delay}s.",
                attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }
}

// A DNS/socket failure or a transient Npgsql connection error at boot should be retried rather
// than crashing; a genuine misconfiguration (bad password, missing table) should still fail fast.
static bool IsTransientStartupError(Exception exception)
{
    for (Exception? e = exception; e is not null; e = e.InnerException)
    {
        if (e is System.Net.Sockets.SocketException or TimeoutException) return true;
        if (e is Npgsql.NpgsqlException { IsTransient: true }) return true;
    }
    return false;
}

// Must run before anything that reads the request scheme, so the proxy's original https scheme
// is applied first.
app.UseForwardedHeaders();

if (requireHttps)
{
    // HSTS instructs browsers to only ever use https for this host; redirection upgrades any
    // stray http request. Both are no-ops in Development (requireHttps defaults false there).
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Recruitment Platform API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Liveness probe for container orchestration and docker-compose healthchecks.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
