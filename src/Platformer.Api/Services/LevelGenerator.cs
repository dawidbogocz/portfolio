using Platformer.Api.Models;

namespace Platformer.Api.Services;

public class LevelGenerator
{
    private const int GroundHeight = 40;
    private const int PlayerJumpHeight = 100;
    private const int MaxHorizontalGap = 150;

    public LevelData GenerateLevel(int seed, int width = 800, int height = 450)
    {
        var rng = new Random(seed);
        int levelWidth = rng.Next(3000, 5001);
        int groundY = height - GroundHeight;

        var platforms = new List<Platform>();

        // Ground platform (full width)
        platforms.Add(new Platform { X = 0, Y = groundY, W = levelWidth, H = GroundHeight });

        // Start position: left side on the ground
        int startX = 50;
        int startY = groundY - 40; // 40px is player height approx

        // Generate procedural platforms
        int platformCount = rng.Next(8, 16);
        int lastX = startX + 80; // first platform starts after start position

        for (int i = 0; i < platformCount; i++)
        {
            int platW = rng.Next(60, 151);
            int gapX = rng.Next(60, MaxHorizontalGap + 1);

            int platX = lastX + gapX;

            // Ensure we don't go beyond level width
            if (platX + platW > levelWidth - 100)
                break;

            // Vary height: ground level or elevated platforms (staggered)
            int platY;
            if (i == 0 || i == platformCount - 1)
            {
                // First and last platforms at ground level
                platY = groundY - GroundHeight; // top of ground
            }
            else
            {
                // Elevate between 20 and PlayerJumpHeight pixels above ground
                int elevation = rng.Next(20, PlayerJumpHeight + 1);
                platY = groundY - elevation;
            }

            platforms.Add(new Platform { X = platX, Y = platY, W = platW, H = GroundHeight });
            lastX = platX + platW;
        }

        // End position: on the last platform
        var lastPlatform = platforms[^1];
        int endX = lastPlatform.X + lastPlatform.W / 2;
        int endY = lastPlatform.Y - 5; // a bit above the platform surface

        // Place coins on or near platforms
        var coins = new List<Coin>();
        int coinCount = rng.Next(10, 21);
        int coinIndex = 0;

        for (int i = 0; i < coinCount; i++)
        {
            // Pick a random platform (skip ground = index 0)
            int platIdx = rng.Next(1, platforms.Count);
            var plat = platforms[platIdx];

            int coinX = rng.Next(plat.X + 10, plat.X + plat.W - 10);
            int coinY = plat.Y - rng.Next(20, 60); // above platform

            coinIndex++;
            coins.Add(new Coin
            {
                X = coinX,
                Y = coinY,
                Id = $"coin_{coinIndex}"
            });
        }

        return new LevelData
        {
            Seed = seed,
            Width = levelWidth,
            Height = height,
            StartX = startX,
            StartY = startY,
            Platforms = platforms,
            Coins = coins,
            EndX = endX,
            EndY = endY
        };
    }
}