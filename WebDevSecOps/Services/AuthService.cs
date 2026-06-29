using System.Net.Http.Json;
using System.Text.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    public AuthService(HttpClient httpClient, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/Login/login", request, ct);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);

                if (loginResponse?.Token is not null && loginResponse.Token.Length > 0)
                {
                    return LoginResult.Success(loginResponse.Token);
                }

                _logger.LogWarning("API returned success but token was null or empty");
                return LoginResult.Failure("Respuesta inválida del servidor de autenticación");
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "Login", ct: ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(rawBody);
            var errorMessage = !string.IsNullOrEmpty(errorResponse?.Detail)
                ? errorResponse.Detail
                : (errorResponse?.Title ?? "Credenciales inválidas");
            return LoginResult.Failure(errorMessage);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Login request was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while calling login API");
            return LoginResult.Failure("Error de conexión con el servidor. Verifique su conexión e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return LoginResult.Failure("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<bool> LogoutAsync(string token, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Logout/logout");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling logout API");
            return false;
        }
    }
}
