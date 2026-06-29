using WebDevSecOps.Services;

namespace WebDevSecOps.SecurityTests.Common;

public class FakeAuthService : IAuthService
{
    public Task<LoginResult> LoginAsync(Models.LoginRequest request, CancellationToken ct = default)
        => Task.FromResult(LoginResult.Success("fake-token-for-testing"));

    public Task<bool> LogoutAsync(string token, CancellationToken ct = default)
        => Task.FromResult(true);
}
