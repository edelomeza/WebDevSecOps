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
}
