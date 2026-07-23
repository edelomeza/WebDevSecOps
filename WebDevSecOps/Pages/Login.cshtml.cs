using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.Pages;

[AllowAnonymous]
[EnableRateLimiting("LoginPolicy")]
public class LoginModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IAuthService authService, ITokenStore tokenStore, ILogger<LoginModel> logger)
    {
        _authService = authService;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    [BindProperty]
    public LoginRequest Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _authService.LoginAsync(Input, ct);

        if (result.IsSuccess)
        {
            var tokenKey = _tokenStore.StoreToken(result.Token!);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, Input.Username),
                new("token_key", tokenKey)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = Input.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            _logger.LogInformation("User {Username} logged in successfully", Input.Username);

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }

        ErrorMessage = result.ErrorMessage;
        _logger.LogWarning("Failed login attempt for user {Username}: {Error}", Input.Username, result.ErrorMessage);
        return Page();
    }
}
