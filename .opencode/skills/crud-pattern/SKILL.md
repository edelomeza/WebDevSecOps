# Skill: crud-pattern

# CRUD Module Implementation Pattern

Guía paso a paso para implementar un nuevo módulo CRUD en este proyecto, siguiendo el patrón establecido por `Usuario` y `Empleado`.

## Orden de creación

```
 1. Models/        → Modelo de dominio + ViewModels por acción + OperationResult (si no existe)
 2. Services/      → Interfaz + Implementación (HttpClient)
 3. Program.cs     → Registro DI con AddHttpClient + AddStandardResilienceHandler
 4. Controllers/   → Controlador con 8 acciones
 5. Views/         → Vistas Razor (Index, Create, Details, Update, Delete)
 6. _Layout.cshtml → Ítem de menú
 7. Unit tests     → IndexTests, CreateTests, UpdateTests, DeleteTests
 8. Integration tests → ApiClientTests
 9. Security tests → Auth + validación + XSS
```

---

## 1. Modelos (`Models/`)

### Modelo de dominio
Clase plana que refleja la respuesta de la API.

```csharp
namespace WebDevSecOps.Models;

public class MiEntidad
{
    public int Id { get; set; }
    public string StrNombre { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}
```

### ViewModels (uno por acción)
- `{Entidad}CreateViewModel.cs` — solo campos editables en creación
- `{Entidad}UpdateViewModel.cs` — mismos campos + `Id` oculto
- `{Entidad}DetailsViewModel.cs` — campos de solo lectura + `StrValorCatalogo` para catálogos
- `{Entidad}DeleteViewModel.cs` — confirmación + `RowVersion` (byte[]) para concurrencia

```csharp
public class MiEntidadCreateViewModel
{
    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre")]
    public string StrNombre { get; set; } = string.Empty;
}
```

```csharp
public class MiEntidadDeleteViewModel
{
    [HiddenInput]
    public int Id { get; set; }

    [HiddenInput]
    public byte[] RowVersion { get; set; } = [];
}
```

### OperationResult
```csharp
namespace WebDevSecOps.Models;

public class OperationResult
{
    public bool Success { get; protected set; }
    public string Message { get; protected set; } = string.Empty;

    public static OperationResult Ok(string message = "Operación exitosa!!") => new() { Success = true, Message = message };
    public static OperationResult Fail(string message) => new() { Success = false, Message = message };
}

public class OperationResult<T> : OperationResult
{
    public T? Data { get; private set; }

    public static OperationResult<T> Ok(T data, string message = "Operación exitosa!!") => new() { Success = true, Message = message, Data = data };
    public static new OperationResult<T> Fail(string message) => new() { Success = false, Message = message };
}
```

---

## 2. Servicios (`Services/`)

### Interfaz
```csharp
public interface IMiEntidadService
{
    Task<PaginatedResponse<MiEntidad>?> GetMiEntidadesAsync(int pageNumber, int pageSize, CancellationToken ct);
    Task<MiEntidad?> GetMiEntidadByIdAsync(int id, CancellationToken ct);
    Task<OperationResult> CreateMiEntidadAsync(MiEntidadCreateViewModel model, CancellationToken ct);
    Task<OperationResult> UpdateMiEntidadAsync(int id, MiEntidadUpdateViewModel model, CancellationToken ct);
    Task<OperationResult> DeleteMiEntidadAsync(int id, byte[] rowVersion, CancellationToken ct);
    Task<PaginatedResponse<MiEntidad>?> SearchMiEntidadesAsync(string? texto, int? idCatalogo, int pageNumber, int pageSize, CancellationToken ct);
}
```

### Implementación
```csharp
public class MiEntidadService : IMiEntidadService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MiEntidadService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private void AttachBearerToken(HttpRequestMessage request)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<PaginatedResponse<MiEntidad>?> GetMiEntidadesAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/MiEntidad?PageNumber={pageNumber}&PageSize={pageSize}");
        AttachBearerToken(request);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaginatedResponse<MiEntidad>>(cancellationToken: ct);
    }

    // ... resto de métodos siguiendo el mismo patrón
}
```

