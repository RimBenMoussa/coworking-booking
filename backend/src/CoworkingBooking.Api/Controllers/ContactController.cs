using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingBooking.Api.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController(ContactService contact) : ControllerBase
{
    /// <summary>Enregistre un message du formulaire de contact.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateContactMessageRequest request)
    {
        var message = await contact.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, new { message.Id });
    }
}
