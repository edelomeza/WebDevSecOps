using System.Net.Http.Json;
using System.Text.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public class VentaService : IVentaService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<VentaService> _logger;

    public VentaService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ITokenStore tokenStore, ILogger<VentaService> logger)
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

    public async Task<PaginatedResponse<Venta>?> GetVentasAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Venta?PageNumber={pageNumber}&PageSize={pageSize}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Venta>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get ventas request was cancelled");
            throw new OperationCanceledException("The get ventas request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching ventas");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching ventas");
            return null;
        }
    }

    public async Task<PaginatedResponse<Venta>?> SearchVentasAsync(string? texto, DateTime? dteFechaInicio, DateTime? dteFechaFin, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
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
            {
                queryParams.Add($"strClaveVenta={Uri.EscapeDataString(texto)}");
                queryParams.Add($"strNombreCliente={Uri.EscapeDataString(texto)}");
            }

            if (dteFechaInicio.HasValue)
                queryParams.Add($"dteFechaInicio={dteFechaInicio.Value:yyyy-MM-dd}");

            if (dteFechaFin.HasValue)
                queryParams.Add($"dteFechaFin={dteFechaFin.Value:yyyy-MM-dd}");

            var queryString = string.Join("&", queryParams);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Venta/buscar?{queryString}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Venta>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Search ventas request was cancelled");
            throw new OperationCanceledException("The search ventas request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error searching ventas");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching ventas");
            return null;
        }
    }

    public async Task<OperationResult> CreateVentaAsync(VentaCreateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Venta")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Venta created successfully");
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "CreateVenta", ct: ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al crear la venta.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Create venta request was cancelled");
            throw new OperationCanceledException("The create venta request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while creating venta");
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating venta");
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<Venta?> GetVentaByIdAsync(int id, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Venta/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Venta>(cancellationToken: ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Venta with id {Id} not found", id);
                return null;
            }

            _logger.LogWarning("Get venta by id failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get venta by id request was cancelled");
            throw new OperationCanceledException("The get venta by id request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching venta {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching venta {Id}", id);
            return null;
        }
    }

    public async Task<List<VentaDetalle>> GetVentaDetallesAsync(int idVenVenta, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return [];
        }

        try
        {
            _logger.LogWarning("GetVentaDetalle requesting idVenVenta={IdVenVenta}", idVenVenta);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/ventadetalle?idVenVenta={idVenVenta}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var rawJson = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("GetVentaDetalle raw JSON: {Body}", rawJson);

            var paginated = await response.Content.ReadFromJsonAsync<PaginatedResponse<VentaDetalle>>(cancellationToken: ct);
            return paginated?.Items?.Where(d => d.IdVenVenta == idVenVenta).ToList() ?? [];
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get venta detalles request was cancelled");
            throw new OperationCanceledException("The get venta detalles request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching venta detalles");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching venta detalles");
            return [];
        }
    }

    public async Task<OperationResult> CreateVentaDetalleAsync(VentaAgregarProductoViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/ventadetalle")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("VentaDetalle created successfully for Venta {Id}", model.IdVenVenta);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "CreateVentaDetalle", ct: ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("CreateVentaDetalle raw body: {Body}", rawBody);
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al agregar el producto.";
            _logger.LogWarning("CreateVentaDetalle failed: {Message}", message);
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Create venta detalle request was cancelled");
            throw new OperationCanceledException("The create venta detalle request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while creating venta detalle");
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating venta detalle");
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<OperationResult> UpdateVentaAsync(int id, Venta model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Venta/{id}")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Venta {Id} updated successfully", id);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "UpdateVenta", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            _logger.LogWarning("UpdateVenta {Id} response body: {Body}", id, rawBody);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return OperationResult.Fail("El registro fue modificado por otro usuario. Recargue la pagina e intente nuevamente.");

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al actualizar la venta.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Update venta request was cancelled");
            throw new OperationCanceledException("The update venta request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while updating venta {Id}", id);
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating venta {Id}", id);
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<OperationResult> DeleteVentaDetalleAsync(int id, byte[] rowVersion, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/ventadetalle/{id}")
            {
                Content = JsonContent.Create(new { id, rowVersion })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("VentaDetalle {Id} deleted successfully", id);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "DeleteVentaDetalle", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return OperationResult.Fail("El registro fue modificado por otro usuario. Recargue la pagina e intente nuevamente.");

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al eliminar el producto.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Delete venta detalle request was cancelled");
            throw new OperationCanceledException("The delete venta detalle request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while deleting venta detalle {Id}", id);
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting venta detalle {Id}", id);
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }
}
