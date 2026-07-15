using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IVentaService
{
    Task<PaginatedResponse<Venta>?> GetVentasAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<PaginatedResponse<Venta>?> SearchVentasAsync(string? texto, DateTime? dteFechaInicio, DateTime? dteFechaFin, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<OperationResult> CreateVentaAsync(VentaCreateViewModel model, CancellationToken ct = default);
}
