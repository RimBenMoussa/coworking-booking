using CoworkingBooking.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Data;

// Crée le compte gestionnaire au démarrage à partir de Admin__Email et Admin__Password.
// Si le compte existe déjà, son mot de passe est remis à la valeur de la configuration :
// changer le secret puis redémarrer suffit à changer le mot de passe.
public static class AdminSeeder
{
    public static async Task EnsureAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
        var email = config["Admin:Email"]?.Trim().ToLowerInvariant();
        var password = config["Admin:Password"];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning("Admin__Email ou Admin__Password non configuré : aucun compte gestionnaire créé.");
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();

        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Email == email);
        if (admin is null)
        {
            admin = new AdminUser { Email = email };
            db.AdminUsers.Add(admin);
            logger.LogInformation("Compte gestionnaire créé : {Email}", email);
        }
        else if (hasher.VerifyHashedPassword(admin, admin.PasswordHash, password) != PasswordVerificationResult.Failed)
        {
            return;
        }

        admin.PasswordHash = hasher.HashPassword(admin, password);
        await db.SaveChangesAsync();
    }
}
