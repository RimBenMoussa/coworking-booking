namespace CoworkingBooking.Api.Models;

public class Space
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public SpaceType Type { get; set; }

    // Postes partagés : nombre de places. Cabines et salles : nombre maximum de personnes.
    public int Capacity { get; set; }

    public string Description { get; set; } = "";
    public int SortOrder { get; set; }

    public List<Tariff> Tariffs { get; set; } = [];
}
