namespace WebDevSecOps.Models;

public class OperationResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public Dictionary<string, string[]>? FieldErrors { get; }

    private OperationResult(bool success, string? errorMessage, Dictionary<string, string[]>? fieldErrors)
    {
        Success = success;
        ErrorMessage = errorMessage;
        FieldErrors = fieldErrors;
    }

    public static OperationResult Ok() => new(true, null, null);

    public static OperationResult Fail(string message) => new(false, message, null);

    public static OperationResult Fail(Dictionary<string, string[]> fieldErrors, string? message = null)
        => new(false, message, fieldErrors);
}
