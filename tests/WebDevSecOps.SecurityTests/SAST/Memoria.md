# Memoria — GlobalSecurityTests

**Archivo:** `SAST/GlobalSecurityTests.cs`
**Total de pruebas:** 10

---

## Cobertura por categoría de seguridad

| Categoría OWASP | Tests |
|---|---|
| **A01** Broken Access Control | `Controllers_HaveAuthorizeAttribute`, `NonexistentRoute_RedirectsToLoginWhenUnauthenticated` |
| **A02** Cryptographic Failures | `Hsts_IsConfiguredInProgramCs`, `HttpsRedirection_IsConfiguredInProgramCs` |
| **A05** Security Misconfiguration | `SecurityHeaders_CspHeaderIsPresent`, `SecurityHeaders_XContentTypeOptionsIsPresent`, `SecurityHeaders_XFrameOptionsIsPresent`, `SecurityHeaders_ReferrerPolicyIsPresent` |
| **A07** Identity & Auth Failures | `CsrfProtection_PostWithoutTokenReturns400` |
| **A09** Security Logging & Monitoring Failures | `RateLimiting_BlocksExcessiveRequests` |

---

## Tests funcionales (HTTP)

Usan `WebApplicationFactory<Program>` + `HttpClient`.

| # | Test | Verifica |
|---|---|---|
| 1 | `SecurityHeaders_CspHeaderIsPresent` | Header `Content-Security-Policy` en respuesta de `/` |
| 2 | `SecurityHeaders_XContentTypeOptionsIsPresent` | Header `X-Content-Type-Options: nosniff` |
| 3 | `SecurityHeaders_XFrameOptionsIsPresent` | Header `X-Frame-Options: DENY` |
| 4 | `SecurityHeaders_ReferrerPolicyIsPresent` | Header `Referrer-Policy: strict-origin-when-cross-origin` |
| 5 | `NonexistentRoute_RedirectsToLoginWhenUnauthenticated` | Ruta inexistente redirige a `/Login` por `FallbackPolicy` |
| 6 | `RateLimiting_BlocksExcessiveRequests` | 20 GET a `/Login` produce al menos 1 `429 Too Many Requests` |
| 7 | `CsrfProtection_PostWithoutTokenReturns400` | POST sin token antiforgery retorna `400 Bad Request` |

## Tests estáticos (SAST)

Leen `Program.cs` o usan reflection.

| # | Test | Verifica |
|---|---|---|
| 8 | `Hsts_IsConfiguredInProgramCs` | `app.UseHsts()` existe en `Program.cs` |
| 9 | `HttpsRedirection_IsConfiguredInProgramCs` | `app.UseHttpsRedirection()` existe en `Program.cs` |
| 10 | `Controllers_HaveAuthorizeAttribute` | Todos los `Controller` tienen `[Authorize]` (excepto `[AllowAnonymous]`) |

---

## Historial de correcciones

| Test | Problema inicial | Solución |
|---|---|---|
| `CsrfProtection_PostWithoutTokenReturns400` | Enviaba JSON (`application/json`) no activaba validación antiforgery; fallaba por rate limiting de tests previos | Cambiado a `FormUrlEncodedContent` + factory aislada por test |
| `NonexistentRoute_RedirectsToLoginWhenUnauthenticated` | Esperaba `404` pero obtenía `302` por `FallbackPolicy` | Cambiado a verificar `302` con redirect a `/Login` |
| `HttpsRedirection_RedirectsHttpToHttps` | `WebApplicationFactory` no soporta HTTPS para verificar redirect real | Cambiado a test estático que verifica `UseHttpsRedirection` en `Program.cs` |
| `SecurityHeaders_ReferrerPolicyIsPresent` (+ XContentType, XFrame) | Headers no configurados en la app | Agregados a middleware de Program.cs |
| `SecurityHeaders_StrictTransportSecurityIsPresent` | `UseHsts()` solo aplica en HTTPS; factory usa HTTP | Cambiado a test estático que verifica `UseHsts` en `Program.cs` |

---

# LoginSecurityTests

**Archivo:** `SAST/LoginSecurityTests.cs`
**Total de pruebas:** 8 métodos, 17 casos

---

## Cobertura por categoría OWASP

