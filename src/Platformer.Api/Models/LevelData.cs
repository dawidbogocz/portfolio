namespace Platformer.Api.Models;

public class LevelData
{
    public int Seed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int StartX { get; set; }
    public int StartY { get; set; }
    public List<Platform> Platforms { get; set; } = new();
    public List<Coin> Coins { get; set; } = new();
    public List<EnemyData> Enemies { get; set; } = new();
    public List<TrapData> Traps { get; set; } = new();
    public int EndX { get; set; }
    public int EndY { get; set; }
}

public class Platform
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
    public string? Theme { get; set; }
}

public class Coin
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Id { get; set; } = string.Empty;
}

public class EnemyData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
    public double Speed { get; set; }
    public int PatrolStart { get; set; }
    public int PatrolEnd { get; set; }
}

public class TrapData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; }
    public int H { get; set; }
}