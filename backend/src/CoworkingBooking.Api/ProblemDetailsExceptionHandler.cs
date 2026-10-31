using CoworkingBooking.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingBooking.Api;

// Transforme les BusinessException en réponses ProblemDetails (400, 401, 404, 409).
// Les autres exceptions deviennent une erreur 500 générique, sans détail technique pour le client.
public class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetails, ILogger<ProblemDetailsExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;
        if (exception is BusinessException business)
        {
            problem = new ProblemDetails { Status = business.StatusCode, Title = business.Title, Detail = business.Message };
        }
        else
        {
            logger.LogError(exception, "Erreur non gérée sur {Path}", context.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erreur interne",
                Detail = "Une erreur inattendue est survenue. Réessayez dans un instant."
            };
        }

        context.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
