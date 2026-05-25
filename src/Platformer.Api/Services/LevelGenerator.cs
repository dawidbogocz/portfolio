using Platformer.Api.Models;

namespace Platformer.Api.Services;

public class LevelGenerator
{
    private const int GroundHeight = 40;
    private const int GroundY = 410; // 450 - 40
    private const int PlayerJumpHeight = 100;
    private const int MaxHorizontalGap = 160;
    private const int ChunkSize = 400;

    public LevelData GenerateLevel(int seed, int width = 800, int height = 450)
    {
        var rng = new Random(seed);
        int levelWidth = ((rng.Next(8, 14) * ChunkSize) / 100) * 100 + ChunkSize;

        var platforms = new List<Platform>();
        var coins = new List<Coin>();
        var enemies = new List<EnemyData>();
        var traps = new List<TrapData>();

        // Ground spans the full level
        platforms.Add(new Platform { X = 0, Y = GroundY, W = levelWidth, H = GroundHeight, Theme = "ground" });

        int startX = 50;
        int lastPlatformEndX = startX + 60;

        int chunkCount = levelWidth / ChunkSize;
        int totalPlatforms = 0;
        int coinIndex = 0;
        int enemyIndex = 0;

        for (int chunk = 0; chunk < chunkCount; chunk++)
        {
            int chunkStartX = chunk * ChunkSize;

            // Determine chunk theme based on position and seed
            string theme = GetChunkTheme(rng, chunk, chunkCount);

            // Number of platforms in this chunk
            int platformsInChunk = theme switch
            {
                "starter" => rng.Next(1, 3),
                "rest" => rng.Next(1, 2),
                "challenge" => rng.Next(1, 3),
                _ => rng.Next(2, 4)
            };

            for (int p = 0; p < platformsInChunk; p++)
            {
                totalPlatforms++;
                int platW = rng.Next(70, 181);
                int gapX = rng.Next(50, MaxHorizontalGap + 1);

                int platX = lastPlatformEndX + gapX;

                if (platX + platW > levelWidth - 80)
                    break;

                // Elevation varies by theme
                int platY;
                if (theme == "starter" || totalPlatforms <= 2)
                {
                    platY = GroundY - GroundHeight;
                }
                else if (theme == "challenge")
                {
                    int elevation = rng.Next(60, PlayerJumpHeight + 15);
                    platY = GroundY - elevation;
                }
                else
                {
                    int elevation = rng.Next(20, PlayerJumpHeight + 1);
                    platY = GroundY - elevation;
                }

                platforms.Add(new Platform { X = platX, Y = platY, W = platW, H = GroundHeight, Theme = theme });
                lastPlatformEndX = platX + platW;

                // Coins on this platform
                if (theme != "challenge" || rng.NextDouble() > 0.4)
                {
                    int coinsOnPlat = rng.Next(1, 4);
                    for (int c = 0; c < coinsOnPlat; c++)
                    {
                        coinIndex++;
                        int coinX = rng.Next(platX + 10, platX + platW - 10);
                        int coinY = platY - rng.Next(20, 60);
                        coins.Add(new Coin { X = coinX, Y = coinY, Id = $"coin_{coinIndex}" });
                    }
                }

                // Enemies on or near this platform
                if (theme == "normal" || theme == "challenge")
                {
                    if (rng.NextDouble() < 0.5)
                    {
                        enemyIndex++;
                        int eW = 28;
                        int eH = 24;
                        int eX = rng.Next(platX + 10, platX + platW - eW - 10);
                        int eY = platY - eH;
                        double speed = 0.8 + rng.NextDouble() * 1.2;
                        int patrolRange = rng.Next(40, Math.Min(platW - 20, 100));

                        enemies.Add(new EnemyData
                        {
                            X = eX, Y = eY, W = eW, H = eH,
                            Speed = Math.Round(speed, 1),
                            PatrolStart = eX - patrolRange / 2,
                            PatrolEnd = eX + patrolRange / 2
                        });
                    }
                }

                // Traps / spikes on challenge platforms
                if (theme == "challenge" && rng.NextDouble() < 0.6)
                {
                    int spikeW = rng.Next(30, 80);
                    int spikeX = rng.Next(platX + 10, platX + platW - spikeW - 10);
                    traps.Add(new TrapData
                    {
                        X = spikeX,
                        Y = platY - 8,
                        W = spikeW,
                        H = 8
                    });
                }
            }
        }

        // Final platform for the end zone
        int endPlatX = lastPlatformEndX + 60;
        platforms.Add(new Platform { X = endPlatX, Y = GroundY - GroundHeight, W = 120, H = GroundHeight, Theme = "end" });
        int endX = endPlatX + 60;
        int endY = GroundY - GroundHeight - 5;

        return new LevelData
        {
            Seed = seed,
            Width = levelWidth,
            Height = height,
            StartX = startX,
            StartY = GroundY - 40,
            Platforms = platforms,
            Coins = coins,
            Enemies = enemies,
            Traps = traps,
            EndX = endX,
            EndY = endY
        };
    }

    private string GetChunkTheme(Random rng, int chunkIndex, int totalChunks)
    {
        if (chunkIndex == 0)
            return "starter";
        if (chunkIndex >= totalChunks - 2)
            return "rest";

        double roll = rng.NextDouble();
        if (roll < 0.35)
            return "normal";
        else if (roll < 0.55)
            return "challenge";
        else
            return "rest";
    }
}