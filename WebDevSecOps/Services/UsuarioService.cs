using System.Net.Http.Json;
using System.Text.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public class UsuarioService : IUsuarioService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ITokenStore tokenStore, ILogger<UsuarioService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    private string? GetToken()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user is not null ? _tokenStore.GetTokenFromPrincipal(user) : null;
    }

    public async Task<PaginatedResponse<Usuario>?> GetUsuariosAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Usuario?PageNumber={pageNumber}&PageSize={pageSize}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Usuario>>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching usuarios from API");
            return null;
        }
    }

    public async Task<CreateUsuarioResult> CreateUsuarioAsync(UsuarioCreateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return CreateUsuarioResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Usuario")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Usuario created successfully");
                return CreateUsuarioResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "CreateUsuario", ct: ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
            {
                return CreateUsuarioResult.Fail(errorResponse.Errors, errorResponse.Title);
            }

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al crear el usuario.";
            return CreateUsuarioResult.Fail(message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Create usuario request was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while creating usuario");
            return CreateUsuarioResult.Fail("Error de conexión con el servidor. Verifique su conexión e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating usuario");
            return CreateUsuarioResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<Usuario?> GetUsuarioByIdAsync(int id, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Usuario/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Usuario>(cancellationToken: ct);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Usuario with id {Id} not found", id);
                return null;
            }

            _logger.LogWarning("Get usuario by id failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Get usuario by id request was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while fetching usuario {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching usuario {Id}", id);
            return null;
        }
    }

    public async Task<CreateUsuarioResult> UpdateUsuarioAsync(int id, UsuarioUpdateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return CreateUsuarioResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Usuario/{id}")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Usuario {Id} updated successfully", id);
                return CreateUsuarioResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "UpdateUsuario", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return CreateUsuarioResult.Fail("El registro fue modificado por otro usuario. Recargue la página e intente nuevamente.");
            }

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
            {
                return CreateUsuarioResult.Fail(errorResponse.Errors, errorResponse.Title);
            }

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al actualizar el usuario.";
            return CreateUsuarioResult.Fail(message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Update usuario request was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while updating usuario {Id}", id);
            return CreateUsuarioResult.Fail("Error de conexión con el servidor. Verifique su conexión e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating usuario {Id}", id);
            return CreateUsuarioResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<CreateUsuarioResult> DeleteUsuarioAsync(int id, byte[] rowVersion, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return CreateUsuarioResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Usuario/{id}")
            {
                Content = JsonContent.Create(new { id, rowVersion })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Usuario {Id} deleted successfully", id);
                return CreateUsuarioResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "DeleteUsuario", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return CreateUsuarioResult.Fail("El registro fue modificado por otro usuario. Recargue la página e intente nuevamente.");
            }

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
            {
                return CreateUsuarioResult.Fail(errorResponse.Errors, errorResponse.Title);
            }

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al eliminar el usuario.";
            return CreateUsuarioResult.Fail(message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Delete usuario request was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while deleting usuario {Id}", id);
            return CreateUsuarioResult.Fail("Error de conexión con el servidor. Verifique su conexión e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting usuario {Id}", id);
            return CreateUsuarioResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }
}
