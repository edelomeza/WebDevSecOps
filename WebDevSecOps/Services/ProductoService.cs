using System.Net.Http.Json;
using System.Text.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public class ProductoService : IProductoService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<ProductoService> _logger;

    public ProductoService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ITokenStore tokenStore, ILogger<ProductoService> logger)
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

    public async Task<PaginatedResponse<Producto>?> GetProductosAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Producto?PageNumber={pageNumber}&PageSize={pageSize}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Producto>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get productos request was cancelled");
            throw new OperationCanceledException("The get productos request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching productos");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching productos");
            return null;
        }
    }

    public async Task<PaginatedResponse<Producto>?> SearchProductosAsync(string? texto, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
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

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Producto/buscar?{queryString}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<Producto>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Search productos request was cancelled");
            throw new OperationCanceledException("The search productos request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error searching productos");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error searching productos");
            return null;
        }
    }

    public async Task<Producto?> GetProductoByIdAsync(int id, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Producto/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<Producto>(cancellationToken: ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Producto with id {Id} not found", id);
                return null;
            }

            _logger.LogWarning("Get producto by id failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get producto by id request was cancelled");
            throw new OperationCanceledException("The get producto by id request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching producto {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching producto {Id}", id);
            return null;
        }
    }

    public async Task<OperationResult> CreateProductoAsync(ProductoCreateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Producto")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Producto created successfully");
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "CreateProducto", ct: ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);
            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al crear el producto.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Create producto request was cancelled");
            throw new OperationCanceledException("The create producto request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while creating producto");
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating producto");
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<OperationResult> UpdateProductoAsync(int id, ProductoUpdateViewModel model, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"api/v1/Producto/{id}")
            {
                Content = JsonContent.Create(model)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Producto {Id} updated successfully", id);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "UpdateProducto", id, ct);

            var rawBody = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return OperationResult.Fail("El registro fue modificado por otro usuario. Recargue la pagina e intente nuevamente.");

            var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

            if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
                return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

            var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al actualizar el producto.";
            return OperationResult.Fail(message);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Update producto request was cancelled");
            throw new OperationCanceledException("The update producto request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while updating producto {Id}", id);
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating producto {Id}", id);
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }

    public async Task<OperationResult> DeleteProductoAsync(int id, byte[] rowVersion, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return OperationResult.Fail("Error de autenticación. Inicie sesión nuevamente.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/Producto/{id}")
            {
                Content = JsonContent.Create(new { id, rowVersion })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Producto {Id} deleted successfully", id);
                return OperationResult.Ok();
            }

            await SafeResponseLogger.LogResponseFailure(_logger, response, "DeleteProducto", id, ct);

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
            _logger.LogWarning(ex, "Delete producto request was cancelled");
            throw new OperationCanceledException("The delete producto request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error while deleting producto {Id}", id);
            return OperationResult.Fail("Error de conexion con el servidor. Verifique su conexion e intente nuevamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting producto {Id}", id);
            return OperationResult.Fail("Error inesperado. Intente nuevamente.");
        }
    }
}
