---
name: "crud-module"
description: "Patterns, conventions and file-by-file templates for creating new CRUD modules (like Empleado) following the established Usuario architecture. Use when building a new CRUD resource: controller, service, viewmodels, views, DI registration, and menu integration."
license: "MIT"
---

# CRUD Module — Construction Guide

This skill documents the exact file structure, patterns, and conventions used to build the **Usuario** CRUD module. Use it as a blueprint when creating a new CRUD resource (e.g., `Empleado`).

## Project Structure for a CRUD Module

Every CRUD module requires **14 new files** + **2 modifications** to existing files:

```
WebDevSecOps/
├── Controllers/
│   └── {Resource}Controller.cs              ← NEW (1)
├── Models/
│   ├── {Resource}.cs                         ← NEW (2) Entity model
│   ├── {Resource}CreateViewModel.cs          ← NEW (3)
│   ├── {Resource}UpdateViewModel.cs          ← NEW (4)
│   ├── {Resource}DetailsViewModel.cs         ← NEW (5)
│   └── {Resource}DeleteViewModel.cs          ← NEW (6)
├── Services/
│   ├── I{Resource}Service.cs                 ← NEW (7) Interface
│   └── {Resource}Service.cs                  ← NEW (8) Implementation
├── Views/
│   └── {Resource}/
│       ├── Index.cshtml                      ← NEW (9)
│       ├── Create.cshtml                     ← NEW (10)
│       ├── Details.cshtml                    ← NEW (11)
│       ├── Update.cshtml                     ← NEW (12)
│       └── Delete.cshtml                     ← NEW (13)
├── Pages/Shared/
│   └── _Layout.cshtml                        ← MODIFY (14) Add menu item
└── Program.cs                                ← MODIFY (15) Register service
```

**Reusable shared files** (already exist, do NOT recreate):
- `Models/PaginatedResponse<T>` — Generic pagination DTO
- `Models/OperationResult.cs` — Result pattern for mutations (see below)
- `Models/ApiErrorResponse.cs` — RFC 7807 error deserialization
- `Services/SafeResponseLogger.cs` — Sanitized logging helper
- `Views/_ViewImports.cshtml` — Namespace imports + tag helpers
- `Views/_ViewStart.cshtml` — Layout reference

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Controller | `{Resource}Controller` | `EmpleadoController` |
| Service interface | `I{Resource}Service` | `IEmpleadoService` |
| Service impl | `{Resource}Service` | `EmpleadoService` |
| Entity model | `{Resource}` | `Empleado` |
| ViewModel | `{Resource}{Action}ViewModel` | `EmpleadoCreateViewModel` |
| View folder | `Views/{Resource}/` | `Views/Empleado/` |
| Route segment | `/{Resource}/{action}` | `/Empleado/Index` |
| API endpoint | `api/v1/{Resource}` | `api/v1/Empleado` |
| String fields | Prefix `Str` | `StrNombre`, `StrTelefono` |
| Date fields | Prefix `Dte` | `DteFechaRegistro` |
| Cancellation token | `ct` | `CancellationToken ct` |

## Controller Pattern (`{Resource}Controller.cs`)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDevSecOps.Models;
using WebDevSecOps.Services;

namespace WebDevSecOps.Controllers;

[Authorize]
public class EmpleadoController : Controller
{
    private readonly IEmpleadoService _service;
    private readonly ILogger<EmpleadoController> _logger;

    public EmpleadoController(IEmpleadoService service, ILogger<EmpleadoController> logger)
    {
        _service = service;
        _logger = logger;
    }
```

### Actions Checklist

| # | Action | Verb | Parameters | Returns | Caching |
|---|--------|------|-----------|---------|---------|
| 1 | `Index` | GET | `int pageNumber = 1, int pageSize = 10` | `View(PaginatedResponse<{Resource}>)` | `[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, NoStore = false)]` |
| 2 | `Details` | GET | `int id, CancellationToken ct, string? returnUrl = null` | `View({Resource}DetailsViewModel)` | `[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, NoStore = false)]` |
| 3 | `Create` | GET | `string? returnUrl = null` | `View()` | None |
| 4 | `Create` | POST | `{Resource}CreateViewModel model, CancellationToken ct, string? returnUrl = null` | `RedirectToAction("Index")` or `View(model)` | None |
| 5 | `Update` | GET | `int id, CancellationToken ct, string? returnUrl = null` | `View({Resource}UpdateViewModel)` | None |
| 6 | `Update` | POST | `{Resource}UpdateViewModel model, CancellationToken ct, string? returnUrl = null` | `RedirectToAction("Index")` or `View(model)` | None |
| 7 | `Delete` | GET | `int id, CancellationToken ct, string? returnUrl = null` | `View({Resource}DeleteViewModel)` | None |
| 8 | `Delete` | POST | `{Resource}DeleteViewModel model, CancellationToken ct, string? returnUrl = null` | `RedirectToAction("Index")` or `View(model)` | None |

### Required Attributes on POST actions
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
```

