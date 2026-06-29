# Pruebas Unitarias — Usuarios (Index, Create, Update y Delete)

## Archivos

| Archivo | Tests |
|---------|-------|
| `tests/WebDevSecOps.UnitTests/Pages/Usuarios/IndexTests.cs` | 7 |
| `tests/WebDevSecOps.UnitTests/Pages/Usuarios/CreateTests.cs` | 17 |
| `tests/WebDevSecOps.UnitTests/Pages/Usuarios/UpdateTests.cs` | 20 |
| `tests/WebDevSecOps.UnitTests/Pages/Usuarios/DeleteTests.cs` | 15 |

## Framework
- **xUnit** + **Moq**
- Patrón AAA (Arrange-Act-Assert)
- Helper `CreateController()` para inicializar el `UsuarioController` con mocks de `IUsuarioService` + `ILogger<UsuarioController>`, `ControllerContext` y `TempData`

---

## IndexTests — Cobertura (7 tests)

| # | Test | Escenario |
|---|------|-----------|
| 1 | `Index_ReturnsViewWithPaginatedResponse_WhenServiceReturnsData` | Servicio retorna `PaginatedResponse<Usuario>` con datos → `ViewResult` con modelo poblado, sin `TempData["Error"]` |
| 2 | `Index_ReturnsViewWithNullModel_WhenServiceReturnsNull` | Servicio retorna `null` → `ViewResult` con modelo `null`, `TempData["Error"]` asignado |
| 3 | `Index_ReturnsViewWithEmptyList_WhenServiceReturnsNoData` | Servicio retorna lista vacía → `ViewResult` con `Items` vacío, `TotalCount = 0` |
| 4 | `Index_UsesDefaultPagination_WhenNoParametersProvided` | Sin parámetros → `pageNumber=1`, `pageSize=10` por defecto |
| 5 | `Index_PassesCustomPaginationToService` | Con `pageNumber=3`, `pageSize=25` → argumentos propagados al servicio |
| 6 | `Index_LogsWarning_WhenServiceReturnsNull` | Servicio retorna `null` → `Log(LogLevel.Warning)` llamado exactamente una vez |
| 7 | `Index_DoesNotLogWarning_WhenServiceReturnsData` | Servicio retorna datos → `Log(LogLevel.Warning)` NO es llamado |

---

## CreateTests — Cobertura (17 tests)

### GET Create (2)

| # | Test | Assert |
|---|------|--------|
| 1 | `Create_Get_ReturnsView_WhenCalled` | `ViewResult`, modelo `null` |
| 2 | `Create_Get_SetsReturnUrlInViewData_WhenReturnUrlProvided` | `ViewData["ReturnUrl"]` == `"/dashboard"` |

### POST Create — Controller con Mocks (8)

| # | Test | Mock Setup | Assert |
|---|------|-----------|--------|
| 3 | `Create_Post_ReturnsView_WhenModelStateInvalid` | `ModelState.AddModelError(...)` | `ViewResult`, servicio **no** llamado |
| 4 | `Create_Post_RedirectsToIndex_WhenServiceSucceeds` | `CreateUsuarioAsync` → `Ok()` | `RedirectToActionResult("Index")`, `TempData["Success"]` |
| 5 | `Create_Post_RedirectsToReturnUrl_WhenLocalUrl` | `Ok()`, `Url.IsLocalUrl` → `true` | `RedirectResult` a returnUrl |
| 6 | `Create_Post_RedirectsToIndex_WhenNonLocalUrl` | `Ok()`, `Url.IsLocalUrl` → `false` | `RedirectToActionResult("Index")` |
| 7 | `Create_Post_ReturnsViewWithFieldErrors_WhenServiceFails` | `Fail({ "strNombre": ["error"] })` | `ViewResult`, `ModelState["strNombre"]` con error |
| 8 | `Create_Post_ReturnsViewWithGeneralError_WhenServiceFails` | `Fail("mensaje")` | `ViewResult`, `ModelState[""]` con error |
| 9 | `Create_Post_ReturnsViewWithBothErrors_WhenServiceFailsWithFieldAndGeneral` | `Fail(fieldErrors, "mensaje")` | Ambos errores en `ModelState` |
| 10 | `Create_Post_LogsInformation_WhenServiceSucceeds` | `Ok()` | `logger.Verify(LogLevel.Information, ...)` |

