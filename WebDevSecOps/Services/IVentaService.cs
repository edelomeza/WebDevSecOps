using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IVentaService
{
    Task<PaginatedResponse<Venta>?> GetVentasAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<PaginatedResponse<Venta>?> SearchVentasAsync(string? texto, DateTime? dteFechaInicio, DateTime? dteFechaFin, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<OperationResult> CreateVentaAsync(VentaCreateViewModel model, CancellationToken ct = default);

    Task<Venta?> GetVentaByIdAsync(int id, CancellationToken ct = default);

    Task<List<VentaDetalle>> GetVentaDetallesAsync(int idVenVenta, CancellationToken ct = default);

    Task<OperationResult> CreateVentaDetalleAsync(VentaAgregarProductoViewModel model, CancellationToken ct = default);

    Task<OperationResult> DeleteVentaDetalleAsync(int id, byte[] rowVersion, CancellationToken ct = default);

    Task<OperationResult> UpdateVentaAsync(int id, Venta model, CancellationToken ct = default);
}
