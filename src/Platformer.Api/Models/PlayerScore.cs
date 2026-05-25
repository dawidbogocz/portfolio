namespace Platformer.Api.Models;

public class PlayerScore
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Seed { get; set; }
    public DateTime DateAchieved { get; set; }
}