# WebDevSecOps

**Web project developed with DevSecOps best practices**

ASP.NET Core (BFF Pattern) application with continuous security integration
at every stage of the development lifecycle: static analysis (SAST), dependency
analysis (SCA), dynamic analysis (DAST), secret and container scanning.

---

## Features

- **Secure Authentication** — Cookie-based with BFF pattern (server-side token storage)
- **CRUD Usuarios** — Create, read, update, delete with pagination
- **CRUD Empleados** — With TipoEmpleado catalog, text search and type filter
- **CRUD Clientes** — With autocomplete for sales selection
- **CRUD Productos** — With autocomplete and stock validation
- **CRUD Ventas with detail (VentaDetalle)** — Sales management, product assignment with autocomplete, stock validation, AJAX delete, status update
- **Autocomplete JSON endpoints** — Clients, Users and Products with async search
- **Read-only catalogs** — TipoEmpleado, EstadoVenta as independent HttpClient services
- **Layered security** — CSP, HSTS, anti-forgery, rate limiting on login, OWASP headers
- **Remote API** — Consumes external REST API with Polly resilience (retry, timeout, circuit breaker)
- **DevSecOps Pipeline** — Automated CI/CD with security analysis on every push
- **Optimistic concurrency** — Concurrency control via RowVersion

---

## Technologies

| Layer | Technologies |
|---|---|
| **Backend** | ASP.NET Core 10, Razor Pages, MVC Controllers |
| **Frontend** | Bootstrap 5.3.x, jQuery 3.x, Toastr |
| **API Mock** | WireMock |
| **Containers** | Docker, Docker Compose |
| **Testing** | xUnit, Moq, Coverlet |
| **CI/CD** | GitHub Actions |
| **SAST** | SecurityCodeScan, SonarAnalyzer, CodeQL |
| **SCA** | NuGet Audit, Snyk |
| **DAST** | OWASP ZAP |
| **Secret Scanning** | Gitleaks |
| **Container Scanning** | Trivy |
| **SBOM** | CycloneDX |

---

## Prerequisites

- .NET SDK 10.0
- Docker Desktop
- Visual Studio 2022+ / VS Code / Rider

---

## Quick Setup

```bash
# 1. Restore dependencies
dotnet restore

# 2. Build
dotnet build

# 3. Run tests
dotnet test
```

---

## Running

The application consumes an external REST API. Configure the base URL in `appsettings.json`:

```json
"ApiSettings": {
    "BaseUrl": "https://webapidevopsproject-h5fn4.ondigitalocean.app/"
}
```

### With Docker Compose

```bash
docker compose up webapp
```

Access the application at `http://localhost`.

### Without Docker

```bash
cd WebDevSecOps
dotnet run --launch-profile https
```

### Development with mock API (WireMock)

```bash
# Terminal 1 - Start mock API
docker run -p 7227:7227 -v /path/to/mappings:/home/wiremock/mappings \
  wiremock/wiremock:latest --port 7227

# Terminal 2 - Start web app (pointing to localhost)
cd WebDevSecOps
# Change BaseUrl in appsettings.json to "https://localhost:7227/"
dotnet run --launch-profile https
```

---

## Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/WebDevSecOps.UnitTests
dotnet test tests/WebDevSecOps.IntegrationTests
dotnet test tests/WebDevSecOps.SecurityTests
dotnet test tests/WebDevSecOps.E2E
```

---

## DevSecOps Pipeline

The automated pipeline (GitHub Actions) runs in parallel:

| Job | Tool | Purpose |
|---|---|---|
| Build + SonarCloud | `dotnet build`, SonarScanner | Build and code quality |
| CodeQL | GitHub CodeQL | Code vulnerability analysis |
| SAST | SecurityCodeScan, Roslyn analyzers | Static security rules |
| SCA | `dotnet list package --vulnerable`, Snyk | Dependency vulnerabilities |
| Secret Scanning | Gitleaks | Hardcoded credentials and secrets |
| Container Scanning | Trivy | Docker image vulnerabilities |
| DAST | OWASP ZAP | Automated penetration testing |
| SBOM | CycloneDX | SBOM generation in releases |

---

## Reporting Vulnerabilities

If you find a security vulnerability, please check [SECURITY.md](.github/SECURITY.md)
for responsible disclosure instructions. **Do not open public issues.**

---

## Project Structure

```
WebDevSecOps/
├── .github/                  # CI/CD, dependabot, security policy
├── WebDevSecOps/             # Main application
│   ├── Controllers/          # 5 MVC controllers
│   │   ├── UsuarioController.cs
│   │   ├── EmpleadoController.cs
│   │   ├── ClienteController.cs
│   │   ├── ProductoController.cs
│   │   └── VentaController.cs
│   ├── Models/               # Models, ViewModels and DTOs
│   │   ├── Usuario/          # Usuario + 5 VMs + AutocompleteDto
│   │   ├── Empleado/         # Empleado + 4 VMs + EmpCatTipoEmpleado
│   │   ├── Cliente/          # Cliente + 4 VMs + AutocompleteDto
│   │   ├── Producto/         # Producto + 4 VMs + AutocompleteDto
│   │   ├── Venta/            # Venta, VentaDetalle, VenCatEstado + 3 VMs
│   │   ├── ApiErrorResponse, OperationResult, PaginatedResponse
│   │   └── Auth (LoginRequest, LoginResponse)
│   ├── Pages/                # Razor Pages (Login, Home, Layout)
│   ├── Services/             # 8 interface/implementation pairs + TokenStore
│   │   ├── AuthService       # Login/logout with BFF pattern
│   │   ├── UsuarioService    # CRUD usuarios
│   │   ├── EmpleadoService   # CRUD empleados + search
│   │   ├── ClienteService    # CRUD clientes + autocomplete
│   │   ├── ProductoService   # CRUD productos + autocomplete + stock
│   │   ├── VentaService      # CRUD ventas + VentaDetalle
│   │   ├── EstadoVentaService# Read-only catalog
│   │   └── TipoEmpleadoService# Read-only catalog
│   ├── Views/                # 5 modules with 4-5 views each
│   │   ├── Usuario/          # Index, Create, Details, Update, Delete
│   │   ├── Empleado/         # Index, Create, Details, Update, Delete
│   │   ├── Cliente/          # Index, Create, Details, Update, Delete
│   │   ├── Producto/         # Index, Create, Details, Update, Delete
│   │   ├── Venta/            # Index, Create, Productos
│   │   └── Shared/           # _Layout.cshtml, _ValidationScriptsPartial
│   └── wwwroot/              # Static files (CSS, JS, libs)
├── tests/
│   ├── WebDevSecOps.UnitTests/
│   │   ├── Pages/            # Tests per module (Empleados, Login, Productos,
│   │   │                     #   Usuarios, Ventas)
│   │   └── Common/           # TestData, MockHttpMessageHandler, TestConstants
│   ├── WebDevSecOps.IntegrationTests/
│   │   └── Services/         # API Client tests per service
│   ├── WebDevSecOps.SecurityTests/
│   │   ├── SAST/             # Static security tests
│   │   ├── DAST/             # Dynamic tests
│   │   ├── SCA/              # Dependency analysis
│   │   └── SecretScanning/   # Secret scanning
│   ├── WebDevSecOps.E2E/
│   └── MemoriaPruebas.md    # Testing lessons learned
├── docker/                   # Container configuration
├── docker-compose.yml
└── AGENTS.md                 # Project memory (conventions, commands)
```

---

## CRUD Modules

| Module | Controller | Services | Views | Tests |
|--------|-----------|----------|-------|-------|
| **Usuario** | `UsuarioController.cs` | `IUsuarioService` | 5 views | Unit, Integration, Security |
| **Empleado** | `EmpleadoController.cs` | `IEmpleadoService`, `ITipoEmpleadoService` | 5 views | Unit, Integration, Security |
| **Cliente** | `ClienteController.cs` | `IClienteService` | 5 views + Memoria.md | — |
| **Producto** | `ProductoController.cs` | `IProductoService` | 5 views | Unit, Integration, Security |
| **Venta** | `VentaController.cs` | `IVentaService`, `IEstadoVentaService` | 3 views + Productos + Memoria.md | Unit, Integration, Security |

### Consumed API Endpoints

| Module | Endpoint | Method |
|--------|----------|--------|
| **Auth** | `/api/v1/Login/login` | POST |
| **Usuario** | `/api/v1/Usuario` | GET, POST |
| | `/api/v1/Usuario/{id}` | GET, PUT, DELETE |
| | `/api/v1/Usuario/buscar?texto=&strValor=&pageNumber=&pageSize=` | GET |
| **Empleado** | `/api/v1/Empleado` | GET, POST |
| | `/api/v1/Empleado/{id}` | GET, PUT, DELETE |
| | `/api/v1/Empleado/buscar?texto=&idTipoEmpleado=` | GET |
| | `/api/v1/TipoEmpleado` | GET |
| | `/api/v1/TipoEmpleado/{id}` | GET |
| **Cliente** | `/api/v1/Cliente` | GET, POST |
| | `/api/v1/Cliente/{id}` | GET, PUT, DELETE |
| | `/api/v1/Cliente/buscar?texto=` | GET |
| | `/api/v1/Cliente/autocomplete?texto=` | GET |
| **Producto** | `/api/v1/Producto` | GET, POST |
| | `/api/v1/Producto/{id}` | GET, PUT, DELETE |
| | `/api/v1/Producto/buscar?texto=` | GET |
| **Venta** | `/api/v1/Venta` | GET, POST |
| | `/api/v1/Venta/{id}` | GET, PUT |
| | `/api/v1/Venta/buscar?strClaveVenta=&strNombreCliente=&dteFechaInicio=&dteFechaFin=` | GET |
| | `/api/v1/ventadetalle?idVenVenta=` | GET |
| | `/api/v1/ventadetalle` | POST |
| | `/api/v1/ventadetalle/{id}` | DELETE |
| | `/api/v1/ventadetalle/autocomplete?texto=` | GET |
| | `/api/v1/EstadoVenta` | GET |
| | `/api/v1/EstadoVenta/{id}` | GET |

---

## License

Educational project for DevSecOps demonstration purposes.
