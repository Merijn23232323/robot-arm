using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RobotArm.Api.Data;
using RobotArm.Api.Models;
using RobotArm.Api.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

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
    [EndpointSummary("Registreer een gebruiker")]
    [EndpointDescription("Maakt een gebruiker aan en slaat het wachtwoord veilig gehasht op.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Gebruikersnaam, e-mailadres en wachtwoord zijn verplicht."
            });
        }

        if (await _context.Users.AnyAsync(user => user.Username == username))
        {
            return Conflict(new
            {
                message = "Deze gebruikersnaam is al in gebruik."
            });
        }

        if (await _context.Users.AnyAsync(user => user.Email == email))
        {
            return Conflict(new
            {
                message = "Dit e-mailadres is al geregistreerd."
            });
        }

        var user = new User
        {
            Username = username,
            Email = email
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

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
                user.CreatedAt
            }
        });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [EndpointSummary("Login")]
    [EndpointDescription("Logt een gebruiker in en geeft een JWT-token terug.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Email en wachtwoord zijn verplicht."
            });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Email of wachtwoord is onjuist."
            });
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Email of wachtwoord is onjuist."
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
    [EndpointSummary("Vernieuw een JWT-token")]
    [EndpointDescription("Vervangt een geldig refresh token door een nieuw JWT-token en refresh token.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "Refresh token is verplicht." });
        }

        var refreshTokenHash = HashRefreshToken(request.RefreshToken);
        var user = await _context.Users
            .SingleOrDefaultAsync(candidate => candidate.RefreshTokenHash == refreshTokenHash);

        if (user == null || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Refresh token is ongeldig of verlopen." });
        }

        var (token, refreshToken) = CreateTokenPair(user);
        user.RefreshTokenHash = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return Ok(new { token, refreshToken });
    }

    [Authorize]
    [HttpGet("me")]
    [EndpointSummary("Haal de ingelogde gebruiker op")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users.FindAsync(int.Parse(userId!));

        return user == null
            ? Unauthorized()
            : Ok(new { user.Id, user.Username, user.Email });
    }

    private (string Token, string RefreshToken) CreateTokenPair(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
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

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return (accessToken, refreshToken);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}