using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IEstadoVentaService
{
    Task<PaginatedResponse<VenCatEstado>?> GetAllAsync(int pageNumber = 1, int pageSize = 100, CancellationToken ct = default);

    Task<VenCatEstado?> GetByIdAsync(int id, CancellationToken ct = default);
}
