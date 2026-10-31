using CoworkingBooking.Api.Models;

namespace CoworkingBooking.Api.Services;

// Constantes métier regroupées à un seul endroit.
public static class BookingRules
{
    // Fenêtre d'occupation estimée d'un poste partagé après l'arrivée prévue.
    public static readonly TimeSpan SharedDeskWindow = TimeSpan.FromHours(6);

    // La place est gardée 15 minutes après l'heure d'arrivée prévue.
    public static readonly TimeSpan HoldDelay = TimeSpan.FromMinutes(15);

    // Check-in accepté jusqu'à 30 minutes avant l'heure prévue.
    public static readonly TimeSpan EarlyCheckIn = TimeSpan.FromMinutes(30);

    // Un client arrivé qui dépasse son heure de fin prévue est considéré présent encore 1 h
    // (on ne sait pas quand il partira, mais il occupe sa place maintenant).
    public static readonly TimeSpan OverstayMargin = TimeSpan.FromHours(1);

    // Seuil entre tarif court et tarif journée.
    public static readonly TimeSpan DayThreshold = TimeSpan.FromHours(6);

    public const int MaxCabinHours = 8;
    public const int MaxRoomHours = 12;
    public const int MaxDaysAhead = 60;

    // Réservations qui occupent réellement une place.
    public static readonly BookingStatus[] ActiveStatuses = [BookingStatus.Confirmee, BookingStatus.Arrivee];

    // Statuts qu'un client peut encore annuler.
    public static readonly BookingStatus[] CancellableStatuses = [BookingStatus.Confirmee, BookingStatus.DevisDemande];

    // Libellé français d'un statut, pour les messages d'erreur.
    public static string Label(BookingStatus status) => status switch
    {
        BookingStatus.Confirmee => "Confirmée",
        BookingStatus.DevisDemande => "Devis demandé",
        BookingStatus.Arrivee => "Arrivée",
        BookingStatus.Terminee => "Terminée",
        BookingStatus.Annulee => "Annulée",
        BookingStatus.NonPresente => "Non présenté",
        BookingStatus.Refusee => "Refusée",
        _ => status.ToString()
    };
}
