using System.ComponentModel.DataAnnotations;

namespace RobotArm.Api.Models;

public class RobotCommand
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int RobotId { get; set; }

    public Robot Robot { get; set; } = null!;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Servo { get; set; } = string.Empty;

    [Range(0, 180)]
    public int Angle { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}   