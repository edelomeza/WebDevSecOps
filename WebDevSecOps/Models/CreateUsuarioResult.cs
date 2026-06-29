namespace WebDevSecOps.Models;

public class CreateUsuarioResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public Dictionary<string, string[]>? FieldErrors { get; }

    private CreateUsuarioResult(bool success, string? errorMessage, Dictionary<string, string[]>? fieldErrors)
    {
        Success = success;
        ErrorMessage = errorMessage;
        FieldErrors = fieldErrors;
    }

    public static CreateUsuarioResult Ok() => new(true, null, null);

    public static CreateUsuarioResult Fail(string message) => new(false, message, null);

    public static CreateUsuarioResult Fail(Dictionary<string, string[]> fieldErrors, string? message = null)
        => new(false, message, fieldErrors);
}
