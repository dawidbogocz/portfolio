using Platformer.Api.Models;

namespace Platformer.Api.Services;

public class LevelGenerator
{
    private const int GroundHeight = 40;
    private const int GroundY = 410;
    private const int ChunkSize = 400;
    private const int MinLevelWidth = 2400;
    private const int MaxHorizontalGap = 160;
    private const int MaxPlatformElevation = 150; // max px above player start Y that's reachable w/ double jump
    private const int SafeElevation = 90;         // reachable with a single jump
    private const int PlayerStartY = 370;          // GroundY - GroundHeight

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
        int currentGroundY = GroundY;

        bool hasOverheadPlatformAboveGap = false;

        for (int chunk = 0; chunk < chunkCount; chunk++)
        {
            int chunkStartX = chunk * ChunkSize;
            string theme = GetChunkTheme(rng, chunk, chunkCount);

            // === GROUND SEGMENT ===
            bool placeGroundInThisChunk = true;
            int groundGapChance = theme switch
            {
                "challenge" => 50,
                "normal" => 35,
                "rest" => 10,
                "starter" => 0,
                _ => 25
            };

            if (theme == "challenge" && rng.Next(100) < groundGapChance)
                placeGroundInThisChunk = false;

            if (placeGroundInThisChunk)
            {
                // Underground / ascent sections
                if (theme == "challenge" && rng.NextDouble() < 0.3)
                    currentGroundY = Math.Min(GroundY + 40, currentGroundY + 30);
                else if (theme == "rest" && currentGroundY > GroundY)
                    currentGroundY = Math.Max(GroundY, currentGroundY - 30);
                else if (currentGroundY > GroundY && rng.NextDouble() < 0.3)
                    currentGroundY = Math.Max(GroundY, currentGroundY - 20);

                int groundW = ChunkSize;
                if (theme != "starter" && rng.Next(100) < groundGapChance)
                    groundW = ChunkSize - rng.Next(80, 200);

                groundSegmentIndex++;
                platforms.Add(new Platform { X = chunkStartX, Y = currentGroundY, W = groundW, H = GroundHeight, Theme = "ground" });

                hasOverheadPlatformAboveGap = false;
            }
            else
            {
                // NO GROUND THIS CHUNK -- gap. Player must jump across or die.
                int bridgeX = chunkStartX + ChunkSize / 2 - 40;

                // Bridge platform at ground level
                platforms.Add(new Platform { X = bridgeX, Y = currentGroundY, W = 80, H = GroundHeight, Theme = "challenge" });

                // Stepping-stone platform slightly lower so player can reach it from the bridge
                platforms.Add(new Platform { X = bridgeX - 30, Y = currentGroundY - 30, W = 60, H = 24, Theme = "challenge" });

                // Second stepping stone further into the gap at medium height
                int stone2X = bridgeX + 100 + rng.Next(0, 40);
                int stone2W = rng.Next(50, 80);
                int stone2Y = currentGroundY - rng.Next(50, 80);
                platforms.Add(new Platform { X = stone2X, Y = stone2Y, W = stone2W, H = 24, Theme = "challenge" });

                // Guide coins on stepping stones
                for (int gc = 0; gc < 2; gc++)
                {
                    coinIndex++;
                    coins.Add(new Coin
                    {
                        X = bridgeX - 10 + gc * 25,
                        Y = currentGroundY - 55 - gc * 5,
                        Id = $"coin_{coinIndex}"
                    });
                }

                // Advance lastPlatformEndX past this gap so no overhead bleeds into the hole
                lastPlatformEndX = chunkStartX + ChunkSize + rng.Next(50, 150);

                // MARKER: this chunk has a gap -- no overhead platforms allowed
                hasOverheadPlatformAboveGap = true;
            }

            // === OVERHEAD PLATFORMS ===
            // RULE: No overhead platforms above ground gaps (player must fall through)
            // RULE: Every platform must be reachable (elevation capped to MaxPlatformElevation)
            if (!hasOverheadPlatformAboveGap)
            {
                int platformsInChunk = theme switch
                {
                    "starter" => rng.Next(1, 3),
                    "rest" => rng.Next(1, 2),
                    "challenge" => rng.Next(2, 4),
                    _ => rng.Next(2, 4)
                };

                for (int p = 0; p < platformsInChunk; p++)
                {
                    totalPlatforms++;
                    int platW = theme == "challenge" ? rng.Next(80, 160) : rng.Next(80, 181);
                    int gapX = theme == "challenge" ? rng.Next(60, 140) : rng.Next(50, MaxHorizontalGap + 1);

                    int platX = lastPlatformEndX + gapX;
                    if (platX + platW > levelWidth - 80)
                        break;

                    // Elevation -- capped to ensure reachability
                    int platY;
                    if (theme == "starter" || totalPlatforms <= 2)
                    {
                        platY = PlayerStartY;
                    }
                    else if (theme == "challenge")
                    {
                        if (p == 0)
                        {
                            // First challenge platform is still within single-jump range
                            platY = PlayerStartY - SafeElevation;
                        }
                        else
                        {
                            // Higher platforms need a stepping stone from the previous one
                            int prevPlatY = platforms.Last().Y;
                            int elevation = rng.Next(60, MaxPlatformElevation + 1);
                            // Clamp so the gap from previous platform is at most SafeElevation
                            if (prevPlatY - PlayerStartY > 0)
                                elevation = Math.Min(elevation, MaxPlatformElevation);
                            platY = PlayerStartY - elevation;
                        }
                    }
                    else if (theme == "rest")
                    {
                        // Rest platforms are low -- easy reach
                        platY = PlayerStartY - rng.Next(10, 50);
                    }
                    else
                    {
                        int elevation = rng.Next(10, SafeElevation + 1);
                        platY = PlayerStartY - elevation;
                    }

                    // Clamp: minimum Y value so it's reachable with double jump
                    int minPlatY = PlayerStartY - MaxPlatformElevation;
                    if (platY < minPlatY)
                        platY = minPlatY;

                    platforms.Add(new Platform { X = platX, Y = platY, W = platW, H = GroundHeight, Theme = theme });
                    lastPlatformEndX = platX + platW;

                    // Determine safe zone for coins and enemies (clear of traps)
                    int safeStart = platX + 10;
                    int safeEnd = platX + platW - 10;

                    // === TRAPS (spikes on platforms) ===
                    // Compute trap FIRST so coins and enemies avoid it
                    bool hasTrap = theme == "challenge" && platW >= 100 && rng.NextDouble() < 0.5;
                    int trapSpikeW = 0, trapSpikeX = 0;
                    bool trapLeftSide = false;

                    if (hasTrap)
                    {
                        int maxSpikeW = Math.Min(60, platW - 70);
                        if (maxSpikeW >= 20)
                        {
                            trapSpikeW = rng.Next(20, maxSpikeW + 1);
                            trapLeftSide = rng.NextDouble() < 0.5;
                            trapSpikeX = trapLeftSide ? platX + 10 : platX + platW - trapSpikeW - 10;
                            traps.Add(new TrapData { X = trapSpikeX, Y = platY - 8, W = trapSpikeW, H = 8 });

                            // Shrink safe zone away from trap side
                            if (trapLeftSide)
                                safeStart = trapSpikeX + trapSpikeW + 5;
                            else
                                safeEnd = trapSpikeX - 5;
                        }
                        else
                        {
                            hasTrap = false;
                        }
                    }

                    // === COINS ===
                    // Only in the safe zone (never on top of traps)
                    int coinsOnPlat = rng.Next(1, 4);
                    int coinArcHeight = Math.Min(60, platY - currentGroundY - 20);
                    if (coinArcHeight < 20) coinArcHeight = 20;

                    int coinRange = safeEnd - safeStart - 20;
                    if (coinRange > 0)
                    {
                        for (int c = 0; c < coinsOnPlat; c++)
                        {
                            coinIndex++;
                            int coinX = rng.Next(safeStart + 5, Math.Max(safeStart + 6, safeEnd - 5));
                            int coinY = platY - rng.Next(Math.Max(15, coinArcHeight - 15), coinArcHeight + 10);
                            coinY = Math.Max(coinY, platY - 80);
                            coins.Add(new Coin { X = coinX, Y = coinY, Id = $"coin_{coinIndex}" });
                        }
                    }

                    // Guide coins on the safe side when there's a trap (visual cue to land there)
                    if (hasTrap && (safeEnd - safeStart) >= 30)
                    {
                        int guideX = trapLeftSide ? safeEnd - 25 : safeStart + 5;
                        for (int tc = 0; tc < 2; tc++)
                        {
                            coinIndex++;
                            coins.Add(new Coin
                            {
                                X = guideX + tc * 15,
                                Y = platY - 30 - tc * 5,
                                Id = $"coin_{coinIndex}"
                            });
                        }
                    }

                    // === ENEMIES ===
                    // Only in the safe zone (never patrol into traps)
                    if ((theme == "normal" || theme == "challenge") && (safeEnd - safeStart) >= 50 && rng.NextDouble() < 0.5)
                    {
                        enemyIndex++;
                        int eW = 28, eH = 24;
                        int maxEnemyX = Math.Max(safeStart, safeEnd - eW);
                        int eX = rng.Next(safeStart, maxEnemyX + 1);
                        int eY = platY - eH;
                        double speed = 0.8 + rng.NextDouble() * 1.2;
                        int maxPatrol = Math.Max(30, Math.Min(safeEnd - safeStart - 20, 80));
                        int patrolRange = rng.Next(Math.Min(30, maxPatrol), maxPatrol + 1);

                        enemies.Add(new EnemyData
                        {
                            X = eX, Y = eY, W = eW, H = eH,
                            Speed = Math.Round(speed, 1),
                            PatrolStart = Math.Max(safeStart, eX - patrolRange / 2),
                            PatrolEnd = Math.Min(safeEnd, eX + patrolRange / 2)
                        });
                    }
                }

                // Clamp lastPlatformEndX so it doesn't bleed into the next chunk (which might be a gap)
                int chunkCeiling = (chunk + 1) * ChunkSize - 20;
                if (lastPlatformEndX > chunkCeiling)
                    lastPlatformEndX = chunkCeiling;
            }

            // === GROUND-LEVEL SPIKE GAUNTLET ===
            // RULE: only on full ground chunks, and only if there's an overhead platform nearby to escape onto
            if (theme == "challenge" && placeGroundInThisChunk && rng.NextDouble() < 0.3)
            {
                // Verify the chunk has an overhead platform the player can jump to
                bool hasOverhead = platforms.Any(p =>
                    p.X >= chunkStartX && p.X <= chunkStartX + ChunkSize &&
                    p.Y < currentGroundY - 40 && p.Theme != "ground" && p.Theme != "rest");

                if (hasOverhead)
                {
                    int maxRunW = Math.Min(200, ChunkSize - 80);
                    if (maxRunW >= 60)
                    {
                        int spikeRunW = rng.Next(60, maxRunW + 1);
                        int maxRunX = Math.Max(40, ChunkSize - spikeRunW - 20);
                        int spikeRunX = chunkStartX + rng.Next(Math.Min(40, maxRunX), maxRunX + 1);
                        traps.Add(new TrapData { X = spikeRunX, Y = currentGroundY - 8, W = spikeRunW, H = 8 });
                    }
                }
            }

            // === COINS ABOVE GAPS (guide the player) ===
            // Only when there IS ground in this chunk, place coins mid-air as guides
            if (placeGroundInThisChunk && rng.NextDouble() < 0.25)
            {
                int airCoins = rng.Next(1, 3);
                for (int ac = 0; ac < airCoins; ac++)
                {
                    coinIndex++;
                    coins.Add(new Coin
                    {
                        X = chunkStartX + ChunkSize / 2 + rng.Next(-80, 80),
                        Y = currentGroundY - rng.Next(60, 100),
                        Id = $"coin_{coinIndex}"
                    });
                }
            }
        }

