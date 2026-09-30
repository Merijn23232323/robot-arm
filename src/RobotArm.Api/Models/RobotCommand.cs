using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace RobotArm.Api.Models;

public class RobotCommand
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int RobotId { get; set; }

    [ValidateNever]
    public Robot Robot { get; set; } = null!;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Servo { get; set; } = string.Empty;

    [Range(0, 180)]
    public int Angle { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}   