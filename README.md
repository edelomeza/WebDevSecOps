# WebDevSecOps

**Proyecto Web desarrollado tomando en consideración buenas prácticas de DevSecOps**

Aplicación ASP.NET Core (BFF Pattern) con integración continua de seguridad en cada
etapa del ciclo de desarrollo: análisis estático (SAST), análisis de dependencias (SCA),
análisis dinámico (DAST), escaneo de secretos y contenedores.

---

## Características

- **Autenticación segura** — Cookie-based con BFF pattern (token almacenado server-side)
- **CRUD de usuarios** — Crear, leer, actualizar y eliminar con paginación
- **CRUD de empleados** — Con catálogo TipoEmpleado, búsqueda por texto y filtro por tipo
- **CRUD de clientes** — Con autocomplete para selección en ventas
- **CRUD de productos** — Con autocomplete y validación de existencia (stock)
- **CRUD de ventas con detalle (VentaDetalle)** — Gestión de ventas, asignación de productos con autocomplete, validación de stock, eliminación AJAX, actualización de estado
- **Autocomplete JSON endpoints** — Clientes, Usuarios y Productos con búsqueda asíncrona
- **Catálogos read-only** — TipoEmpleado, EstadoVenta como servicios HttpClient independientes
- **Seguridad por capas** — CSP, HSTS, anti-forgery, rate limiting en login, OWASP headers
- **API remota** — Consume API REST externa con Polly resilience (retry, timeout, circuit breaker)
- **Pipeline DevSecOps** — CI/CD automatizado con análisis de seguridad en cada push
- **Optimistic concurrency** — Control de concurrencia vía RowVersion

---

## Tecnologías

| Capa | Tecnologías |
|---|---|
| **Backend** | ASP.NET Core 10, Razor Pages, MVC Controllers |
| **Frontend** | Bootstrap 5.3.x, jQuery 3.x, Toastr |
| **API Mock** | WireMock |
| **Contenedores** | Docker, Docker Compose |
| **Testing** | xUnit, Moq, Coverlet |
| **CI/CD** | GitHub Actions |
| **SAST** | SecurityCodeScan, SonarAnalyzer, CodeQL |
| **SCA** | NuGet Audit, Snyk |
| **DAST** | OWASP ZAP |
| **Secret Scanning** | Gitleaks |
| **Container Scanning** | Trivy |
| **SBOM** | CycloneDX |

---

## Requisitos previos

- .NET SDK 10.0
- Docker Desktop
- Visual Studio 2022+ / VS Code / Rider

---

## Configuración rápida

```bash
# 1. Restaurar dependencias
dotnet restore

# 2. Compilar
dotnet build

# 3. Ejecutar pruebas
dotnet test
```

---

## Ejecución

La aplicación consume una API REST externa. Configurar la URL base en `appsettings.json`:

```json
"ApiSettings": {
    "BaseUrl": "https://webapidevopsproject-h5fn4.ondigitalocean.app/"
}
```

### Con Docker Compose

```bash
docker compose up webapp
```

Acceder a la aplicación en `http://localhost`.

### Sin Docker

```bash
cd WebDevSecOps
dotnet run --launch-profile https
```

### Desarrollo con API mock (WireMock)

```bash
# Terminal 1 - Iniciar mock API
docker run -p 7227:7227 -v /ruta/a/mappings:/home/wiremock/mappings \
  wiremock/wiremock:latest --port 7227

# Terminal 2 - Iniciar web app (apuntando a localhost)
cd WebDevSecOps
# Cambiar BaseUrl en appsettings.json a "https://localhost:7227/"
dotnet run --launch-profile https
```

---

## Pruebas

```bash
# Todas las pruebas
dotnet test

# Proyecto específico
dotnet test tests/WebDevSecOps.UnitTests
dotnet test tests/WebDevSecOps.IntegrationTests
dotnet test tests/WebDevSecOps.SecurityTests
dotnet test tests/WebDevSecOps.E2E
```

---

## Pipeline DevSecOps

El pipeline automatizado (GitHub Actions) ejecuta en paralelo:

| Job | Herramienta | Propósito |
|---|---|---|
| Build + SonarCloud | `dotnet build`, SonarScanner | Compilación y calidad de código |
| CodeQL | GitHub CodeQL | Análisis de vulnerabilidades en el código |
| SAST | SecurityCodeScan, Roslyn analyzers | Reglas de seguridad estáticas |
| SCA | `dotnet list package --vulnerable`, Snyk | Vulnerabilidades en dependencias |
| Secret Scanning | Gitleaks | Credenciales y secretos hardcodeados |
| Container Scanning | Trivy | Vulnerabilidades en imagen Docker |
| DAST | OWASP ZAP | Pruebas de penetración automatizadas |
| SBOM | CycloneDX | Generación de SBOM en releases |

