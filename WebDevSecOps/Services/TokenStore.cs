using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace WebDevSecOps.Services;

public class TokenStore : ITokenStore
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Expiration = TimeSpan.FromHours(8);
    private const string TokenKeyClaim = "token_key";

    public TokenStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string StoreToken(string token)
    {
        var key = Guid.NewGuid().ToString("N");
        _cache.Set(key, token, Expiration);
        return key;
    }

    public string? GetToken(string key)
    {
        return _cache.TryGetValue(key, out string? token) ? token : null;
    }

    public string? GetTokenFromPrincipal(ClaimsPrincipal user)
    {
        var key = user.FindFirst(TokenKeyClaim)?.Value;
        return key is not null ? GetToken(key) : null;
    }

    public void RemoveToken(string key)
    {
        _cache.Remove(key);
    }

    public void RemoveTokenFromPrincipal(ClaimsPrincipal user)
    {
        var key = user.FindFirst(TokenKeyClaim)?.Value;
        if (key is not null)
        {
            _cache.Remove(key);
        }
    }
}
