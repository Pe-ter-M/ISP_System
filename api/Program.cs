var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ── Health endpoint ──────────────────────────────────────────
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        service = "Internet Provider API",
        version = "1.0.0",
        timestamp = DateTime.UtcNow
    });
})
.WithName("HealthCheck");

// ── Hello World ──────────────────────────────────────────────
app.MapGet("/", () =>
{
    return Results.Ok(new { message = "Hello World" });
});

app.Run();