### Servicio de catálogo (read-only)
Si el CRUD necesita un dropdown desde un catálogo:

```csharp
public interface IMiCatalogoService
{
    Task<PaginatedResponse<MiCatalogo>?> GetAllAsync(int pageNumber, int pageSize, CancellationToken ct);
    Task<MiCatalogo?> GetByIdAsync(int id, CancellationToken ct);
}
```

---

## 3. Registro en `Program.cs`

```csharp
builder.Services.AddHttpClient<IMiEntidadService, MiEntidadService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7227/");
}).AddStandardResilienceHandler();

builder.Services.AddHttpClient<IMiCatalogoService, MiCatalogoService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7227/");
}).AddStandardResilienceHandler();
```

Colocar después de los registros existentes de UsuarioService.

---

## 4. Controlador (`Controllers/{Entidad}Controller.cs`)

### 8 acciones

| Verbo | Ruta | Propósito |
|-------|------|-----------|
| GET | `Index?texto=&idTipoEmpleado=&pageNumber=&pageSize=` | Lista paginada con filtros |
| GET | `Create` | Formulario de creación |
| POST | `Create` | Procesar creación |
| GET | `Details/{id}` | Vista de solo lectura |
| GET | `Update/{id}` | Formulario de edición |
| POST | `Update` | Procesar edición |
| GET | `Delete/{id}` | Confirmación de eliminación |
| POST | `Delete` | Procesar eliminación |

### Filtrado en Index
```csharp
[HttpGet]
[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, NoStore = false)]
public async Task<IActionResult> Index(string? texto = null, int? idTipoEmpleado = null, int pageNumber = 1, int pageSize = 10, CancellationToken ct = default)
{
    await CargarCatalogosAsync(ct);

    PaginatedResponse<MiEntidad>? result;

    if (!string.IsNullOrWhiteSpace(texto) || idTipoEmpleado.HasValue)
        result = await _service.SearchMiEntidadesAsync(texto, idTipoEmpleado, pageNumber, pageSize, ct);
    else
        result = await _service.GetMiEntidadesAsync(pageNumber, pageSize, ct);

    if (result is null)
    {
        _logger.LogWarning("Failed to load entidades");
        TempData["Error"] = "No se pudieron cargar los registros.";
    }

    return View(result);
}
```

### Dropdown helper
```csharp
private async Task CargarCatalogosAsync(CancellationToken ct)
{
    var catalogos = await _catalogoService.GetAllAsync(1, 100, ct);
    if (catalogos?.Items is not null)
    {
        ViewBag.CatalogoList = new SelectList(catalogos.Items, "Id", "StrValor");
        ViewBag.CatalogoDict = catalogos.Items.ToDictionary(c => c.Id, c => c.StrValor);
    }
}
```

### POST actions — patrón general
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(MiEntidadCreateViewModel model, CancellationToken ct, string? returnUrl = null)
{
    if (!ModelState.IsValid)
    {
        await CargarCatalogosAsync(ct);
        return View(model);
    }

    var result = await _service.CreateMiEntidadAsync(model, ct);

    if (!result.Success)
    {
        ModelState.AddModelError("", result.Message);
        await CargarCatalogosAsync(ct);
        return View(model);
    }

    TempData["Success"] = result.Message;

    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        return Redirect(returnUrl);

    return RedirectToAction(nameof(Index));
}
```

---

## 5. Vistas (`Views/{Entidad}/`)

### Index.cshtml — Lista paginada con filtros
```html
@model PaginatedResponse<MiEntidad>

<form method="get" class="row g-3 mb-3">
    <div class="col-md-4">
        <input type="text" name="texto" class="form-control" placeholder="Buscar..." value="@Context.Request.Query["texto"]" />
    </div>
    <div class="col-md-3">
        <select name="idTipoEmpleado" class="form-select" asp-items="ViewBag.CatalogoList">
            <option value="">— Todos —</option>
        </select>
    </div>
    <div class="col-md-2">
        <button type="submit" class="btn btn-primary">Buscar</button>
        <a asp-action="Index" class="btn btn-outline-secondary">Limpiar</a>
    </div>
    <div class="col-md-3 text-end">
        <a asp-action="Create" class="btn btn-success">Nuevo</a>
    </div>
