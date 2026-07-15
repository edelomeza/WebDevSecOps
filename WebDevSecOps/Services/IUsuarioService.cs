using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IUsuarioService
{
    Task<PaginatedResponse<Usuario>?> GetUsuariosAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<PaginatedResponse<Usuario>?> BuscarUsuariosAsync(string texto, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);

    Task<List<SegUsuarioAutocompleteDto>> AutocompleteUsuariosAsync(string texto, int maxResultados = 10, CancellationToken ct = default);

    Task<CreateUsuarioResult> CreateUsuarioAsync(UsuarioCreateViewModel model, CancellationToken ct = default);

    Task<Usuario?> GetUsuarioByIdAsync(int id, CancellationToken ct = default);

    Task<CreateUsuarioResult> UpdateUsuarioAsync(int id, UsuarioUpdateViewModel model, CancellationToken ct = default);

    Task<CreateUsuarioResult> DeleteUsuarioAsync(int id, byte[] rowVersion, CancellationToken ct = default);
}
