using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IClienteService
{
    Task<PaginatedResponse<Cliente>?> GetClientesAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<PaginatedResponse<Cliente>?> SearchClientesAsync(string? texto, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<Cliente?> GetClienteByIdAsync(int id, CancellationToken ct = default);

    Task<OperationResult> CreateClienteAsync(ClienteCreateViewModel model, CancellationToken ct = default);

    Task<OperationResult> UpdateClienteAsync(int id, ClienteUpdateViewModel model, CancellationToken ct = default);

    Task<OperationResult> DeleteClienteAsync(int id, byte[] rowVersion, CancellationToken ct = default);
}