### Validación de Modelos — `UsuarioCreateViewModel` (7)

| # | Test | Escenario | Assert |
|---|------|-----------|--------|
| 11 | `CreateViewModel_Nombre_Required_Fails` | `StrNombre = ""` | Error: "obligatorio" |
| 12 | `CreateViewModel_Nombre_MaxLength_Fails` | `StrNombre = new string('a', 51)` | Error: "exceder" |
| 13 | `CreateViewModel_Nombre_InvalidCharacters_Fails` | `StrNombre = "Juan@#$"` | Error: "solo permite" |
| 14 | `CreateViewModel_Password_MinLength_Fails` | `StrPWD = "Ab1!@"` (5 chars) | Error: "al menos" |
| 15 | `CreateViewModel_Password_Complexity_Fails` | `StrPWD = "abcdefgh"` (solo minúsculas) | Error: "mayúsculas" |
| 16 | `CreateViewModel_Email_InvalidFormat_Fails` | `StrCorreoElectronico = "invalido"` | Error: "formato" |
| 17 | `CreateViewModel_ValidModel_PassesAll` | Todos válidos | `IsValid == true`, 0 errores |

---

## UpdateTests — Cobertura (20 tests)

### GET Update (6)

| # | Test | Assert |
|---|------|--------|
| 1 | `Update_Get_ReturnsViewWithModel_WhenUserFound` | `ViewResult`, modelo tipo `UsuarioUpdateViewModel` |
| 2 | `Update_Get_RedirectsToIndex_WhenUserNotFound` | `RedirectToActionResult("Index")`, `TempData["Error"]` |
| 3 | `Update_Get_RedirectsToReturnUrl_WhenUserNotFoundWithLocalUrl` | `RedirectResult` a returnUrl |
| 4 | `Update_Get_RedirectsToIndex_WhenUserNotFoundWithNonLocalUrl` | `RedirectToActionResult("Index")` |
| 5 | `Update_Get_LogsWarning_WhenUserNotFound` | `logger.Verify(LogLevel.Warning, ...)` |
| 6 | `Update_Get_SetsReturnUrlInViewData_WhenUserFound` | `ViewData["ReturnUrl"]` == `"/perfil"` |

### POST Update — Controller con Mocks (7)

| # | Test | Mock Setup | Assert |
|---|------|-----------|--------|
| 7 | `Update_Post_ReturnsView_WhenModelStateInvalid` | `ModelState.AddModelError(...)` | `ViewResult`, servicio **no** llamado |
| 8 | `Update_Post_RedirectsToIndex_WhenServiceSucceeds` | `UpdateUsuarioAsync` → `Ok()` | `RedirectToActionResult("Index")`, `TempData["Success"]` |
| 9 | `Update_Post_RedirectsToReturnUrl_WhenLocalUrl` | `Ok()`, `Url.IsLocalUrl` → `true` | `RedirectResult` a returnUrl |
| 10 | `Update_Post_RedirectsToIndex_WhenNonLocalUrl` | `Ok()`, `Url.IsLocalUrl` → `false` | `RedirectToActionResult("Index")` |
| 11 | `Update_Post_ReturnsViewWithMappedFieldErrors_WhenServiceFails` | `Fail({ "strNombre": ... })` | `ViewResult`, `ModelState["StrNombre"]` mapeado |
| 12 | `Update_Post_ReturnsViewWithMappedErrors_ForAllFieldKeys` | `Fail` con 4 field errors | Todos los keys mapeados correctamente |
| 13 | `Update_Post_ReturnsViewWithGeneralError_WhenServiceFails` | `Fail("mensaje")` | `ViewResult`, `ModelState[""]` con error |
| 14 | `Update_Post_LogsInformation_WhenServiceSucceeds` | `Ok()` | `logger.Verify(LogLevel.Information, ...)` |

### Validación de Modelos — `UsuarioUpdateViewModel` (6)

