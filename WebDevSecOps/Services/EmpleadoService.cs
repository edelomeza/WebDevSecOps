using System.Net.Http.Json;
using System.Text.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public class EmpleadoService : IEmpleadoService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<EmpleadoService> _logger;

    public EmpleadoService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ITokenStore tokenStore, ILogger<EmpleadoService> logger)
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

    public async Task<PaginatedResponse<Empleado>?> GetEmpleadosAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Empleado?PageNumber={pageNumber}&PageSize={pageSize}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Empleado>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get empleados request was cancelled");
            throw new OperationCanceledException("The get empleados request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching empleados");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching empleados");
            return null;
        }
    }

    public async Task<PaginatedResponse<Empleado>?> SearchEmpleadosAsync(string? texto, int? idTipoEmpleado, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
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

            if (idTipoEmpleado.HasValue)
                queryParams.Add($"idTipoEmpleado={idTipoEmpleado.Value}");

            var queryString = string.Join("&", queryParams);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Empleado/buscar?{queryString}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Empleado>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Search empleados request was cancelled");
            throw new OperationCanceledException("The search empleados request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error searching empleados");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching empleados");
            return null;
        }
    }

    public async Task<Empleado?> GetEmpleadoByIdAsync(int id, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Empleado/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Empleado>(cancellationToken: ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Empleado with id {Id} not found", id);
                return null;
            }

            _logger.LogWarning("Get empleado by id failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get empleado by id request was cancelled");
            throw new OperationCanceledException("The get empleado by id request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching empleado {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching empleado {Id}", id);
            return null;
        }
    }

    public async Task<OperationResult> CreateEmpleadoAsync(EmpleadoCreateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticacion. Inicie sesion nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Empleado")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Empleado created successfully");
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "CreateEmpleado", ct: ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al crear el empleado.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Create empleado request was cancelled");
            throw new OperationCanceledException("The create empleado request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while creating empleado");
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating empleado");
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<OperationResult> UpdateEmpleadoAsync(int id, EmpleadoUpdateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticacion. Inicie sesion nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Empleado/{id}")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Empleado {Id} updated successfully", id);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "UpdateEmpleado", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return OperationResult.Fail("El registro fue modificado por otro usuario. Recargue la pagina e intente nuevamente.");

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al actualizar el empleado.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Update empleado request was cancelled");
            throw new OperationCanceledException("The update empleado request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while updating empleado {Id}", id);
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating empleado {Id}", id);
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<OperationResult> DeleteEmpleadoAsync(int id, byte[] rowVersion, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticacion. Inicie sesion nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Empleado/{id}")
            {
                Content = JsonContent.Create(new { id, rowVersion })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Empleado {Id} deleted successfully", id);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "DeleteEmpleado", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return OperationResult.Fail("El registro fue modificado por otro usuario. Recargue la pagina e intente nuevamente.");

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al eliminar el empleado.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Delete empleado request was cancelled");
            throw new OperationCanceledException("The delete empleado request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while deleting empleado {Id}", id);
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting empleado {Id}", id);
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }
}