        // === END ZONE ===
        // Ground under the end zone so the player doesn't fall into a hole
        int endZoneStartX = lastPlatformEndX + 60;
        int endZoneEndX = endZoneStartX + 45 * 2 + 80 + 120 + 60;
        platforms.Add(new Platform { X = endZoneStartX - 20, Y = GroundY, W = endZoneEndX - endZoneStartX + 40, H = GroundHeight, Theme = "ground" });

        // Gradual staircase, each step within single-jump range
        int endStepX = endZoneStartX;
        for (int step = 0; step < 3; step++)
        {
            int stepW = 80;
            int stepY = PlayerStartY - step * 30;
            stepY = Math.Max(stepY, PlayerStartY - SafeElevation);

            platforms.Add(new Platform { X = endStepX + step * 45, Y = stepY, W = stepW, H = GroundHeight, Theme = "end" });

            // Coins arc above each step
            for (int ec = 0; ec < 3; ec++)
            {
                coinIndex++;
                coins.Add(new Coin
                {
                    X = endStepX + step * 45 + 10 + ec * 20,
                    Y = stepY - 25 - ec * 8,
                    Id = $"coin_{coinIndex}"
                });
            }
        }

        int finalPlatX = endStepX + 2 * 45 + 80;
        int finalPlatY = Math.Max(PlayerStartY - SafeElevation, PlayerStartY - 60);
        platforms.Add(new Platform { X = finalPlatX, Y = finalPlatY, W = 120, H = GroundHeight, Theme = "end" });
        int endX = finalPlatX + 60;
        int endY = finalPlatY;

        return new LevelData
        {
            Seed = seed,
            Width = levelWidth,
            Height = height,
            StartX = startX,
            StartY = PlayerStartY,
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
        else if (roll < 0.50)
            return "challenge";
        else
            return "rest";
    }
}