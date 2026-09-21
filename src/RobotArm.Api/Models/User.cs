namespace RobotArm.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? RefreshTokenHash { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }

    // Rol voor autorisatie
    public string Role { get; set; } = "User";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Robot> Robots { get; set; } = new();
}