namespace CoworkingBooking.Api.Models;

// Compte gestionnaire. Créé au démarrage à partir de la configuration (Admin__Email, Admin__Password).
public class AdminUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}
