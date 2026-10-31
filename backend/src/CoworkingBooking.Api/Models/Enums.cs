namespace CoworkingBooking.Api.Models;

// Les valeurs sont écrites sans accents (identifiants C#) et stockées en texte
// dans PostgreSQL. Le frontend affiche les libellés accentués.

public enum SpaceType
{
    PostePartage, // Open Space, Terrasse : plusieurs personnes en même temps
    Cabine,       // Phone box : un seul créneau à la fois
    SallePrivee   // Salles de réunion : sur devis
}

public enum TariffType
{
    Court,             // poste partagé, moins de 6 h
    Journee,           // poste partagé, 6 h ou plus
    Nuit,              // poste partagé, arrivée entre 20:00 et 05:59
    Heure,             // cabine, par heure entamée
    HeureParPersonne,  // cabine Duo, par heure entamée et par personne
    AbonnementMensuel  // prix de l'abonnement (pas lié à un espace)
}

public enum BookingStatus
{
    Confirmee,
    DevisDemande,
    Arrivee,
    Terminee,
    Annulee,
    NonPresente,
    Refusee
}

public enum SubscriptionStatus
{
    EnAttenteDePaiement,
    Active
}

public enum PaymentMethod
{
    Especes,
    Carte
}