### Error Handling Pattern (same for Create, Update, Delete POST)

```csharp
// After calling service method:
if (result.Success)
{
    _logger.LogInformation("{Resource} {Id} created/updated/deleted successfully", id);
    TempData["Success"] = "{Resource} procesado exitosamente.";
    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        return Redirect(returnUrl);
    return RedirectToAction("Index");
}

// Field errors
if (result.FieldErrors is not null)
{
    foreach (var kvp in result.FieldErrors)
    {
        foreach (var msg in kvp.Value)
        {
            var key = kvp.Key switch
            {
                "strNombre" => nameof({Resource}UpdateViewModel.StrNombre),
                // ... map other fields
                _ => kvp.Key
            };
            ModelState.AddModelError(key, msg);
        }
    }
}

// General error
if (result.ErrorMessage is not null)
    ModelState.AddModelError(string.Empty, result.ErrorMessage);

ViewData["ReturnUrl"] = returnUrl;
return View(model);
```

### ReturnUrl Handling

- Always accept `string? returnUrl = null` on every action.
- Store in `ViewData["ReturnUrl"]` on GET.
- Pass as hidden field `<input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />` in forms.
- On success, validate with `Url.IsLocalUrl(returnUrl)` before redirecting.
- Pass `asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)"` in action links.

## Entity Model (`Models/{Resource}.cs`)

```csharp
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class Empleado
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strNombre")]
    public string StrNombre { get; set; } = string.Empty;

    [JsonPropertyName("strCorreoElectronico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;

    [JsonPropertyName("dteFechaRegistro")]
    public DateTime DteFechaRegistro { get; set; }

    [JsonPropertyName("rowVersion")]
    public byte[]? RowVersion { get; set; }
}
```

**Rules:**
- `string` defaults to `string.Empty` (never null).
- `RowVersion` is `byte[]?` on the entity, `byte[]` (non-nullable) on ViewModels.
- Always use `[JsonPropertyName("snake_case")]` matching the API contract.

## ViewModel Patterns