</form>

<table class="table">
    <thead>
        <tr>
            <th>Nombre</th>
            <th>Catálogo</th>
            <th>Acciones</th>
        </tr>
    </thead>
    <tbody>
        @if (Model?.Items is not null)
        {
            @foreach (var item in Model.Items)
            {
                <tr>
                    <td>@item.StrNombre</td>
                    <td>@(ViewBag.CatalogoDict?[item.IdTipoEmpleado] ?? "")</td>
                    <td>
                        <a asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-info">Detalles</a>
                        <a asp-action="Update" asp-route-id="@item.Id" class="btn btn-sm btn-warning">Editar</a>
                        <a asp-action="Delete" asp-route-id="@item.Id" class="btn btn-sm btn-danger">Eliminar</a>
                    </td>
                </tr>
            }
        }
    </tbody>
</table>

@if (Model is not null)
{
    <nav>
        <ul class="pagination">
            @for (int i = 1; i <= Model.TotalPages; i++)
            {
                <li class="page-item @(i == Model.PageNumber ? "active" : "")">
                    <a class="page-link" asp-action="Index" asp-route-pageNumber="@i" asp-route-texto="@Context.Request.Query["texto"]" asp-route-idTipoEmpleado="@Context.Request.Query["idTipoEmpleado"]">@i</a>
                </li>
            }
        </ul>
    </nav>
}
```

### Create.cshtml / Update.cshtml
```html
@model MiEntidadCreateViewModel

<div asp-validation-summary="ModelOnly" class="alert alert-danger" role="alert"></div>

<form method="post">
    @Html.AntiForgeryToken()

    <div class="mb-3">
        <label asp-for="StrNombre" class="form-label"></label>
        <input asp-for="StrNombre" class="form-control" />
        <span asp-validation-for="StrNombre" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="IdTipoEmpleado" class="form-label"></label>
        <select asp-for="IdTipoEmpleado" class="form-select" asp-items="ViewBag.CatalogoList">
            <option value="">— Seleccione —</option>
        </select>
        <span asp-validation-for="IdTipoEmpleado" class="text-danger"></span>
    </div>

    <button type="submit" class="btn btn-primary">Guardar</button>
    <a asp-action="Index" class="btn btn-outline-secondary">Cancelar</a>
</form>
```

### Delete.cshtml
```html
@model MiEntidadDeleteViewModel

<p>¿Está seguro de eliminar <strong>@Model.StrNombre</strong>?</p>

<form method="post">
    @Html.AntiForgeryToken()
    <input type="hidden" asp-for="Id" />
    <input type="hidden" asp-for="RowVersion" />

    <button type="submit" class="btn btn-danger">Eliminar</button>
    <a asp-action="Index" class="btn btn-outline-secondary">Cancelar</a>
</form>
```

---

## 6. Menú en `_Layout.cshtml`

Agregar después del ítem "Usuarios":

```html
<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-controller="MiEntidad" asp-action="Index">MiEntidad</a>
</li>
```

---

## 7. Pruebas

### Unit tests — datos de prueba (`ContractTestData.cs`)
```csharp
public static readonly PaginatedResponse<MiEntidad> ValidMiEntidadPaginatedResponse = new()
{
    Items = [ValidMiEntidad],
    PageNumber = 1,
    PageSize = 10,
    TotalCount = 1,
    TotalPages = 1
};

public static readonly MiEntidad ValidMiEntidad = new()
{
    Id = 1,
    StrNombre = "Ejemplo",
    RowVersion = [0x01, 0x02, 0x03, 0x04]
};

public static readonly PaginatedResponse<MiCatalogo> ValidCatalogoPaginatedResponse = new()
{
    Items = [ValidCatalogo],
    TotalCount = 1
};

