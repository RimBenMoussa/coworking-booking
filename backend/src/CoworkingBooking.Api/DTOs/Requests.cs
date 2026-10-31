using System.ComponentModel.DataAnnotations;
using CoworkingBooking.Api.Models;

namespace CoworkingBooking.Api.DTOs;

// Les erreurs de validation de ces attributs sont renvoyées automatiquement en 400 (ValidationProblemDetails).

public record CreateBookingRequest
{
    [Required(ErrorMessage = "L'espace est obligatoire.")]
    public int? SpaceId { get; init; }

    [Required(ErrorMessage = "Le nom complet est obligatoire.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Le nom complet doit contenir entre 3 et 100 caractères.")]
    public string FullName { get; init; } = "";

    [Required(ErrorMessage = "Le téléphone est obligatoire.")]
    public string Phone { get; init; } = "";

    [Required(ErrorMessage = "La date d'arrivée est obligatoire.")]
    public DateOnly? Date { get; init; }

    [Required(ErrorMessage = "L'heure d'arrivée est obligatoire.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "L'heure doit être au format HH:mm (ex. 09:30).")]
    public string Time { get; init; } = "";

    // Cabines et salles uniquement.
    [Range(1, 20, ErrorMessage = "Le nombre de personnes doit être compris entre 1 et 20.")]
    public int? Persons { get; init; }

    // Cabines et salles uniquement, en heures.
    [Range(1, 12, ErrorMessage = "La durée doit être comprise entre 1 et 12 heures.")]
    public int? DurationHours { get; init; }
}

public record CancelBookingRequest
{
    [Required(ErrorMessage = "Le téléphone est obligatoire.")]
    public string Phone { get; init; } = "";
}

public record CreateSubscriptionRequest
{
    [Required(ErrorMessage = "Le nom complet est obligatoire.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Le nom complet doit contenir entre 3 et 100 caractères.")]
    public string FullName { get; init; } = "";

    [Required(ErrorMessage = "Le téléphone est obligatoire.")]
    public string Phone { get; init; } = "";

    [Required(ErrorMessage = "L'email est obligatoire.")]
    [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide.")]
    [StringLength(200, ErrorMessage = "L'email est trop long.")]
    public string Email { get; init; } = "";

    [Required(ErrorMessage = "La date de début est obligatoire.")]
    public DateOnly? StartDate { get; init; }
}

public record CreateContactMessageRequest
{
    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères.")]
    public string Name { get; init; } = "";

    [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide.")]
    [StringLength(200, ErrorMessage = "L'email est trop long.")]
    public string? Email { get; init; }

    public string? Phone { get; init; }

    [Required(ErrorMessage = "Le message est obligatoire.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "Le message doit contenir entre 5 et 2000 caractères.")]
    public string Message { get; init; } = "";
}

public record LoginRequest
{
    [Required(ErrorMessage = "L'email est obligatoire.")]
    public string Email { get; init; } = "";

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    public string Password { get; init; } = "";
}

public record CheckOutRequest
{
    [Required(ErrorMessage = "Le moyen de paiement est obligatoire (Especes ou Carte).")]
    public PaymentMethod? PaymentMethod { get; init; }
}

public record AcceptQuoteRequest
{
    [Required(ErrorMessage = "Le montant du devis est obligatoire.")]
    [Range(1, 100000, ErrorMessage = "Le montant du devis doit être compris entre 1 et 100 000 DH.")]
    public decimal? Amount { get; init; }

    [StringLength(500, ErrorMessage = "La note est trop longue (500 caractères maximum).")]
    public string? Note { get; init; }
}

public record RejectQuoteRequest
{
    [StringLength(500, ErrorMessage = "La note est trop longue (500 caractères maximum).")]
    public string? Note { get; init; }
}

public record UpdatePriceRequest
{
    [Required(ErrorMessage = "L'identifiant du tarif est obligatoire.")]
    public int? Id { get; init; }

    [Required(ErrorMessage = "Le montant est obligatoire.")]
    [Range(0, 100000, ErrorMessage = "Le montant doit être compris entre 0 et 100 000 DH.")]
    public decimal? Amount { get; init; }
}
