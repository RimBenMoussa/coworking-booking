using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingBooking.Api.Controllers;

[ApiController]
[Route("api/spaces")]
public class SpacesController(CatalogService catalog) : ControllerBase
{
    /// <summary>Liste des espaces avec leurs tarifs, et le prix de l'abonnement.</summary>
    [HttpGet]
    public Task<SpacesResponse> GetSpaces() => catalog.GetSpacesAsync();

    /// <summary>Places restantes (postes partagés) ou créneaux occupés de la journée (cabines, salles).</summary>
    /// <param name="id">Identifiant de l'espace.</param>
    /// <param name="date">Date locale, ex. 2026-11-02.</param>
    /// <param name="time">Heure locale d'arrivée, ex. 09:30.</param>
    /// <param name="durationHours">Durée prévue en heures (cabines et salles).</param>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityDto>> GetAvailability(
        int id, [FromQuery] DateOnly date, [FromQuery] string time, [FromQuery] int? durationHours)
    {
        if (!TimeOnly.TryParseExact(time, "HH:mm", out var parsed))
            throw BusinessException.BadRequest("L'heure doit être au format HH:mm (ex. 09:30).");
        return await catalog.GetAvailabilityAsync(id, date, parsed, durationHours);
    }
}