### CreateViewModel

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class EmpleadoCreateViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9_ ]+$", ErrorMessage = "El nombre solo permite letras, numeros y espacios.")]
    [JsonPropertyName("strNombre")]
    [Display(Name = "Nombre")]
    public string StrNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [StringLength(50, ErrorMessage = "El correo no puede exceder los 50 caracteres.")]
    [EmailAddress(ErrorMessage = "El formato del correo electronico no es valido.")]
    [JsonPropertyName("strCorreoElectronico")]
    [Display(Name = "Correo Electronico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;
}
```

### UpdateViewModel — add hidden Id + RowVersion, nullable password

```csharp
[Required][JsonRequired][JsonPropertyName("id")]
public int Id { get; set; }

// ... same fields as Create but with RowVersion added:

[Required][JsonPropertyName("rowVersion")]
public byte[] RowVersion { get; set; } = [];

// Optional password (nullable, not [Required]):
[MinLength(8, ErrorMessage = "...")]
[DataType(DataType.Password)]
[JsonPropertyName("strPWD")]
[Display(Name = "Contrasena")]
public string? StrPWD { get; set; }
```

### DetailsViewModel — include DteFechaRegistro, read-only

```csharp
[Display(Name = "Nombre Completo")]
[JsonPropertyName("strNombre")]
public string StrNombre { get; set; } = string.Empty;

[Display(Name = "Fecha de Registro")]
[JsonPropertyName("dteFechaRegistro")]
public DateTime DteFechaRegistro { get; set; }

[Required][JsonPropertyName("rowVersion")]
public byte[] RowVersion { get; set; } = [];
```

### DeleteViewModel — display-only fields + concurrency

```csharp
[Required][JsonRequired][JsonPropertyName("id")]
public int Id { get; set; }

[Display(Name = "Nombre")]
public string StrNombre { get; set; } = string.Empty;

[Required][JsonPropertyName("rowVersion")]
public byte[] RowVersion { get; set; } = [];
```

## OperationResult (`Models/OperationResult.cs`)

Reuse this pattern instead of creating a per-resource result class:

```csharp
namespace WebDevSecOps.Models;

public class OperationResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public Dictionary<string, string[]>? FieldErrors { get; }

    private OperationResult(bool success, string? errorMessage, Dictionary<string, string[]>? fieldErrors)
    {
        Success = success;
        ErrorMessage = errorMessage;
        FieldErrors = fieldErrors;
    }

    public static OperationResult Ok() => new(true, null, null);
    public static OperationResult Fail(string message) => new(false, message, null);
    public static OperationResult Fail(Dictionary<string, string[]> fieldErrors, string? message = null)
        => new(false, message, fieldErrors);
}
```

If `CreateUsuarioResult` exists in the project, it can be renamed to `OperationResult` for reuse or kept as-is; either way, use this pattern in the new service.

## Service Layer

### Interface

```csharp
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public interface IEmpleadoService
{
    Task<PaginatedResponse<Empleado>?> GetEmpleadosAsync(int pageNumber = 1, int pageSize = 10, CancellationToken ct = default);
    Task<OperationResult> CreateEmpleadoAsync(EmpleadoCreateViewModel model, CancellationToken ct = default);
    Task<Empleado?> GetEmpleadoByIdAsync(int id, CancellationToken ct = default);
    Task<OperationResult> UpdateEmpleadoAsync(int id, EmpleadoUpdateViewModel model, CancellationToken ct = default);
    Task<OperationResult> DeleteEmpleadoAsync(int id, byte[] rowVersion, CancellationToken ct = default);
}
```

### Implementation Template

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.Services;

public class EmpleadoService : IEmpleadoService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenStore _tokenStore;
    private readonly ILogger<EmpleadoService> _logger;

    public EmpleadoService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ITokenStore tokenStore,
        ILogger<EmpleadoService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    private string? GetToken()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user is not null ? _tokenStore.GetTokenFromPrincipal(user) : null;
    }
```

#### Method — List (GET)

```csharp
public async Task<PaginatedResponse<Empleado>?> GetEmpleadosAsync(
    int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
{
    var token = GetToken();
    if (token is null) { _logger.LogWarning("No access token found"); return null; }

    try
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"api/v1/Empleado?PageNumber={pageNumber}&PageSize={pageSize}");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaginatedResponse<Empleado>>(cancellationToken: ct);
    }
    catch (OperationCanceledException ex)
    {
        _logger.LogWarning(ex, "Request was cancelled");
        throw new OperationCanceledException("The request was cancelled.", ex);
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "Connection error");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        return null;
    }
}
```

#### Method — GetById (GET)

```csharp
public async Task<Empleado?> GetEmpleadoByIdAsync(int id, CancellationToken ct = default)
{
    var token = GetToken();
    if (token is null) { _logger.LogWarning("No access token found"); return null; }

    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Empleado/{id}");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<Empleado>(cancellationToken: ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Empleado with id {Id} not found", id);
            return null;
        }

        _logger.LogWarning("Get by id failed with status {StatusCode}", response.StatusCode);
        return null;
    }
    catch (OperationCanceledException ex) { /* rethrow */ }
    catch (HttpRequestException ex) { /* return null */ }
    catch (Exception ex) { /* return null */ }
}
```

#### Method — Create (POST)

```csharp
public async Task<OperationResult> CreateEmpleadoAsync(
    EmpleadoCreateViewModel model, CancellationToken ct = default)
{
    var token = GetToken();
    if (token is null) return OperationResult.Fail("Error de autenticacion. Inicie sesion nuevamente.");

    try
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Empleado")
        {
            Content = JsonContent.Create(model)
        };
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Empleado created successfully");
            return OperationResult.Ok();
        }

        await SafeResponseLogger.LogResponseFailure(_logger, response, "CreateEmpleado", ct: ct);

        var rawBody = await response.Content.ReadAsStringAsync(ct);
        var errorResponse = JsonSerializer.Deserialize<ApiErrorResponse>(rawBody);

        if (errorResponse?.Errors is not null && errorResponse.Errors.Count > 0)
            return OperationResult.Fail(errorResponse.Errors, errorResponse.Title);

        var message = errorResponse?.Detail ?? errorResponse?.Title ?? "Error al crear.";
        return OperationResult.Fail(message);
    }
    catch (OperationCanceledException ex) { /* rethrow */ }
    catch (HttpRequestException ex) { /* return Fail("Error de conexion...") */ }
    catch (Exception ex) { /* return Fail("Error inesperado...") */ }
}
```

