namespace CoworkingBooking.Api.Services;

// Erreur métier transformée en réponse ProblemDetails par ProblemDetailsExceptionHandler.
public class BusinessException(int statusCode, string title, string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;

    public static BusinessException BadRequest(string detail) =>
        new(StatusCodes.Status400BadRequest, "Requête invalide", detail);

    public static BusinessException NotFound(string detail) =>
        new(StatusCodes.Status404NotFound, "Introuvable", detail);

    public static BusinessException Conflict(string detail) =>
        new(StatusCodes.Status409Conflict, "Conflit", detail);
}
