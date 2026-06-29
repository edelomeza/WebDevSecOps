# Memoria de Modificaciones — WebDevSecOps

## Commits Realizados (9)

### Código — Correcciones SonarQube

| Commit | Regla | Archivos | Cambio |
|--------|-------|----------|--------|
| `d780035` | S6964 | `UsuarioUpdateViewModel.cs`, `UsuarioDeleteViewModel.cs` | Agregar `[JsonRequired]` junto a `[Required]` en propiedades `Id` |
| | S6966 | `Program.cs` | `app.Run()` → `await app.RunAsync()` |
| | S6667 | `UsuarioService.cs`, `AuthService.cs` | Pasar `ex` como parámetro en `_logger.LogWarning()` dentro de `catch (OperationCanceledException)` |
| | S6967 | `UsuarioController.cs` | Agregar `if (!ModelState.IsValid) return BadRequest(ModelState)` en acciones GET |
| | S1481 | `LoginTest.cs`, `LogoutApiClientTests.cs`, `UsuarioApiClientTests.cs` | Inline de variables `handler` no usadas y discard `_` en tuplas |
| | S1144 | `UsuarioApiClientTests.cs` | Eliminar campo `_loggerMock` no utilizado |
| | S2094 | 3 clases vacías | Eliminar `UsuarioPage.cs`, `UsuarioCrudE2ETests.cs`, `UsuarioCrudFlowTests.cs` |
| | S1186 | `Privacy.cshtml.cs` | Agregar comentario interno en método `OnGet()` |
| `5af1147` | S2139 | `UsuarioService.cs`, `AuthService.cs` | `throw;` → `throw new OperationCanceledException(msg, ex)` en 5 catch blocks |

### Pipeline (.github/workflows/ci.yml)

| Commit | Problema | Cambio |
|--------|----------|--------|
| `f9ab818` | `secrets.SONAR_TOKEN` vacío causa error de formato | Agregar `if: secrets.SONAR_TOKEN != ''` en Begin SonarCloud |
| `668fbb8` | `secrets` no es accesible en `if:` de GA | Cambiar a `if: env.SONAR_TOKEN != ''` con `env: SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}` |
| `f85bf52` | `snyk/actions@v4` no existe | → `@v2` (falló) |
| `47d46f9` | `snyk/actions@v2` no existe | → `@v1.0.0` |
| `f69b7fd` | Redundancia y falta de guard en Snyk | → `@v1`, agregar `if: env.SNYK_TOKEN != ''`, quitar `\|\| true` |
| `8b3f07a` | `trivy-action@v0.28.0` depende de `setup-trivy@v0.2.1` inexistente | → `@v0.36.0` |

### Dockerfile

| Commit | Problema | Cambio |
|--------|----------|--------|
| `439d33b` | Solo copiaba 1 `.csproj` a ruta plana; restore fallaba | Copiar los 5 `.csproj` con sus rutas relativas + `dotnet publish` sobre proyecto específico |

---

## Buenas Prácticas Aplicadas

### Pipeline (ci.yml)

| Práctica | Implementación |
|----------|---------------|
| **Guard condicional con `env`** | `if: env.TOKEN != ''` + `env: TOKEN: ${{ secrets.TOKEN }}` (única forma válida en GA) |
| **Tokens opcionales** | Steps de SonarCloud y Snyk se saltan si el token no existe |
| **Sin redundancia** | Usar solo `continue-on-error: true`, no combinar con `\|\| true` |
| **Major version pinning** | `@v1` en vez de `@v1.0.0` para recibir parches automáticos |
| **Versiones actualizadas** | `trivy-action@v0.36.0`, `snyk/actions/setup@v1` |

### Dockerfile

| Práctica | Implementación |
|----------|---------------|
| **Layer caching** | Copiar todos los `.csproj` antes de `RUN dotnet restore` |
| **Rutas relativas** | Cada `.csproj` mantiene su estructura de directorios dentro del contenedor |
| **Publicar proyecto específico** | `dotnet publish WebDevSecOps/WebDevSecOps.csproj` (no toda la solución) |
| **Multi-stage build** | Imagen final solo contiene el publish, sin SDK ni código fuente |

### Código C#

| Práctica | Implementación |
|----------|---------------|
| **Excepciones con contexto** | `throw new OperationCanceledException(msg, ex)` preserva stack trace original |
| **Logging de excepciones** | Siempre pasar la excepción como primer parámetro: `_logger.LogError(ex, "...")` |
| **ModelState en GET** | Validar `ModelState.IsValid` también en acciones GET con parámetros |
| **JsonRequired** | `[JsonRequired]` de `System.Text.Json` para campos obligatorios (adicional a `[Required]`) |

---

## Impacto en el Pipeline

| Antes | Después |
|-------|---------|
| 39 errores SonarQube + fallas de infraestructura | **0 warnings, 0 errores** en build local |
| Actions con versiones inexistentes (snyk@v4, trivy@v0.28.0) | Versiones estables validadas |
| Docker build fallaba por .csproj faltantes | Multi-proyecto funciona correctamente |
| Pipeline fallaba si faltaban secrets | Steps se saltan gracefulmente |

## Tests que Requieren API Externa

Los siguientes tests están marcados con `[Skip("Requiere API externa")]` y necesitan `localhost:7227` para ejecutarse:

- `ZapLoginSecurityTests.LoginPost_WithInvalidCredentials_ReturnsUnauthorized`
- `ZapLoginSecurityTests.LoginEndpoint_DoesNotExposeStackTraceOnError`
- `ZapLoginSecurityTests.LoginPage_ReturnsSameResponseTimeForValidAndInvalidUsers`
- `UsuarioSecurityTests.UsuarioCreate_RejectsDuplicateEmail`
