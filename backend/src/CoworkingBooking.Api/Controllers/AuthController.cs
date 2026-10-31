using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingBooking.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Connexion du gestionnaire. Renvoie un jeton JWT à envoyer dans l'en-tête Authorization.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<LoginResponse> Login(LoginRequest request) => auth.LoginAsync(request.Email, request.Password);
}
