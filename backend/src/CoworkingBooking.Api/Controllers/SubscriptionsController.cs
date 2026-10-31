using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingBooking.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController(SubscriptionService subscriptions) : ControllerBase
{
    /// <summary>Demande d'abonnement mensuel. Elle reste en attente jusqu'au paiement à l'accueil.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateSubscriptionRequest request)
    {
        var s = await subscriptions.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, new
        {
            s.Id, s.FullName, s.StartDate, s.EndDate, s.Status, s.Price
        });
    }
}