| # | Test | Escenario | Assert |
|---|------|-----------|--------|
| 15 | `UpdateViewModel_Nombre_Required_Fails` | `StrNombre = ""` | Error: "obligatorio" |
| 16 | `UpdateViewModel_Nombre_MaxLength_Fails` | `StrNombre = new string('a', 51)` | Error: "exceder" |
| 17 | `UpdateViewModel_Nombre_InvalidCharacters_Fails` | `StrNombre = "Juan@#$"` | Error: "solo permite" |
| 18 | `UpdateViewModel_Password_MinLength_Fails` | `StrPWD = "Ab1!@"` (5 chars) | Error: "al menos" |
| 19 | `UpdateViewModel_Email_InvalidFormat_Fails` | `StrCorreoElectronico = "invalido"` | Error: "formato" |
| 20 | `UpdateViewModel_ValidModel_PassesAll` | Todos válidos | `IsValid == true`, 0 errores |

---

---

## DeleteTests — Cobertura (15 tests)

### GET Delete (6)

| # | Test | Assert |
|---|------|--------|
| 1 | `Delete_Get_ReturnsViewWithModel_WhenUserFound` | `ViewResult`, modelo tipo `UsuarioDeleteViewModel` |
| 2 | `Delete_Get_RedirectsToIndex_WhenUserNotFound` | `RedirectResult("/Usuario/Index")`, `TempData["Error"]` |
| 3 | `Delete_Get_RedirectsToReturnUrl_WhenUserNotFoundWithLocalUrl` | `RedirectResult` a returnUrl |
| 4 | `Delete_Get_RedirectsToIndex_WhenUserNotFoundWithNonLocalUrl` | `RedirectResult("/Usuario/Index")` |
| 5 | `Delete_Get_LogsWarning_WhenUserNotFound` | `logger.Verify(LogLevel.Warning, "not found for delete")` |
| 6 | `Delete_Get_SetsReturnUrlInViewData_WhenUserFound` | `ViewData["ReturnUrl"]` == `"/dashboard"` |

### POST Delete — Controller con Mocks (7)

| # | Test | Mock Setup | Assert |
|---|------|-----------|--------|
| 7 | `Delete_Post_ReturnsView_WhenModelStateInvalid` | `ModelState.AddModelError(...)` | `ViewResult`, servicio **no** llamado |
| 8 | `Delete_Post_RedirectsToIndex_WhenServiceSucceeds` | `DeleteUsuarioAsync` → `Ok()` | `RedirectToActionResult("Index")`, `TempData["Success"]` |
| 9 | `Delete_Post_RedirectsToReturnUrl_WhenLocalUrl` | `Ok()`, `Url.IsLocalUrl` → `true` | `RedirectResult` a returnUrl |
| 10 | `Delete_Post_RedirectsToIndex_WhenNonLocalUrl` | `Ok()`, `Url.IsLocalUrl` → `false` | `RedirectToActionResult("Index")` |
| 11 | `Delete_Post_ReturnsViewWithMappedFieldErrors_WhenServiceFails` | `Fail({"id":...,"rowVersion":...})` | `ModelState["Id"]` y `["RowVersion"]` mapeados |
| 12 | `Delete_Post_ReturnsViewWithGeneralError_WhenServiceFails` | `Fail("mensaje")` | `ViewResult`, `ModelState[""]` con error |
| 13 | `Delete_Post_LogsInformation_WhenServiceSucceeds` | `Ok()` | `logger.Verify(LogLevel.Information, ...)` |

### Validación de Modelos — `UsuarioDeleteViewModel` (2)

| # | Test | Escenario | Assert |
|---|------|-----------|--------|
| 14 | `DeleteViewModel_ValidModel_PassesAll` | Todos válidos | `IsValid == true`, 0 errores |
| 15 | `DeleteViewModel_RowVersion_Required_Fails_WhenNull` | `RowVersion = null` | Error `[Required]` en `RowVersion` |

---

## Resultado global
- **80/80 tests pasaron** (21 LoginTest + 7 IndexTests + 17 CreateTests + 20 UpdateTests + 15 DeleteTests)
- **0 errores**
- **1 warning residual** de Sonar por stub vacío (`EditTests.cs`)
