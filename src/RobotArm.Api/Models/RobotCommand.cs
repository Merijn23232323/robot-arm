namespace RobotArm.Api.Models;

public class RobotCommand
{
    public int Id { get; set; }

    public int RobotId { get; set; }

    public Robot Robot { get; set; } = null!;

    public string Servo { get; set; } = string.Empty;

    public int Angle { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}   