| Categoría OWASP | Tests |
|---|---|
| **A01** Broken Access Control | `LoginPage_BlocksOpenRedirect` |
| **A07** Identity & Auth Failures | `LoginPage_RejectsEmptyUser`, `LoginPage_RejectsEmptyPassword`, `LoginPage_SanitizesXssInUserField`, `LoginPage_HasAntiForgeryToken`, `LoginPage_ReturnsLoginPageOnGet` |
| **A09** Security Logging & Monitoring Failures | `LoginPage_LocksOutAfterMultipleFailedAttempts` |

## Tests funcionales (HTTP)

Usan `WebApplicationFactory<Program>` aislada por test + `HttpClient`.

| # | Test | Verifica |
|---|---|---|
| 1 | `LoginPage_RejectsEmptyUser` | Usuario vacío &rarr; 200 + &ldquo;required&rdquo; (3 casos: `""`, `"   "`, `null`) |
| 2 | `LoginPage_RejectsEmptyPassword` | Contraseña vacía &rarr; 200 + &ldquo;required&rdquo; (3 casos) |
| 3 | `LoginPage_SanitizesXssInUserField` | 6 payloads XSS no se reflejan como HTML crudo en la respuesta |
| 4 | `LoginPage_BlocksOpenRedirect` | 5 URLs externas bloqueadas como `returnUrl` (GET y POST) |
| 5 | `LoginPage_ReturnsLoginPageOnGet` | GET `/Login` &rarr; 200 + botón &ldquo;Ingresar&rdquo; visible |
| 6 | `LoginPage_HasAntiForgeryToken` | La página contiene `__RequestVerificationToken` |
| 7 | `LoginPage_LocksOutAfterMultipleFailedAttempts` | 5 fallos + 1 extra &rarr; 429 Too Many Requests |

## Buenas prácticas aplicadas

| Práctica | Descripción |
|---|---|
| **Aislamiento total por test** | Cada test crea su propia `WebApplicationFactory` con `using`. Elimina interferencia del rate limiter (5 POST/min) y estado de autenticación entre tests. |
| **Token antiforgery explícito** | Los tests POST extraen `__RequestVerificationToken` vía `GetAntiForgeryTokenAsync()` (regex sobre HTML del GET) y lo incluyen en el payload. Sin esto el middleware CSRF rechaza con 400. |
| **`AllowAutoRedirect = false`** | El cliente HTTP no sigue redirecciones; cada test inspecciona el código de estado real (302 vs 200 vs 429). |
| **`HandleCookies = true`** | Mantiene cookies antiforgery y de autenticación entre requests del mismo cliente (esencial para sesiones coherentes). |
| **Datos parametrizados centralizados** | Payloads XSS y open redirect definidos en `SecurityTestData.cs` &mdash; reutilizable y fácil de extender. |
| **Verificación semántica** | `SanitizesXssInUserField` chequea `DoesNotContain(xssPayload, body)` en lugar de buscar `<script>` en el body decodificado; evita falsos positivos con etiquetas del layout. |
| **Assert sobre el cuerpo, no solo status** | Además del código HTTP, se verifica contenido específico (`"required"`, `"Ingresar"`, `"__RequestVerificationToken"`) para confirmar respuesta correcta. |
| **Sin estado compartido** | Eliminado `IClassFixture` y constructor con `_client`. Evita fugas de estado entre tests (cookies, rate limiter, sesión). |

## Historial de correcciones

| Test | Problema inicial | Solución |
|---|---|---|
| `LoginPage_RejectsEmptyUser`, `LoginPage_RejectsEmptyPassword`, `LoginPage_SanitizesXssInUserField`, `LoginPage_BlocksOpenRedirect`, `LoginPage_LocksOutAfterMultipleFailedAttempts` | Rate limiter agotado por tests previos (429 inesperado) + ausencia de token antiforgery (400) | Factory aislada por test + extracción de `__RequestVerificationToken` antes del POST |
| `LoginPage_ReturnsLoginPageOnGet` | Buscaba &ldquo;Login&rdquo; en página en español (&ldquo;Iniciar Sesión&rdquo;) | Cambiado a buscar &ldquo;Ingresar&rdquo; (texto del botón) |
| `LoginPage_SanitizesXssInUserField` | Falso positivo con `<script>` del `_ValidationScriptsPartial` | Cambiado a verificar payload crudo en body sin decodificar |
| `LoginSecurityTests` completo | Usaba `IClassFixture` con estado compartido | Eliminado fixture compartido; cada test crea su propia instancia |
