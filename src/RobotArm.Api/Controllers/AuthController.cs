using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RobotArm.Api.Data;
using RobotArm.Api.Models;
using RobotArm.Api.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Net.Mail;

namespace RobotArm.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(
        AppDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [EndpointSummary("Registreer een gebruiker")]
    [EndpointDescription("Maakt een nieuwe gebruiker aan en slaat het wachtwoord veilig gehasht op.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var username = request.Username?.Trim() ?? "";
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var password = request.Password ?? "";

        // Verplichte velden controleren
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new
            {
                message = "Validatie mislukt.",
                errors = new[]
                {
                    "Gebruikersnaam, e-mailadres en wachtwoord zijn verplicht."
                }
            });
        }

        // Gebruikersnaam controleren
        if (username.Length < 3)
        {
            return BadRequest(new
            {
                message = "Validatie mislukt.",
                errors = new[]
                {
                    "Gebruikersnaam moet minimaal 3 tekens bevatten."
                }
            });
        }

        // E-mailadres controleren
        if (!IsValidEmail(email))
        {
            return BadRequest(new
            {
                message = "Validatie mislukt.",
                errors = new[]
                {
                    "Vul een geldig e-mailadres in."
                }
            });
        }

        // Wachtwoord controleren
        if (password.Length < 8)
        {
            return BadRequest(new
            {
                message = "Validatie mislukt.",
                errors = new[]
                {
                    "Wachtwoord moet minimaal 8 tekens bevatten."
                }
            });
        }

        // Controleren of gebruikersnaam al bestaat
        if (await _context.Users.AnyAsync(user => user.Username == username))
        {
            return Conflict(new
            {
                message = "Registratie mislukt.",
                errors = new[]
                {
                    "Deze gebruikersnaam is al in gebruik."
                }
            });
        }

        // Controleren of e-mailadres al bestaat
        if (await _context.Users.AnyAsync(user => user.Email == email))
        {
            return Conflict(new
            {
                message = "Registratie mislukt.",
                errors = new[]
                {
                    "Dit e-mailadres is al geregistreerd."
                }
            });
        }

        var user = new User
        {
            Username = username,
            Email = email,
            Role = "User"
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(user, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Registratie succesvol.",
            user = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Role,
                user.CreatedAt
            }
        });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [EndpointSummary("Login")]
    [EndpointDescription("Controleert de inloggegevens en geeft bij een succesvolle login een JWT-token en refresh token terug.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var password = request.Password ?? "";

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new
            {
                message = "Validatie mislukt.",
                errors = new[]
                {
                    "E-mailadres en wachtwoord zijn verplicht."
                }
            });
        }

        if (!IsValidEmail(email))
        {
            return BadRequest(new
            {
                message = "Validatie mislukt.",
                errors = new[]
                {
                    "Vul een geldig e-mailadres in."
                }
            });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        // Zelfde foutmelding bij verkeerde email en verkeerd wachtwoord.
        // Zo geven we niet prijs of een account bestaat.
        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Inloggen mislukt.",
                errors = new[]
                {
                    "E-mailadres of wachtwoord is onjuist."
                }
            });
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Inloggen mislukt.",
                errors = new[]
                {
                    "E-mailadres of wachtwoord is onjuist."
                }
            });
        }

        var (token, refreshToken) = CreateTokenPair(user);

        user.RefreshTokenHash = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Login succesvol.",
            token,
            refreshToken
        });
    }

    // POST: api/auth/refresh
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [EndpointSummary("Vernieuw een JWT-token")]
    [EndpointDescription("Vervangt een geldig refresh token door een nieuw JWT-token en refresh token.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new
            {
                message = "Validatie mislukt.",
                errors = new[]
                {
                    "Refresh token is verplicht."
                }
            });
        }

        var refreshTokenHash =
            HashRefreshToken(request.RefreshToken);

        var user = await _context.Users
            .SingleOrDefaultAsync(candidate =>
                candidate.RefreshTokenHash == refreshTokenHash);

        if (user == null ||
            user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                message = "Token vernieuwen mislukt.",
                errors = new[]
                {
                    "Refresh token is ongeldig of verlopen."
                }
            });
        }

        var (token, refreshToken) = CreateTokenPair(user);

        user.RefreshTokenHash = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Token succesvol vernieuwd.",
            token,
            refreshToken
        });
    }

    // GET: api/auth/me
    [Authorize]
    [HttpGet("me")]
    [EndpointSummary("Haal de ingelogde gebruiker op")]
    [EndpointDescription("Geeft de gegevens van de momenteel ingelogde gebruiker terug.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var id))
        {
            return Unauthorized(new
            {
                message = "Authenticatie mislukt.",
                errors = new[]
                {
                    "Gebruiker kon niet worden bepaald."
                }
            });
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Authenticatie mislukt.",
                errors = new[]
                {
                    "Gebruiker is niet gevonden."
                }
            });
        }

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.Role
        });
    }

    private (string Token, string RefreshToken) CreateTokenPair(User user)
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Username),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            // Nodig voor [Authorize(Roles = "Admin")]
            new Claim(
                ClaimTypes.Role,
                user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!
            )
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        return (accessToken, refreshToken);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);

            return address.Address.Equals(
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}