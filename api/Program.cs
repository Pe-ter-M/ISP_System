using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using InternetProvider.Api.Modules.Infrastructure;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Auth.Core;
using InternetProvider.Api.Modules.Auth.Interfaces;
using InternetProvider.Api.Modules.Users.Interfaces;
using InternetProvider.Api.Modules.Users.Core;
using InternetProvider.Api.Modules.Settings.Interfaces;
using InternetProvider.Api.Modules.Settings.Core;
using InternetProvider.Api.Modules.Organization.Interfaces;
using InternetProvider.Api.Modules.Organization.Core;
using InternetProvider.Api.Modules.Plans.Interfaces;
using InternetProvider.Api.Modules.Plans.Core;
using InternetProvider.Api.Services;

// ── Serilog bootstrap (catches startup errors before config loads) ──
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog from appsettings ─────────────────────────────────
    builder.Host.UseSerilog((context, config) =>
    {
        config.ReadFrom.Configuration(context.Configuration);
    });

    // ── Database ─────────────────────────────────────────────────
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=isp_manager;Username=radius;Password=radpass";

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    // ── Auth services ────────────────────────────────────────────
    builder.Services.AddSingleton<JwtService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IAuthRepository, AuthRepository>();

    // ── User services ────────────────────────────────────────────
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();

    // ── Settings services ────────────────────────────────────────────
    builder.Services.AddScoped<ISettingRepository, SettingRepository>();
    builder.Services.AddScoped<ISettingService, SettingService>();

    // ── Organization services ────────────────────────────────────────
    builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
    builder.Services.AddScoped<IOrganizationService, OrganizationService>();

    // ── Plan services ────────────────────────────────────────────────
    builder.Services.AddScoped<IPlanRepository, PlanRepository>();
    builder.Services.AddScoped<IPlanService, PlanService>();

    builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste your JWT token here. Get it from POST /api/auth/login"
            }
        };
        return Task.CompletedTask;
    });
});

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Internet Provider API")
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                   .WithPreferredScheme("Bearer");
        });
    }

    // ── Exception middleware ─────────────────────────────────────
    app.UseMiddleware<ExceptionMiddleware>();

    // ── JWT middleware ───────────────────────────────────────────
    app.UseJwtAuth();

    // ── Auto-apply migrations + seed ─────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        await DatabaseSeeder.SeedAsync(db);
    }

    // ── Endpoints ─────────────────────────────────────────────────
    app.MapAllEndpoints();

    // ── Health ────────────────────────────────────────────────────
    app.MapGet("/api/health", () =>
    {
        return Results.Ok(new
        {
            status = "healthy",
            service = "Internet Provider API",
            version = "1.0.0",
            timestamp = DateTime.UtcNow
        });
    });

    // ── Print startup info ───────────────────────────────────────
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var urls = app.Urls;
        Log.Information("Internet Provider API started");
        foreach (var url in urls)
            Log.Information("Listening on {Url}", url);
        Log.Information("Scalar docs at {Url}/scalar/v1", urls.FirstOrDefault());
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
