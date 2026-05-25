using Platformer.Api.Models;

namespace Platformer.Api.Services;

public class LevelGenerator
{
    private const int GroundHeight = 40;
    private const int GroundY = 410;
    private const int PlayerJumpHeight = 110;
    private const int ChunkSize = 400;
    private const int MinLevelWidth = 2400;
    private const int MaxHorizontalGap = 160;

    public LevelData GenerateLevel(int seed, int height = 450)
    {
        var rng = new Random(seed);
        int levelWidth = Math.Max(MinLevelWidth, (rng.Next(8, 14) * ChunkSize / 100) * 100 + ChunkSize);

        var platforms = new List<Platform>();
        var coins = new List<Coin>();
        var enemies = new List<EnemyData>();
        var traps = new List<TrapData>();

        int startX = 50;
        int lastPlatformEndX = startX + 60;
        int coinIndex = 0;
        int enemyIndex = 0;
        int groundSegmentIndex = 0;

        int chunkCount = levelWidth / ChunkSize;
        int totalPlatforms = 0;

        // Track the current ground Y for vertical sections
        int currentGroundY = GroundY;

        for (int chunk = 0; chunk < chunkCount; chunk++)
        {
            int chunkStartX = chunk * ChunkSize;
            string theme = GetChunkTheme(rng, chunk, chunkCount);

            // === GROUND SEGMENT (with optional gaps) ===
            bool placeGroundInThisChunk = true;
            int groundGapChance = theme switch
            {
                "challenge" => 50,
                "normal" => 35,
                "rest" => 10,
                "starter" => 0,
                _ => 25
            };

            // Skip ground placement for certain challenge chunks
            if (theme == "challenge" && rng.Next(100) < groundGapChance)
                placeGroundInThisChunk = false;

            if (placeGroundInThisChunk)
            {
                // Decide if ground in this chunk is same or different Y (descend/ascent)
                if (theme == "challenge" && rng.NextDouble() < 0.35)
                {
                    // Underground section: lower ground, player drops into it
                    currentGroundY = Math.Min(GroundY + 60, currentGroundY + 40);
                }
                else if (theme == "rest" && currentGroundY > GroundY)
                {
                    // Ascend back to normal ground
                    currentGroundY = Math.Max(GroundY, currentGroundY - 40);
                }
                else if (currentGroundY > GroundY && rng.NextDouble() < 0.3)
                {
                    currentGroundY = Math.Max(GroundY, currentGroundY - 30);
                }

                int groundW = ChunkSize;
                // Occasionally make the ground shorter (creates the gap for next chunk)
                if (theme != "starter" && rng.Next(100) < groundGapChance)
                {
                    groundW = ChunkSize - rng.Next(80, 200);
                }

                groundSegmentIndex++;
                platforms.Add(new Platform
                {
                    X = chunkStartX,
                    Y = currentGroundY,
                    W = groundW,
                    H = GroundHeight,
                    Theme = "ground"
                });

                // If ground is shorter, place a safety platform below the gap
                if (groundW < ChunkSize - 20)
                {
                    int gapStart = chunkStartX + groundW;
                    int gapEnd = chunkStartX + ChunkSize;
                    int gapWidth = gapEnd - gapStart;
                    if (gapWidth > 60)
                    {
                        // Safety platform below the gap
                        platforms.Add(new Platform
                        {
                            X = gapStart + 10,
                            Y = currentGroundY + 30,
                            W = gapWidth - 20,
                            H = GroundHeight / 2,
                            Theme = "rest"
                        });
                    }
                }
            }
            else
            {
                // No ground this chunk: whole chunk is a gap with a single narrow bridge platform
                int bridgeX = chunkStartX + ChunkSize / 2 - 40;
                platforms.Add(new Platform
                {
                    X = bridgeX,
                    Y = currentGroundY,
                    W = 80,
                    H = GroundHeight,
                    Theme = "challenge"
                });
                // Safety platform below
                platforms.Add(new Platform
                {
                    X = chunkStartX + 20,
                    Y = currentGroundY + 50,
                    W = ChunkSize - 40,
                    H = GroundHeight / 2,
                    Theme = "rest"
                });
            }

            // === OVERHEAD PLATFORMS ===
            int platformsInChunk = theme switch
            {
                "starter" => rng.Next(1, 3),
                "rest" => rng.Next(1, 2),
                "challenge" => rng.Next(3, 5),
                _ => rng.Next(2, 4)
            };

            for (int p = 0; p < platformsInChunk; p++)
            {
                totalPlatforms++;
                int platW = theme == "challenge" ? rng.Next(50, 140) : rng.Next(70, 181);
                int gapX = theme == "challenge" ? rng.Next(60, 140) : rng.Next(50, MaxHorizontalGap + 1);

                int platX = lastPlatformEndX + gapX;
                if (platX + platW > levelWidth - 80)
                    break;

                // Elevation
                int platY;
                if (theme == "starter" || totalPlatforms <= 2)
                {
                    platY = currentGroundY - GroundHeight;
                }
                else if (theme == "challenge" && p < 2)
                {
                    // Staircase: first 2 platforms in challenge are steps
                    int stepHeight = (p + 1) * 35;
                    platY = currentGroundY - PlayerJumpHeight - stepHeight;
                }
                else if (theme == "challenge")
                {
                    int elevation = rng.Next(50, PlayerJumpHeight + 25);
                    platY = currentGroundY - elevation;
                }
                else if (theme == "rest")
                {
                    // Rest platforms are low, easy to reach
                    platY = currentGroundY - rng.Next(20, 60);
                }
                else
                {
                    int elevation = rng.Next(20, PlayerJumpHeight + 1);
                    platY = currentGroundY - elevation;
                }

                platforms.Add(new Platform
                {
                    X = platX,
                    Y = platY,
                    W = platW,
                    H = GroundHeight,
                    Theme = theme
                });
                lastPlatformEndX = platX + platW;

                // === COINS ===
                bool placeCoins = theme != "challenge" || rng.NextDouble() > 0.2;
                if (placeCoins)
                {
                    int coinsOnPlat = theme == "challenge"
                        ? rng.Next(2, 5)
                        : rng.Next(1, 4);

                    // For high platforms, place more coins as reward
                    int rewardBonus = 0;
                    if (platY < currentGroundY - PlayerJumpHeight - 20)
                        rewardBonus = 3;

                    coinsOnPlat += rewardBonus;

                    for (int c = 0; c < coinsOnPlat; c++)
                    {
                        coinIndex++;
                        int coinX = rng.Next(platX + 10, platX + platW - 10);
                        int coinY;
                        if (c == 0)
                            coinY = platY - 30;
                        else
                            coinY = platY - rng.Next(20, 70);
                        coins.Add(new Coin
                        {
                            X = coinX,
                            Y = coinY,
                            Id = $"coin_{coinIndex}"
                        });
                    }
                }

                // === ENEMIES ===
                bool placeEnemies = theme == "normal" || theme == "challenge";
                int maxEnemies = theme == "challenge" ? 2 : 1;
                if (placeEnemies)
                {
                    int enemiesOnPlat = rng.Next(0, maxEnemies + 1);
                    for (int e = 0; e < enemiesOnPlat; e++)
                    {
                        if (rng.NextDouble() < 0.55 && platW >= 60)
                        {
                            enemyIndex++;
                            int eW = 28;
                            int eH = 24;
                            int maxEnemyX = Math.Max(platX + 10, platX + platW - eW - 10);
                            int eX = rng.Next(platX + 10, maxEnemyX + 1);
                            int eY = platY - eH;
                            double speed = 0.8 + rng.NextDouble() * 1.4;
                            int maxPatrol = Math.Max(40, Math.Min(platW - 20, 100));
                            int patrolRange = rng.Next(Math.Min(40, maxPatrol), maxPatrol + 1);

                            enemies.Add(new EnemyData
                            {
                                X = eX, Y = eY, W = eW, H = eH,
                                Speed = Math.Round(speed, 1),
                                PatrolStart = eX - patrolRange / 2,
                                PatrolEnd = eX + patrolRange / 2
                            });
                        }
                    }
                }

                // === TRAPS (spikes) ===
                // On challenge platforms
                if (theme == "challenge" && rng.NextDouble() < 0.6)
                {
                    int maxSpikeW = Math.Min(80, platW - 40);
                    if (maxSpikeW >= 30)
                    {
                        int spikeW = rng.Next(30, maxSpikeW + 1);
                        int maxSpikeX = Math.Max(platX + 10, platX + platW - spikeW - 10);
                        int spikeX = rng.Next(platX + 10, maxSpikeX + 1);
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

            // === GROUND-LEVEL SPIKE GAUNTLET ===
            // In challenge chunks, place spikes on the ground
            if (theme == "challenge" && rng.NextDouble() < 0.4 && placeGroundInThisChunk)
            {
                int maxRunW = Math.Min(250, ChunkSize - 80);
                if (maxRunW >= 100)
                {
                    int spikeRunW = rng.Next(100, maxRunW + 1);
                    int maxRunX = Math.Max(40, ChunkSize - spikeRunW - 20);
                    int spikeRunX = chunkStartX + rng.Next(Math.Min(40, maxRunX), maxRunX + 1);
                    traps.Add(new TrapData
                    {
                        X = spikeRunX,
                        Y = currentGroundY - 8,
                        W = spikeRunW,
                        H = 8
                    });
                }
            }

            // === COINS IN THE AIR (over gaps) ===
            // Guide the player across gaps
            if (!placeGroundInThisChunk || (theme != "starter" && rng.NextDouble() < 0.3))
            {
                int airCoins = rng.Next(1, 4);
                for (int ac = 0; ac < airCoins; ac++)
                {
                    coinIndex++;
                    coins.Add(new Coin
                    {
                        X = chunkStartX + ChunkSize / 2 + rng.Next(-60, 60),
                        Y = currentGroundY - rng.Next(60, 130),
                        Id = $"coin_{coinIndex}"
                    });
                }
            }
        }

        // === END ZONE ===
        // Stair-step platforms leading up to the goal
        int endStepX = lastPlatformEndX + 60;
        for (int step = 0; step < 3; step++)
        {
            int stepW = 80;
            int stepY = GroundY - GroundHeight - step * 35;
            platforms.Add(new Platform
            {
                X = endStepX + step * 50,
                Y = stepY,
                W = stepW,
                H = GroundHeight,
                Theme = "end"
            });

            // Coins on each step
            for (int ec = 0; ec < 3; ec++)
            {
                coinIndex++;
                coins.Add(new Coin
                {
                    X = endStepX + step * 50 + 10 + ec * 20,
                    Y = stepY - 30 - ec * 5,
                    Id = $"coin_{coinIndex}"
                });
            }
        }

        int finalPlatX = endStepX + 2 * 50 + 80;
        platforms.Add(new Platform
        {
            X = finalPlatX,
            Y = GroundY - GroundHeight - 70,
            W = 120,
            H = GroundHeight,
            Theme = "end"
        });
        int endX = finalPlatX + 60;
        int endY = GroundY - GroundHeight - 70;

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
        if (roll < 0.30)
            return "normal";
        else if (roll < 0.55)
            return "challenge";
        else
            return "rest";
    }
}