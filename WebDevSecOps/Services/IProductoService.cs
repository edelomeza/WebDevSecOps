using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IProductoService
{
    Task<PaginatedResponse<Producto>?> GetProductosAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<PaginatedResponse<Producto>?> SearchProductosAsync(string? texto, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<Producto?> GetProductoByIdAsync(int id, CancellationToken ct = default);

    Task<OperationResult> CreateProductoAsync(ProductoCreateViewModel model, CancellationToken ct = default);

    Task<OperationResult> UpdateProductoAsync(int id, ProductoUpdateViewModel model, CancellationToken ct = default);

    Task<OperationResult> DeleteProductoAsync(int id, byte[] rowVersion, CancellationToken ct = default);

    Task<List<ProProductoAutocompleteDto>> AutocompleteProductosAsync(string texto, int maxResultados = 10, CancellationToken ct = default);
}
