using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IEmpleadoService
{
    Task<PaginatedResponse<Empleado>?> GetEmpleadosAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<PaginatedResponse<Empleado>?> SearchEmpleadosAsync(string? texto, int? idTipoEmpleado, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<Empleado?> GetEmpleadoByIdAsync(int id, CancellationToken ct = default);

    Task<OperationResult> CreateEmpleadoAsync(EmpleadoCreateViewModel model, CancellationToken ct = default);

    Task<OperationResult> UpdateEmpleadoAsync(int id, EmpleadoUpdateViewModel model, CancellationToken ct = default);

    Task<OperationResult> DeleteEmpleadoAsync(int id, byte[] rowVersion, CancellationToken ct = default);
}
