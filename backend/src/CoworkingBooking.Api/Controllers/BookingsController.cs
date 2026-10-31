using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(BookingService bookings) : ControllerBase
{
    /// <summary>Crée une réservation (ou une demande de devis pour une salle) et renvoie son code.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest request)
    {
        var booking = await bookings.CreateAsync(request);
        var dto = BookingDto.From(booking);
        return CreatedAtAction(nameof(Get), new { code = dto.Code, phone = dto.Phone }, dto);
    }

    /// <summary>Consulte une réservation avec son code et le téléphone utilisé.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<BookingDto> Get(string code, [FromQuery] string? phone) =>
        BookingDto.From(await bookings.GetAsync(code, phone));

    /// <summary>Annule gratuitement une réservation.</summary>
    [HttpPost("{code}/cancel")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<BookingDto> Cancel(string code, CancelBookingRequest request) =>
        BookingDto.From(await bookings.CancelAsync(code, request.Phone));
}
