using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<bool> LogoutAsync(string token, CancellationToken ct = default);
}
