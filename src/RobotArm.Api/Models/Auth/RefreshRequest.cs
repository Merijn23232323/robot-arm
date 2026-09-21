using System.ComponentModel.DataAnnotations;

namespace RobotArm.Api.Models.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
