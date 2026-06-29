# Contract Testing — Login API

## Objetivo
Validar el contrato entre el consumidor (`AuthService`) y el proveedor (API externa `/api/v1/Login/login`).

## Archivos

| Archivo | Descripción |
|---|---|
| `Services/LoginApiClientTests.cs` | 21 tests de contrato |
| `../../WebDevSecOps.UnitTests/Common/ContractTestData.cs` | Datos del contrato |
| `WebDevSecOps.IntegrationTests.csproj` | +Moq, +ref a UnitTests |

## Cobertura (21 tests — todos verdes)

| Categoría | Tests | Aspectos validados |
|---|---|---|
| Request Contract | 6 | POST `api/v1/Login/login`, JSON `{user,password}`, Content-Type, caracteres especiales, sin campos extra |
| Response Contract | 7 | Token válido, caracteres especiales, forward-compat, token null/vacío/ausente, JSON inválido |
| Error Contract | 5 | 401/400/500 con/sin body, fallback a title, campos vacíos |
| Resilience Contract | 3 | HttpRequestException, OperationCanceledException, excepción genérica |

## Contrato definido

### Request
```
POST api/v1/Login/login
Content-Type: application/json
Body: { "user": "...", "password": "..." }
```

### Response (éxito — 200)
```json
{ "token": "string", "expiresIn": int }
```

### Response (error — 4xx/5xx)
```json
{ "title": "string", "detail": "string", "status": int }
```

---

# Contract Testing — Usuario API

## Objetivo
Validar el contrato entre el consumidor (`UsuarioService`) y el proveedor (API REST `/api/v1/Usuario`).

## Archivos

| Archivo | Descripción |
|---|---|
| `Services/UsuarioApiClientTests.cs` | 32 tests de contrato |
| `../../WebDevSecOps.UnitTests/Common/ContractTestData.cs` | Datos del contrato |
| `WebDevSecOps.IntegrationTests.csproj` | +Moq, +ref a UnitTests |

## Cobertura (32 tests — todos verdes)

| Endpoint | Tests | Request | Response | Token | Resilience |
|---|---|---|---|---|---|
| `GET /api/v1/Usuario` (list) | 6 | URL, query params, Bearer | PaginatedResponse, empty list, server error | MissingToken | — |
| `GET /api/v1/Usuario/{id}` | 6 | URL, Bearer | Usuario, 404, server error | MissingToken | — |
| `POST /api/v1/Usuario` | 7 | URL, JSON snake_case, Content-Type, Bearer | Ok, field errors | MissingToken | Network, Cancellation, Generic |
| `PUT /api/v1/Usuario/{id}` | 5 | URL, JSON con rowVersion | Ok, 409 Conflict | MissingToken | — |
| `DELETE /api/v1/Usuario/{id}` | 5 | URL, JSON con id+rowVersion | Ok, 409 Conflict | MissingToken | — |

## Contrato definido

### Request (todos)
```
Authorization: Bearer {token}
```

### GET list
```
GET /api/v1/Usuario?PageNumber={n}&PageSize={n}
```
```json
// Response 200
{ "items": [Usuario], "totalCount": int, "pageNumber": int, "pageSize": int, "totalPages": int }
```

### GET by ID
```
GET /api/v1/Usuario/{id}
```
```json
// Response 200
{ "id": int, "strNombre": "string", "strCorreoElectronico": "string", "dteFechaRegistro": "datetime", "rowVersion": "byte[]" }
// Response 404 → null
```

### POST Create
```
POST /api/v1/Usuario
Content-Type: application/json
Body: { "strNombre": "string", "strPWD": "string", "strCorreoElectronico": "string" }
```
```json
// Response 200 → Success
// Response 400
{ "title": "string", "detail": "string", "status": 400, "errors": { "strNombre": ["..."] } }
```

### PUT Update
```
PUT /api/v1/Usuario/{id}
Content-Type: application/json
Body: { "id": int, "strNombre": "string", "strPWD?": "string", "strCorreoElectronico": "string", "rowVersion": "byte[]" }
```
```json
// Response 200 → Success
// Response 409 → "El registro fue modificado por otro usuario..."
// Response 400 → ApiErrorResponse
```

### DELETE Delete
```
DELETE /api/v1/Usuario/{id}
Content-Type: application/json
Body: { "id": int, "rowVersion": "byte[]" }
```
```json
// Response 200 → Success
// Response 409 → "El registro fue modificado por otro usuario..."
// Response 400 → ApiErrorResponse
```

### Response (error genérico — 4xx/5xx)
```json
{ "title": "string", "detail": "string", "status": int, "errors": { "campo": ["mensaje"] } }
```

---

# Contract Testing — Logout API

## Objetivo
Validar el contrato entre el consumidor (`AuthService.LogoutAsync`) y el proveedor (API externa `/api/v1/Logout/logout`).

## Archivos

| Archivo | Descripción |
|---|---|
| `Services/LogoutApiClientTests.cs` | 12 tests de contrato |
| `../../WebDevSecOps.UnitTests/Common/ContractTestData.cs` | Constantes del contrato |
| `WebDevSecOps.IntegrationTests.csproj` | +Moq, +ref a UnitTests |

## Cobertura (12 tests — todos verdes)

| Categoría | Tests | Aspectos validados |
|---|---|---|
| Request Contract | 3 | POST `api/v1/Logout/logout`, Bearer token, sin body |
| Response Contract | 5 | 200/201 → true, 401/500/404 → false |
| Contract Violations | 2 | Token vacío, token con caracteres especiales |
| Resilience | 2 | HttpRequestException, excepción genérica |

## Contrato definido

### Request
```
POST api/v1/Logout/logout
Authorization: Bearer {token}
```
Sin body ni Content-Type.

### Response
```text
2xx → true
4xx/5xx → false
```
