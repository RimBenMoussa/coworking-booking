using System.Security.Claims;
using System.Text;
using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CoworkingBooking.Api.Services;

// Section "Jwt" de la configuration (Jwt__Key, Jwt__Issuer...).
public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "coworking-booking";
    public string Audience { get; set; } = "coworking-booking";
    public int ExpiresHours { get; set; } = 12;

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));
}

public class AuthService(
    AppDbContext db,
    IPasswordHasher<AdminUser> hasher,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider clock,
    ILogger<AuthService> logger)
{
    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Email == normalized);

        if (admin is null || hasher.VerifyHashedPassword(admin, admin.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Échec de connexion gestionnaire pour {Email}", normalized);
            throw new BusinessException(StatusCodes.Status401Unauthorized, "Non autorisé", "Email ou mot de passe incorrect.");
        }

        var options = jwtOptions.Value;
        var expiresAt = clock.GetUtcNow().AddHours(options.ExpiresHours);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, admin.Email),
                new Claim(ClaimTypes.Role, "Gestionnaire")
            ]),
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(options.SigningKey(), SecurityAlgorithms.HmacSha256)
        });

        logger.LogInformation("Connexion gestionnaire {Email}", admin.Email);
        return new LoginResponse(token, expiresAt, admin.Email);
    }
}