---

## Reportar vulnerabilidades

Si encuentras una vulnerabilidad de seguridad, por favor consulta [SECURITY.md](.github/SECURITY.md)
para las instrucciones de reporte responsable. **No abras issues públicos.**

---

## Estructura del proyecto

```
WebDevSecOps/
├── .github/                  # CI/CD, dependabot, security policy
├── WebDevSecOps/             # Aplicación principal
│   ├── Controllers/          # 5 controladores MVC
│   │   ├── UsuarioController.cs
│   │   ├── EmpleadoController.cs
│   │   ├── ClienteController.cs
│   │   ├── ProductoController.cs
│   │   └── VentaController.cs
│   ├── Models/               # Modelos, ViewModels y DTOs
│   │   ├── Usuario/          # Usuario + 5 VMs + AutocompleteDto
│   │   ├── Empleado/         # Empleado + 4 VMs + EmpCatTipoEmpleado
│   │   ├── Cliente/          # Cliente + 4 VMs + AutocompleteDto
│   │   ├── Producto/         # Producto + 4 VMs + AutocompleteDto
│   │   ├── Venta/            # Venta, VentaDetalle, VenCatEstado + 3 VMs
│   │   ├── ApiErrorResponse, OperationResult, PaginatedResponse
│   │   └── Auth (LoginRequest, LoginResponse)
│   ├── Pages/                # Razor Pages (Login, Home, Layout)
│   ├── Services/             # 8 pares interfaz/implementación + TokenStore
│   │   ├── AuthService       # Login/logout con BFF pattern
│   │   ├── UsuarioService    # CRUD usuarios
│   │   ├── EmpleadoService   # CRUD empleados + búsqueda
│   │   ├── ClienteService    # CRUD clientes + autocomplete
│   │   ├── ProductoService   # CRUD productos + autocomplete + stock
│   │   ├── VentaService      # CRUD ventas + VentaDetalle
│   │   ├── EstadoVentaService# Catálogo read-only
│   │   └── TipoEmpleadoService# Catálogo read-only
│   ├── Views/                # 5 módulos con 4-5 vistas cada uno
│   │   ├── Usuario/          # Index, Create, Details, Update, Delete
│   │   ├── Empleado/         # Index, Create, Details, Update, Delete
│   │   ├── Cliente/          # Index, Create, Details, Update, Delete
│   │   ├── Producto/         # Index, Create, Details, Update, Delete
│   │   ├── Venta/            # Index, Create, Productos
│   │   └── Shared/           # _Layout.cshtml, _ValidationScriptsPartial
│   └── wwwroot/              # Estáticos (CSS, JS, libs)
├── tests/
│   ├── WebDevSecOps.UnitTests/
│   │   ├── Pages/            # Tests por módulo (Empleados, Login, Productos,
│   │   │                     #   Usuarios, Ventas)
│   │   └── Common/           # TestData, MockHttpMessageHandler, TestConstants
│   ├── WebDevSecOps.IntegrationTests/
│   │   └── Services/         # Tests de API Client por servicio
│   ├── WebDevSecOps.SecurityTests/
│   │   ├── SAST/             # Pruebas de seguridad estáticas
│   │   ├── DAST/             # Pruebas dinámicas
│   │   ├── SCA/              # Análisis de dependencias
│   │   └── SecretScanning/   # Escaneo de secretos
│   ├── WebDevSecOps.E2E/
│   └── MemoriaPruebas.md    # Lecciones aprendidas en testing
├── docker/                   # Configuración de contenedores
├── docker-compose.yml
└── AGENTS.md                 # Memoria del proyecto (convenciones, comandos)
```

---

## Módulos CRUD

| Módulo | Controller | Servicios | Vistas | Tests |
|--------|-----------|-----------|--------|-------|
| **Usuario** | `UsuarioController.cs` | `IUsuarioService` | 5 vistas | Unit, Integration, Security |
| **Empleado** | `EmpleadoController.cs` | `IEmpleadoService`, `ITipoEmpleadoService` | 5 vistas | Unit, Integration, Security |
| **Cliente** | `ClienteController.cs` | `IClienteService` | 5 vistas + Memoria.md | — |
| **Producto** | `ProductoController.cs` | `IProductoService` | 5 vistas | Unit, Integration, Security |
| **Venta** | `VentaController.cs` | `IVentaService`, `IEstadoVentaService` | 3 vistas + Productos + Memoria.md | Unit, Integration, Security |

### Endpoints API consumidos

| Módulo | Endpoint | Método |
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

## Licencia

Proyecto educativo con fines de demostración DevSecOps.
