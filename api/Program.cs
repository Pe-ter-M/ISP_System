using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Auth.Core;
using InternetProvider.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DB_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=isp_manager;Username=radius;Password=radpass";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── Auth services ────────────────────────────────────────────
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Internet Provider API")
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// ── JWT middleware (extracts user from token) ─────────────────
app.UseJwtAuth();

// ── Auto-apply migrations ─────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
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
    Console.WriteLine("┌─────────────────────────────────────────────────┐");
    Console.WriteLine("│  Internet Provider API is running               │");
    foreach (var url in urls)
        Console.WriteLine($"│  {url,-47}│");
    Console.WriteLine($"│  Scalar API docs: {string.Join(", ", urls.Select(u => $"{u}/scalar/v1"))}  │");
    Console.WriteLine("└─────────────────────────────────────────────────┘");
});

app.Run();
