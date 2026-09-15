namespace RobotArm.Api.Models;

public class Robot
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relatie met User
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // Relatie met RobotCommand
    public List<RobotCommand> Commands { get; set; } = new();
}