# WebDevSecOps

**Proyecto Web desarrollado tomando en consideración buenas prácticas de DevSecOps**

Aplicación ASP.NET Core (BFF Pattern) para gestión de usuarios con integración continua
de seguridad en cada etapa del ciclo de desarrollo: análisis estático (SAST), análisis
de dependencias (SCA), análisis dinámico (DAST), escaneo de secretos y contenedores.

---

## Características

- **Autenticación segura** — Cookie-based con BFF pattern (token almacenado server-side)
- **CRUD de usuarios** — Crear, leer, actualizar y eliminar con paginación
- **Seguridad por capas** — CSP, HSTS, anti-forgery, rate limiting en login, OWASP headers
- **API mockeada** — WireMock para desarrollo y pruebas sin backend real
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

### Con Docker Compose (recomendado)

```bash
docker compose up webapp mock-api
```

Acceder a la aplicación en `http://localhost`.

### Sin Docker

```bash
# Terminal 1 - Iniciar mock API
docker run -p 7227:7227 -v /ruta/a/mappings:/home/wiremock/mappings \
  wiremock/wiremock:latest --port 7227

# Terminal 2 - Iniciar web app
cd WebDevSecOps
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
├── .github/              # CI/CD, dependabot, security policy
├── WebDevSecOps/         # Aplicación principal
│   ├── Controllers/      # Controladores MVC
│   ├── Models/           # Modelos y ViewModels
│   ├── Pages/            # Razor Pages (Login, Home, etc.)
│   ├── Services/         # Servicios de negocio y API Client
│   ├── Views/            # Vistas del CRUD de usuarios
│   └── wwwroot/          # Estáticos (CSS, JS, libs)
├── tests/                # Proyectos de prueba
│   ├── UnitTests/
│   ├── IntegrationTests/
│   ├── SecurityTests/
│   └── E2E/
├── docker/               # Configuración de contenedores
└── docker-compose.yml
```

---

## Licencia

Proyecto educativo con fines de demostración DevSecOps.