#### Method — Update (PUT)

```csharp
public async Task<OperationResult> UpdateEmpleadoAsync(
    int id, EmpleadoUpdateViewModel model, CancellationToken ct = default)
{
    // Same pattern as Create but:
    //   - HttpMethod.Put
    //   - URL: $"api/v1/Empleado/{id}"
    //   - Handle 409 Conflict: return OperationResult.Fail("El registro fue modificado...")
}
```

#### Method — Delete (DELETE)

```csharp
public async Task<OperationResult> DeleteEmpleadoAsync(
    int id, byte[] rowVersion, CancellationToken ct = default)
{
    // Same pattern but:
    //   - HttpMethod.Delete
    //   - URL: $"api/v1/Empleado/{id}"
    //   - Content: JsonContent.Create(new { id, rowVersion })
    //   - Handle 409 Conflict
}
```

### Exception Handling Taxonomy

| Exception | Log Level | Behavior |
|-----------|-----------|----------|
| `OperationCanceledException` | Warning | Re-throw as `OperationCanceledException` |
| `HttpRequestException` | Error | Return null (GET) or `OperationResult.Fail("Error de conexion...")` |
| `Exception` (generic) | Error | Return null (GET) or `OperationResult.Fail("Error inesperado...")` |

## Views

### Index.cshtml — Paginated Table

```razor
@model PaginatedResponse<Empleado>
@{
    ViewData["Title"] = "Empleados";
}

<div class="container">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2 class="mb-0">Empleados</h2>
        <a class="btn btn-primary" asp-action="Create"
           asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)">
            + Agregar Empleado
        </a>
    </div>

    @if (Model?.Items is null || Model.Items.Count == 0)
    {
        <div class="alert alert-info">No se encontraron empleados.</div>
    }
    else
    {
        <div class="table-responsive">
            <table class="table table-striped table-hover align-middle">
                <thead class="table-dark">
                    <tr>
                        <th scope="col">Nombre</th>
                        <th scope="col">Correo Electronico</th>
                        <th scope="col">Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var item in Model.Items)
                    {
                        <tr>
                            <td>@item.StrNombre</td>
                            <td>@item.StrCorreoElectronico</td>
                            <td>
                                <a class="btn btn-sm btn-info" asp-action="Details"
                                   asp-route-id="@item.Id"
                                   asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)">Detalles</a>
                                <a class="btn btn-sm btn-warning" asp-action="Update"
                                   asp-route-id="@item.Id"
                                   asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)">Actualizar</a>
                                <a class="btn btn-sm btn-danger" asp-action="Delete"
                                   asp-route-id="@item.Id"
                                   asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)">Eliminar</a>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>

        <!-- Pagination -->
        <nav aria-label="Navegacion">
            <ul class="pagination justify-content-center">
                @if (Model.PageNumber > 1)
                {
                    <li class="page-item">
                        <a class="page-link" href="/Empleado/Index?pageNumber=@(Model.PageNumber - 1)&amp;pageSize=@Model.PageSize">Anterior</a>
                    </li>
                }
                else
                {
                    <li class="page-item disabled"><span class="page-link">Anterior</span></li>
                }

                @for (int i = 1; i <= Model.TotalPages; i++)
                {
                    <li class="page-item @(i == Model.PageNumber ? "active" : "")">
                        <a class="page-link" href="/Empleado/Index?pageNumber=@i&amp;pageSize=@Model.PageSize">@i</a>
                    </li>
                }

                @if (Model.PageNumber < Model.TotalPages)
                {
                    <li class="page-item">
                        <a class="page-link" href="/Empleado/Index?pageNumber=@(Model.PageNumber + 1)&amp;pageSize=@Model.PageSize">Siguiente</a>
                    </li>
                }
                else
                {
                    <li class="page-item disabled"><span class="page-link">Siguiente</span></li>
                }
            </ul>
        </nav>

        <p class="text-center text-muted small">
            Mostrando pagina @Model.PageNumber de @Model.TotalPages (@Model.TotalCount registros)
        </p>
    }
</div>
```

