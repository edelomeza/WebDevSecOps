---
name: "aspnet-core"
description: "ASP.NET Core MVC best practices, patterns and conventions used in this project"
license: "MIT"
---

# ASP.NET Core Best Practices

This skill documents the patterns, conventions and best practices used in this ASP.NET Core MVC project.

## Project Structure

- **Controllers** — MVC controllers in `Controllers/` folder, one per resource (`UsuarioController.cs`).
- **Views** — Razor views in `Views/{ControllerName}/` folder.
- **Razor Pages** — Used for auth pages (`Login`, `Index` home) in `Pages/` folder.
- **Services** — Business logic and API calls in `Services/` folder, with interfaces (`IUsuarioService`, `IAuthService`).
- **Models** — DTOs, ViewModels, and result types in `Models/` folder.

## Routing

- Default route: `{controller=Usuario}/{action=Index}/{id?}` (configured in `Program.cs`).
- Use `Redirect("/ruta/explicita")` instead of `RedirectToAction(nameof(X), "Y")` to avoid ambiguity between MVC and Razor Pages URL generation.
- For Razor Pages, use `RedirectToPage("/PageName")`.

## Authentication & Authorization

- Cookie-based authentication with BFF (Backend-for-Frontend) pattern.
- `[Authorize]` is the **global fallback policy** — all endpoints require auth by default.
- Use `[AllowAnonymous]` to opt out (e.g., `/Login` page).
- Access tokens are stored in `ClaimTypes` as `"access_token"` and retrieved via `HttpContext.User.FindFirst("access_token")?.Value`.
- Bearer tokens are forwarded to the backend API via `Authorization` header.
- The Login page (`Pages/Login.cshtml.cs`) must handle `ReturnUrl` properly:
  - Bind with `[BindProperty(SupportsGet = true)]`
  - After login success, validate with `Url.IsLocalUrl()` before redirecting.
- Logout is handled via a GET handler (`OnGetLogoutAsync`) on the Index page, called via `asp-page="/Index" asp-page-handler="Logout"`.

## Service Layer

- Services are registered with `AddHttpClient<IUsuarioService, UsuarioService>` for typed HttpClient.
- Services use `IHttpContextAccessor` to read the current user's claims (token).
- API calls use `HttpRequestMessage` with manual bearer token attachment.
- Resilience is configured via `AddStandardResilienceHandler()` (retries, timeouts, circuit breaker).

## HttpClient Usage Pattern

```csharp
public class UsuarioService : IUsuarioService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<CreateUsuarioResult> CreateUsuarioAsync(UsuarioCreateViewModel model, CancellationToken ct)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;
        if (token is null) return CreateUsuarioResult.Fail("Error de autenticación.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Usuario")
        {
            Content = JsonContent.Create(model)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        // handle response...
    }
}
```

## Anti-Forgery Tokens

- All POST actions must have `[ValidateAntiForgeryToken]`.
- Forms must include `@Html.AntiForgeryToken()`.
- GET-based state changes (like logout via handler) may skip CSRF for UX convenience.

## TempData for User Feedback

- Use `TempData["Success"]` for success messages.
- Use `TempData["Error"]` for error messages.
- Display in the view by checking `TempData` at the top of the page.

## View Models

- Create **separate ViewModels** per action: `UsuarioCreateViewModel`, `UsuarioUpdateViewModel`, `UsuarioDetailsViewModel`, `UsuarioDeleteViewModel`.
- Never pass domain entities directly to views.
- Use `Result` wrapper types (`CreateUsuarioResult`) to encapsulate success/failure with field-level errors.

## Error Handling

- Log all errors with `ILogger<T>` using structured logging (`_logger.LogWarning`, `_logger.LogError`).
- Catch specific exception types (`OperationCanceledException`, `HttpRequestException`) before generic `Exception`.
- Provide user-friendly error messages in Spanish.

## Naming Conventions

- String fields prefixed with `Str` (e.g., `StrNombre`, `StrCorreoElectronico`).
- Paginated responses use `PaginatedResponse<T>` with `Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`.
- Async methods use `Async` suffix.
- Cancellation tokens are named `ct` and passed through async calls.

## Bootstrap & Razor Tag Helpers

- Use Bootstrap 5 classes for layout (`container`, `row`, `col-md-*`, `table`, `btn`, `alert`).
- Use `asp-for`, `asp-validation-for`, `asp-action`, `asp-controller`, `asp-page` tag helpers.
- Client-side validation via `_ValidationScriptsPartial`.
- Use `novalidate` on forms with custom JS validation.

## Middleware Pipeline Order (Program.cs)

1. `UseExceptionHandler` / `UseHsts` — dev vs production
2. `UseHttpsRedirection`
3. `UseStaticFiles`
4. `UseRouting`
5. `UseAuthentication`
6. `UseAuthorization`
7. `MapControllerRoute` (MVC routes first)
8. `MapRazorPages`
