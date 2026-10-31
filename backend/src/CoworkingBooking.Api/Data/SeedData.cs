using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Data;

// Espaces et tarifs de départ, insérés par la migration initiale.
// Les tarifs restent modifiables ensuite par le gestionnaire (PUT /api/admin/prices).
public static class SeedData
{
    public const int OpenSpaceId = 1;
    public const int TerrasseId = 2;
    public const int PhoneBoxSolo1Id = 3;
    public const int PhoneBoxSolo2Id = 4;
    public const int PhoneBoxDuoId = 5;
    public const int SalleSparkId = 6;
    public const int SalleNexusId = 7;
    public const int SalleSummitId = 8;

    public static readonly Space[] Spaces =
    [
        new() { Id = OpenSpaceId, Name = "Open Space", Type = SpaceType.PostePartage, Capacity = 17, SortOrder = 1,
                Description = "Grand espace de travail partagé, calme et lumineux." },
        new() { Id = TerrasseId, Name = "Terrasse panoramique", Type = SpaceType.PostePartage, Capacity = 8, SortOrder = 2,
                Description = "Postes de travail en plein air avec vue sur la ville." },
        new() { Id = PhoneBoxSolo1Id, Name = "Phone box Solo 1", Type = SpaceType.Cabine, Capacity = 1, SortOrder = 3,
                Description = "Cabine insonorisée pour un appel ou une visio." },
        new() { Id = PhoneBoxSolo2Id, Name = "Phone box Solo 2", Type = SpaceType.Cabine, Capacity = 1, SortOrder = 4,
                Description = "Cabine insonorisée pour un appel ou une visio." },
        new() { Id = PhoneBoxDuoId, Name = "Phone box Duo", Type = SpaceType.Cabine, Capacity = 2, SortOrder = 5,
                Description = "Cabine pour deux personnes, idéale pour un entretien." },
        new() { Id = SalleSparkId, Name = "Salle Spark", Type = SpaceType.SallePrivee, Capacity = 4, SortOrder = 6,
                Description = "Petite salle de réunion privée." },
        new() { Id = SalleNexusId, Name = "Salle Nexus", Type = SpaceType.SallePrivee, Capacity = 8, SortOrder = 7,
                Description = "Salle de réunion équipée d'un écran." },
        new() { Id = SalleSummitId, Name = "Salle Summit", Type = SpaceType.SallePrivee, Capacity = 12, SortOrder = 8,
                Description = "Grande salle pour ateliers et formations." },
    ];

    public static readonly Tariff[] Tariffs =
    [
        new() { Id = 1, SpaceId = OpenSpaceId, Type = TariffType.Court, Amount = 25 },
        new() { Id = 2, SpaceId = OpenSpaceId, Type = TariffType.Journee, Amount = 40 },
        new() { Id = 3, SpaceId = OpenSpaceId, Type = TariffType.Nuit, Amount = 40 },
        new() { Id = 4, SpaceId = TerrasseId, Type = TariffType.Court, Amount = 30 },
        new() { Id = 5, SpaceId = TerrasseId, Type = TariffType.Journee, Amount = 50 },
        new() { Id = 6, SpaceId = TerrasseId, Type = TariffType.Nuit, Amount = 50 },
        new() { Id = 7, SpaceId = PhoneBoxSolo1Id, Type = TariffType.Heure, Amount = 10 },
        new() { Id = 8, SpaceId = PhoneBoxSolo2Id, Type = TariffType.Heure, Amount = 10 },
        new() { Id = 9, SpaceId = PhoneBoxDuoId, Type = TariffType.HeureParPersonne, Amount = 15 },
        new() { Id = 10, SpaceId = null, Type = TariffType.AbonnementMensuel, Amount = 800 },
    ];

    public static void Apply(ModelBuilder modelBuilder)
    {
        // HasData n'accepte pas les propriétés de navigation remplies : on copie les valeurs simples.
        modelBuilder.Entity<Space>().HasData(Spaces.Select(s => new Space
        {
            Id = s.Id, Name = s.Name, Type = s.Type, Capacity = s.Capacity,
            Description = s.Description, SortOrder = s.SortOrder
        }));
        modelBuilder.Entity<Tariff>().HasData(Tariffs.Select(t => new Tariff
        {
            Id = t.Id, SpaceId = t.SpaceId, Type = t.Type, Amount = t.Amount
        }));
    }
}
