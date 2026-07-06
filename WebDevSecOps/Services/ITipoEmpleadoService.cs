using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface ITipoEmpleadoService
{
    Task<PaginatedResponse<EmpCatTipoEmpleado>?> GetAllAsync(int pageNumber = 1, int pageSize = 100, CancellationToken ct = default);

    Task<EmpCatTipoEmpleado?> GetByIdAsync(int id, CancellationToken ct = default);
}
