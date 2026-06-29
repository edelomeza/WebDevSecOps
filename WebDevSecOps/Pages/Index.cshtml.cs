using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebDevSecOps.Services;

namespace WebDevSecOps.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IAuthService authService, ITokenStore tokenStore, ILogger<IndexModel> logger)
    {
        _authService = authService;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public string? Username { get; set; }

    public void OnGet()
    {
        Username = User.Identity?.Name ?? "Usuario";
    }

    public async Task<IActionResult> OnGetLogoutAsync(CancellationToken ct)
    {
        var token = _tokenStore.GetTokenFromPrincipal(User);

        if (token is not null)
        {
            await _authService.LogoutAsync(token, ct);
        }

        _tokenStore.RemoveTokenFromPrincipal(User);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User logged out");

        return RedirectToPage("/Login");
    }
}