### Create.cshtml — Form with Validation

```razor
@model EmpleadoCreateViewModel
@{
    ViewData["Title"] = "Agregar Empleado";
}

<div class="container">
    <div class="row justify-content-center">
        <div class="col-md-8 col-lg-6">
            <h2 class="mb-4">Agregar Empleado</h2>

            @{
                var generalErrors = ViewData.ModelState[string.Empty]?.Errors;
            }
            @if (generalErrors is { Count: > 0 })
            {
                <div class="alert alert-danger alert-dismissible fade show" role="alert">
                    @foreach (var error in generalErrors)
                    {
                        <p class="mb-0">@error.ErrorMessage</p>
                    }
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                </div>
            }

            <form method="post" class="needs-validation" novalidate>
                @Html.AntiForgeryToken()
                <input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />

                @foreach (var prop in new[] { "StrNombre", "StrCorreoElectronico" })
                {
                    <div class="mb-3">
                        <label asp-for="@prop" class="form-label"></label>
                        <input asp-for="@prop" class="form-control" autocomplete="off"
                               @(prop == "StrCorreoElectronico" ? @"type=""email""" : "") />
                        <span asp-validation-for="@prop" class="text-danger"></span>
                    </div>
                }

                <div class="d-flex gap-2">
                    <button type="submit" class="btn btn-primary">Guardar</button>
                    <a href="@(ViewData["ReturnUrl"] ?? Url.Action("Index"))" class="btn btn-secondary">Cancelar</a>
                </div>
            </form>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

### Client-Side JS Validation Pattern

Always include in `@section Scripts` after `_ValidationScriptsPartial`:

```javascript
<script nonce="@(ViewContext.HttpContext.Items["ScriptNonce"])">
(function () {
    var nombreInput = document.getElementById('StrNombre');
    if (nombreInput) {
        nombreInput.addEventListener('input', function () {
            var val = nombreInput.value;
            var regex = /^[a-zA-Z0-9_ ]+$/;
            if (val.length > 0 && !regex.test(val)) {
                nombreInput.setCustomValidity('Solo letras, numeros, espacios y guion bajo');
                nombreInput.classList.add('is-invalid');
            } else {
                nombreInput.setCustomValidity('');
                nombreInput.classList.remove('is-invalid');
                if (val.length > 0) nombreInput.classList.add('is-valid');
            }
        });
    }

    var emailInput = document.getElementById('StrCorreoElectronico');
    if (emailInput) {
        emailInput.addEventListener('blur', function () {
            var val = emailInput.value.trim();
            if (val.length > 0) {
                var regex = /^[^\s@@]+@@[^\s@@]+\.[^\s@@]+$/;
                if (!regex.test(val)) {
                    emailInput.setCustomValidity('Formato de correo invalido');
                    emailInput.classList.add('is-invalid');
                } else {
                    emailInput.setCustomValidity('');
                    emailInput.classList.remove('is-invalid');
                    emailInput.classList.add('is-valid');
                }
            }
        });
        emailInput.addEventListener('input', function () {
            emailInput.setCustomValidity('');
            emailInput.classList.remove('is-invalid');
            emailInput.classList.remove('is-valid');
        });
    }

    var form = document.querySelector('form.needs-validation');
    if (form) {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    }
})();
</script>
```

### Details.cshtml — Card Display

```razor
@model EmpleadoDetailsViewModel
@{
    ViewData["Title"] = "Detalles del Empleado";
}

<div class="container">
    <div class="row justify-content-center">
        <div class="col-md-8 col-lg-6">
            <h2 class="mb-4">Detalles del Empleado</h2>

            @* General errors block (same as Create) *@

            <div class="card">
                <div class="card-body">
                    <dl class="row mb-0">
                        <dt class="col-sm-4">@Html.DisplayNameFor(m => m.StrNombre)</dt>
                        <dd class="col-sm-8">@Model.StrNombre</dd>

                        <dt class="col-sm-4">@Html.DisplayNameFor(m => m.StrCorreoElectronico)</dt>
                        <dd class="col-sm-8">@Model.StrCorreoElectronico</dd>

                        <dt class="col-sm-4">@Html.DisplayNameFor(m => m.DteFechaRegistro)</dt>
                        <dd class="col-sm-8">@Model.DteFechaRegistro.ToString("dd/MM/yyyy HH:mm")</dd>
                    </dl>
                </div>
            </div>

            <div class="d-flex gap-2 mt-4">
                <a class="btn btn-primary" asp-action="Update"
                   asp-route-id="@Model.Id"
                   asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)">Editar</a>
                <a href="@(ViewData["ReturnUrl"] ?? Url.Action("Index"))" class="btn btn-secondary">Regresar</a>
            </div>
        </div>
    </div>
