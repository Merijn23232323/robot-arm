using System.ComponentModel.DataAnnotations;

namespace RobotArm.Api.Models;

public class Robot
{
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public bool IsOnline { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relatie met User
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // Relatie met RobotCommand
    public List<RobotCommand> Commands { get; set; } = new();
}