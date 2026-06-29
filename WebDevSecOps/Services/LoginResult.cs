namespace WebDevSecOps.Services;

public class LoginResult
{
    public bool IsSuccess { get; }
    public string? Token { get; }
    public string? ErrorMessage { get; }

    private LoginResult(bool isSuccess, string? token, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Token = token;
        ErrorMessage = errorMessage;
    }

    public static LoginResult Success(string token) => new(true, token, null);

    public static LoginResult Failure(string errorMessage) => new(false, null, errorMessage);
}