</div>
```

### Update.cshtml — Form with Hidden Fields

Same as Create but with:
- `<input asp-for="Id" type="hidden" />`
- `<input asp-for="RowVersion" type="hidden" />`
- Password field label: `"Contrasena (dejar en blanco para mantener)"`
- Submit text: "Guardar"

### Delete.cshtml — Confirmation

```razor
@model EmpleadoDeleteViewModel
@{
    ViewData["Title"] = "Eliminar Empleado";
}

<div class="container">
    <div class="row justify-content-center">
        <div class="col-md-8 col-lg-6">
            <h2 class="mb-4">Eliminar Empleado</h2>

            <div class="alert alert-warning" role="alert">
                <i class="bi bi-exclamation-triangle"></i>
                Esta accion no se puede deshacer. Confirme que desea eliminar permanentemente este empleado.
            </div>

            @* General errors block *@

            <form method="post" class="needs-validation" novalidate>
                @Html.AntiForgeryToken()
                <input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />
                <input asp-for="Id" type="hidden" />
                <input asp-for="RowVersion" type="hidden" />

                <dl class="row mb-4">
                    <dt class="col-sm-4">@Html.DisplayNameFor(m => m.StrNombre)</dt>
                    <dd class="col-sm-8">@Model.StrNombre</dd>

                    <dt class="col-sm-4">@Html.DisplayNameFor(m => m.StrCorreoElectronico)</dt>
                    <dd class="col-sm-8">@Model.StrCorreoElectronico</dd>
                </dl>

                <div class="d-flex gap-2">
                    <button type="submit" class="btn btn-danger">Confirmar Eliminar</button>
                    <a href="@(ViewData["ReturnUrl"] ?? Url.Action("Index"))" class="btn btn-secondary">Cancelar</a>
                </div>
            </form>
        </div>
    </div>
</div>
```

## Menu Integration (`_Layout.cshtml`)

Add inside the `@if (User.Identity?.IsAuthenticated == true)` block, **after** the Usuarios `<li>`:

```html
<li class="nav-item">
    <a class="nav-link text-dark" href="/Empleado/Index">Empleados</a>
</li>
```

## DI Registration (`Program.cs`)

Add after the existing `IUsuarioService` registration:

```csharp
builder.Services.AddHttpClient<IEmpleadoService, EmpleadoService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]
        ?? throw new InvalidOperationException("ApiSettings:BaseUrl is not configured."));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler();
```

## Quick-Start Checklist

When creating a new CRUD module, follow these steps in order:

- [ ] Create `Models/{Resource}.cs` — entity with `[JsonPropertyName]`
- [ ] Create `Models/{Resource}CreateViewModel.cs` — validation attributes
- [ ] Create `Models/{Resource}UpdateViewModel.cs` — add Id, RowVersion, nullable password
- [ ] Create `Models/{Resource}DetailsViewModel.cs` — include DteFechaRegistro
- [ ] Create `Models/{Resource}DeleteViewModel.cs` — display fields + RowVersion
- [ ] Create `Services/I{Resource}Service.cs` — 5 methods
- [ ] Create `Services/{Resource}Service.cs` — HttpClient implementation
- [ ] Create `Controllers/{Resource}Controller.cs` — 8 actions
- [ ] Create `Views/{Resource}/Index.cshtml` — table + pagination
- [ ] Create `Views/{Resource}/Create.cshtml` — form + JS validation
- [ ] Create `Views/{Resource}/Details.cshtml` — card display
- [ ] Create `Views/{Resource}/Update.cshtml` — form + hidden fields
- [ ] Create `Views/{Resource}/Delete.cshtml` — confirmation
- [ ] Add `<li>` to `_Layout.cshtml` menu (after Usuarios)
- [ ] Register `I{Resource}Service` in `Program.cs`
