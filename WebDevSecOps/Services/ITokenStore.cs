using System.Security.Claims;

namespace WebDevSecOps.Services;

public interface ITokenStore
{
    string StoreToken(string token);
    string? GetToken(string key);
    string? GetTokenFromPrincipal(ClaimsPrincipal user);
    void RemoveToken(string key);
    void RemoveTokenFromPrincipal(ClaimsPrincipal user);
}