public static readonly MiCatalogo ValidCatalogo = new()
{
    Id = 1,
    StrValor = "Opción 1"
};
```

### Integration tests
Usar WireMock para simular la API. Ejemplo:

```csharp
public class MiEntidadApiClientTests : IClassFixture<WireMockFixture>
{
    private readonly WireMockFixture _wireMock;
    private readonly MiEntidadService _service;

    public MiEntidadApiClientTests(WireMockFixture wireMock)
    {
        _wireMock = wireMock;
        var httpContextAccessor = Mock.Of<IHttpContextAccessor>();
        var httpClient = wireMock.CreateClient();
        _service = new MiEntidadService(httpClient, httpContextAccessor);
    }

    [Fact]
    public async Task GetMiEntidadesAsync_ReturnsData()
    {
        _wireMock.Server.Given(
            RequestBuilder.Create().WithPath("/api/v1/MiEntidad").UsingGet()
        ).RespondWith(
            ResponseBuilder.Create().WithStatusCode(200).WithBodyAsJson(ContractTestData.ValidMiEntidadPaginatedResponse)
        );

        var result = await _service.GetMiEntidadesAsync(1, 10, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Single(result.Items);
    }
}
```

### Security tests
```csharp
public class MiEntidadSecurityTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IAuthService>();
                    services.AddSingleton<IAuthService>(new FakeAuthService());
                    services.PostConfigure<CookieAuthenticationOptions>(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);
                });
            });
    }

    [Fact]
    public async Task Index_RequiresAuthentication()
    {
        var (factory, client) = CreateUnauthenticatedClient();
        using (factory) using (client)
        {
            var (status, _) = await SecurityTestHelpers.GetAsync(client, "/MiEntidad/Index");
            Assert.Equal((int)HttpStatusCode.Redirect, status);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Create_RejectsEmptyNombre(string? nombre)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(...);
        await AuthenticateAsync(client);
        var token = await SecurityTestHelpers.GetAntiForgeryTokenAsync(client, "/MiEntidad/Create");
        var form = SecurityTestHelpers.ToFormPayload(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token },
            { "StrNombre", nombre ?? "" },
        });
        var (status, body) = await SecurityTestHelpers.PostAsync(client, "/MiEntidad/Create", form);
        Assert.Equal((int)HttpStatusCode.OK, status);
        Assert.Contains("obligatorio", body);
    }

    [Theory]
    [MemberData(nameof(SecurityTestData.XssPayloads.All), MemberType = typeof(SecurityTestData.XssPayloads))]
    public async Task Create_RejectsXssInNombre(string xssPayload)
    {
        // mismo patrón que arriba, enviar xssPayload en StrNombre
    }
}
```

### Comandos
```powershell
dotnet build
dotnet test tests\WebDevSecOps.UnitTests --filter "FullyQualifiedName~MiEntidad"
dotnet test tests\WebDevSecOps.IntegrationTests --filter "FullyQualifiedName~MiEntidad"
dotnet test tests\WebDevSecOps.SecurityTests --filter "FullyQualifiedName~MiEntidad"
```

---

## Convenciones clave

| Concepto | Detalle |
|----------|---------|
| Naming | `Str` prefijo para strings, `Async` sufijo, `ct` para CancellationToken |
| TempData | `TempData["Success"]`, `TempData["Error"]`, `TempData["Warning"]` |
| OperationResult | Usar `OperationResult<T>` genérico, no crear tipos resultado específicos |
| Concurrencia | `RowVersion` (byte[]) en DeleteViewModel, pasado al DELETE de la API |
| ReturnUrl | Parámetro opcional en POST, validar con `Url.IsLocalUrl()` |
| Catálogos | `ViewBag.{Nombre}List` (SelectList) + `ViewBag.{Nombre}Dict` (Dictionary), pageSize=100 |
| Paginación | Default pageNumber=1, pageSize=10 |
| Filtros | GET form, si hay filtros → `SearchAsync`, si no → `GetAllAsync` |
| Logger | `_logger.LogWarning(...)` cuando la API retorna null |
