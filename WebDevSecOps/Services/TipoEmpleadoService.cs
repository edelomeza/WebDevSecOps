using System.Net.Http.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public class TipoEmpleadoService : ITipoEmpleadoService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<TipoEmpleadoService> _logger;

    public TipoEmpleadoService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ITokenStore tokenStore, ILogger<TipoEmpleadoService> logger)
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

    public async Task<PaginatedResponse<EmpCatTipoEmpleado>?> GetAllAsync(int pageNumber = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/TipoEmpleado?PageNumber={pageNumber}&PageSize={pageSize}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<PaginatedResponse<EmpCatTipoEmpleado>>(cancellationToken: ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get tipos empleado request was cancelled");
            throw new OperationCanceledException("The get tipos empleado request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching tipos empleado");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching tipos empleado");
            return null;
        }
    }

    public async Task<EmpCatTipoEmpleado?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var token = GetToken();

        if (token is null)
        {
            _logger.LogWarning("No access token found");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/TipoEmpleado/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<EmpCatTipoEmpleado>(cancellationToken: ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("TipoEmpleado with id {Id} not found", id);
                return null;
            }

            _logger.LogWarning("Get tipo empleado by id failed with status {StatusCode}", response.StatusCode);
            return null;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get tipo empleado by id request was cancelled");
            throw new OperationCanceledException("The get tipo empleado by id request was cancelled.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Connection error fetching tipo empleado {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching tipo empleado {Id}", id);
            return null;
        }
    }
}
