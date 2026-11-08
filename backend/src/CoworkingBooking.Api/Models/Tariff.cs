namespace CoworkingBooking.Api.Models;

// Un tarif en dirhams. Les salles privées n'ont pas de tarif : elles sont sur devis.
// L'abonnement mensuel est un tarif sans espace (SpaceId = null).
public class Tariff
{
    public int Id { get; set; }
    public int? SpaceId { get; set; }
    public Space? Space { get; set; }
    public TariffType Type { get; set; }
    public decimal Amount { get; set; }
}
