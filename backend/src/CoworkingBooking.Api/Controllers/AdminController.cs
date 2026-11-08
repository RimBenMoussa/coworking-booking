using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using CoworkingBooking.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingBooking.Api.Controllers;

// Toutes les routes du gestionnaire : un jeton JWT valide est obligatoire (sinon 401).
[ApiController]
[Route("api/admin")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class AdminController(
    FrontDeskService frontDesk,
    CatalogService catalog,
    SubscriptionService subscriptions,
    ContactService contact) : ControllerBase
{
    /// <summary>Réservations d'une journée (par défaut aujourd'hui), filtrables par statut.</summary>
    [HttpGet("bookings")]
    public async Task<List<BookingDto>> GetBookings([FromQuery] DateOnly? date, [FromQuery] BookingStatus? status) =>
        (await frontDesk.ListAsync(date, status)).Select(BookingDto.From).ToList();

    /// <summary>Occupation actuelle de chaque espace.</summary>
    [HttpGet("occupancy")]
    public Task<List<OccupancyDto>> GetOccupancy() => frontDesk.OccupancyAsync();

    [HttpPost("bookings/{code}/check-in")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<BookingDto> CheckIn(string code) => BookingDto.From(await frontDesk.CheckInAsync(code));

    /// <summary>Durée et prix à encaisser si le client part maintenant (rien n'est enregistré).</summary>
    [HttpGet("bookings/{code}/check-out")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<CheckOutDto> PreviewCheckOut(string code) => frontDesk.PreviewCheckOutAsync(code);

    /// <summary>Enregistre le départ : calcule et enregistre le prix et le moyen de paiement.</summary>
    [HttpPost("bookings/{code}/check-out")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<CheckOutDto> CheckOut(string code, CheckOutRequest request) =>
        frontDesk.CheckOutAsync(code, request.PaymentMethod!.Value);

    [HttpPost("quotes/{code}/accept")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<BookingDto> AcceptQuote(string code, AcceptQuoteRequest request) =>
        BookingDto.From(await frontDesk.AcceptQuoteAsync(code, request.Amount!.Value, request.Note));

    [HttpPost("quotes/{code}/reject")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<BookingDto> RejectQuote(string code, RejectQuoteRequest request) =>
        BookingDto.From(await frontDesk.RejectQuoteAsync(code, request.Note));

    [HttpGet("prices")]
    public Task<List<TariffDto>> GetPrices() => catalog.GetPricesAsync();

    [HttpPut("prices")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<List<TariffDto>> UpdatePrices(List<UpdatePriceRequest> request) => catalog.UpdatePricesAsync(request);

    [HttpGet("subscriptions")]
    public Task<List<SubscriptionDto>> GetSubscriptions([FromQuery] SubscriptionStatus? status) =>
        subscriptions.ListAsync(status);

    /// <summary>Active un abonnement après encaissement. Validité : un mois.</summary>
    [HttpPost("subscriptions/{id:int}/activate")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateSubscription(int id)
    {
        var s = await subscriptions.ActivateAsync(id);
        return Ok(new { s.Id, s.StartDate, s.EndDate, s.Status });
    }

    [HttpGet("messages")]
    public Task<List<ContactMessageDto>> GetMessages() => contact.ListAsync();
}
