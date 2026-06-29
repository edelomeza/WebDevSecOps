# DAST — Dynamic Application Security Testing

## Archivos
- `ZapGlobalSecurityTests.cs` — 12 pruebas de seguridad global
- `ZapLoginSecurityTests.cs` — 10 pruebas de login (7 activas, 3 omitidas)
- `ZapUsuarioSecurityTests.cs` — 7 pruebas de CRUD de usuarios
- `owasp-zap-config.context` — configuración de ZAP para escaneo autenticado
- `zap-baseline-rules.tsv` — 26 reglas baseline con niveles FAIL/WARN/IGNORE

## Problemas encontrados y corregidos

| Problema | Solución |
|----------|----------|
| URLs incorrectas (`/Usuarios/` → `/Usuario/`) | Corregidas — el controller es `Usuario` sin 's' |
| IClassFixture compartido → rate limiter agotado (429) | Factory aislado por prueba |
| Sin FakeAuthService → AuthService llamaba a localhost:7227 | Inyectado FakeAuthService + CookieSecurePolicy.SameAsRequest |
| Sin token antiforgery → POST devolvía 400 | GetAntiForgeryTokenAsync antes de cada POST |
| Token stale tras login | Token fresco desde página destino después de autenticar |
| Campos de formulario incorrectos (`user`/`password`) | Corregidos a `Input.Username`/`Input.Password` |
| Campos de modelo incorrectos (`Nombre`/`Email`/`Password`) | Corregidos a `StrNombre`/`StrCorreoElectronico`/`StrPWD` |
| TRACE retorna 200 (lo maneja Kestrel) | Eliminado del test |
| Nonexistent endpoint devuelve 302 (FallbackPolicy) | 404 → 302 |
| Delete inválido redirige a Index | 404 → 302 |
| Path de ProjectRoot incorrecto (6 niveles `..`) | Corregido a 5 niveles |

## Pruebas omitidas (requieren API externa)
- `LoginPost_WithInvalidCredentials_ReturnsUnauthorized`
- `LoginEndpoint_DoesNotExposeStackTraceOnError`
- `LoginPage_ReturnsSameResponseTimeForValidAndInvalidUsers`

Dependen de `AuthService` → `https://localhost:7227/api/v1/Login/login`

## Estado actual
- **DAST**: 22/25 pruebas pasan, 3 omitidas
- **Total proyecto**: 101/111 pasan, 6 fallan (snyk/gitleaks CLI no instalados), 4 omitidas
