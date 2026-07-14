using System.Net.Http.Json;
using System.Text.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public class ClienteService : IClienteService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ITokenStore tokenStore, ILogger<ClienteService> logger)
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

    public async Task<PaginatedResponse<Cliente>?> GetClientesAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Cliente?PageNumber={pageNumber}&PageSize={pageSize}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Cliente>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get clientes request was cancelled");
            throw new OperationCanceledException("The get clientes request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching clientes");
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON while fetching clientes");
            return null;
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Unsupported content while fetching clientes");
            return null;
        }
    }

    public async Task<PaginatedResponse<Cliente>?> SearchClientesAsync(string? texto, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            var queryParams = new List<string>
            {
                $"PageNumber={pageNumber}",
                $"PageSize={pageSize}"
            };

            if (!string.IsNullOrEmpty(texto))
                queryParams.Add($"texto={Uri.EscapeDataString(texto)}");

            var queryString = string.Join("&", queryParams);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Cliente/buscar?{queryString}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Cliente>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Search clientes request was cancelled");
            throw new OperationCanceledException("The search clientes request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error searching clientes");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching clientes");
            return null;
        }
    }

    public async Task<Cliente?> GetClienteByIdAsync(int id, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Cliente/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Cliente>(cancellationToken: ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Cliente with id {Id} not found", id);
                return null;
            }

            _logger.LogWarning("Get cliente by id failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get cliente by id request was cancelled");
            throw new OperationCanceledException("The get cliente by id request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching cliente {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching cliente {Id}", id);
            return null;
        }
    }

    public async Task<OperationResult> CreateClienteAsync(ClienteCreateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Cliente")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Cliente created successfully");
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "CreateCliente", ct: ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al crear el cliente.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Create cliente request was cancelled");
            throw new OperationCanceledException("The create cliente request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while creating cliente");
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating cliente");
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<OperationResult> UpdateClienteAsync(int id, ClienteUpdateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Cliente/{id}")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Cliente {Id} updated successfully", id);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "UpdateCliente", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return OperationResult.Fail("El registro fue modificado por otro usuario. Recargue la pagina e intente nuevamente.");

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al actualizar el cliente.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Update cliente request was cancelled");
            throw new OperationCanceledException("The update cliente request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while updating cliente {Id}", id);
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating cliente {Id}", id);
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<OperationResult> DeleteClienteAsync(int id, byte[] rowVersion, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Cliente/{id}")
            {
                Content = JsonContent.Create(new { id, rowVersion })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Cliente {Id} deleted successfully", id);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "DeleteCliente", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return OperationResult.Fail("El registro fue modificado por otro usuario. Recargue la pagina e intente nuevamente.");

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al eliminar el cliente.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Delete cliente request was cancelled");
            throw new OperationCanceledException("The delete cliente request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while deleting cliente {Id}", id);
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting cliente {Id}", id);
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }
}
