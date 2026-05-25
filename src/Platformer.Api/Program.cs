using Microsoft.EntityFrameworkCore;
using Platformer.Api.Data;
using Platformer.Api.Models;
using Platformer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var dbPath = builder.Configuration.GetValue<string>("Database:Path") ?? "data/game.db";

// Ensure the data directory exists
var dbDir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
    Directory.CreateDirectory(dbDir);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<LevelGenerator>();

var app = builder.Build();

app.UseCors();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    db.Database.EnsureCreated();
}

// GET /api/game/level?seed=
app.MapGet("/api/game/level", (int? seed, LevelGenerator generator) =>
{
    int actualSeed = seed ?? Random.Shared.Next();
    var level = generator.GenerateLevel(actualSeed);
    return Results.Ok(level);
});

// POST /api/game/score
app.MapPost("/api/game/score", async (PlayerScoreRequest request, GameDbContext db) =>
{
    var score = new PlayerScore
    {
        PlayerName = request.PlayerName,
        Score = request.Score,
        Seed = request.Seed,
        Kills = request.Kills,
        DateAchieved = DateTime.UtcNow
    };
    db.PlayerScores.Add(score);
    await db.SaveChangesAsync();
    return Results.Created($"/api/game/score/{score.Id}", score);
});

// GET /api/game/leaderboard?top=10
app.MapGet("/api/game/leaderboard", async (int? top, GameDbContext db) =>
{
    int take = top ?? 10;
    var scores = await db.PlayerScores
        .OrderByDescending(s => s.Score)
        .Take(take)
        .Select(s => new
        {
            s.PlayerName,
            s.Score,
            s.Seed,
            s.Kills,
            s.DateAchieved
        })
        .ToListAsync();
    return Results.Ok(scores);
});

app.Run();

// Request DTO for POST /api/game/score
record PlayerScoreRequest(string PlayerName, int Score, int Seed, int Kills);