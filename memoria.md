# Memoria Técnica — Implementación de `returnUrl` en Usuario (Create + Update)

## Fecha
2026-06-25

## Problema
Las acciones `Create` y `Update` de `UsuarioController` redirigían siempre a `/Usuario/Index` (página 1) tras guardar o cancelar, sin importar desde qué página paginada o vista (Index, Details) hubiera navegado el usuario.

## Solución
Patrón `returnUrl` — pasar la URL de origen como query parameter desde las vistas origen, preservarla en un hidden input en los formularios, y redirigir a ella tras guardar/cancelar si es una URL local válida.

---

## Fase 1 — Create

### `Controllers/UsuarioController.cs`
- **GET Create** (línea 61): acepta `string? returnUrl = null`, lo guarda en `ViewData["ReturnUrl"]`.
- **POST Create** (línea 69): acepta `string? returnUrl = null`. En éxito, si `returnUrl` es local (`Url.IsLocalUrl`), redirige a ella; sino a `RedirectToAction("Index")`. En errores de validación, preserva `returnUrl` en `ViewData`.

### `Views/Usuario/Index.cshtml`
- Línea 17: enlace "+ Agregar Usuario" pasa `asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)"`.

### `Views/Usuario/Create.cshtml`
- Línea 27: hidden input `<input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />`.
- Línea 50: Cancelar usa `href="@(ViewData["ReturnUrl"] ?? Url.Action("Index"))"`.

---

## Fase 2 — Update

### `Controllers/UsuarioController.cs`
- **GET Update** (línea 111): acepta `string? returnUrl = null`, lo guarda en `ViewData["ReturnUrl"]`. Si el usuario no existe y hay `returnUrl` local, redirige a ella; sino a `/Usuario/Index`.
- **POST Update** (línea 133): acepta `string? returnUrl = null`. En éxito, si `returnUrl` es local, redirige a ella; sino a `RedirectToAction("Index")`. En errores de validación, preserva `returnUrl` en `ViewData`.

### `Views/Usuario/Index.cshtml`
- Línea 51: enlace "Actualizar" pasa `asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)"`.

### `Views/Usuario/Details.cshtml`
- Línea 41: enlace "Editar" pasa `asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)"`.

### `Views/Usuario/Update.cshtml`
- Línea 27: hidden input `<input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />`.
- Línea 53: Cancelar usa `href="@(ViewData["ReturnUrl"] ?? Url.Action("Index"))"`.

---

## Seguridad
- `Url.IsLocalUrl(returnUrl)` previene open redirect attacks (mismo patrón usado en `Pages/Login.cshtml.cs`).

## Pruebas de compilación
- `dotnet build` → 0 errores, warnings preexistentes.

## Archivos modificados (resumen)
| Archivo | Cambios |
|---------|---------|
| `Controllers/UsuarioController.cs` | GET/POST Create + GET/POST Update |
| `Views/Usuario/Index.cshtml` | Enlaces "+ Agregar Usuario" y "Actualizar" |
| `Views/Usuario/Details.cshtml` | Enlace "Editar" |
| `Views/Usuario/Create.cshtml` | Hidden input + Cancelar |
| `Views/Usuario/Update.cshtml` | Hidden input + Cancelar |

---

## Fase 3 — Delete

### `Controllers/UsuarioController.cs`
- **GET Delete** (línea 190): acepta `string? returnUrl = null` (después de `CancellationToken`), guarda en `ViewData["ReturnUrl"]`. Si el usuario no existe y hay `returnUrl` local, redirige a ella; sino a `/Usuario/Index`.
- **POST Delete** (línea 213): acepta `string? returnUrl = null`. En éxito, si `returnUrl` es local, redirige a ella; sino a `RedirectToAction("Index")`. En errores de validación, preserva `returnUrl` en `ViewData`.

### `Views/Usuario/Index.cshtml`
- Línea 52: enlace "Eliminar" pasa `asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)"`.

### `Views/Usuario/Delete.cshtml`
- Línea 32: hidden input `<input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />`.
- Línea 46: Cancelar usa `href="@(ViewData["ReturnUrl"] ?? Url.Action("Index"))"`.

---

## Fase 4 — Details

### `Controllers/UsuarioController.cs`
- **GET Details** (línea 37): acepta `string? returnUrl = null` (después de `CancellationToken`), guarda en `ViewData["ReturnUrl"]`. Si el usuario no existe y hay `returnUrl` local, redirige a ella; sino a `/Usuario/Index`.

### `Views/Usuario/Index.cshtml`
- Línea 50: enlace "Detalles" pasa `asp-route-returnUrl="@(Context.Request.Path + Context.Request.QueryString)"`.

### `Views/Usuario/Details.cshtml`
- Línea 42: "Regresar" usa `href="@(ViewData["ReturnUrl"] ?? Url.Action("Index"))"`.

---

## Resumen Final

### Problema
Create, Update, Delete y Details redirigían siempre a `/Usuario/Index` (página 1), perdiendo paginación y contexto de navegación.

### Solución implementada
Patrón `returnUrl` con validación `Url.IsLocalUrl()` en todos los flujos de `UsuarioController`:

| Archivo | Cambio |
|---------|--------|
| `Controllers/UsuarioController.cs` | GET/POST Create, Update, Delete + GET Details, GET Update — aceptan `returnUrl`, redirigen a ella si es local |
| `Views/Usuario/Index.cshtml` | Enlaces "Detalles", "Agregar Usuario", "Actualizar" y "Eliminar" pasan URL actual |
| `Views/Usuario/Details.cshtml` | Enlace "Editar" + botón "Regresar" usan `returnUrl` |
| `Views/Usuario/Create.cshtml` | Hidden input + Cancelar usa `returnUrl` |
| `Views/Usuario/Update.cshtml` | Hidden input + Cancelar usa `returnUrl` |
| `Views/Usuario/Delete.cshtml` | Hidden input + Cancelar usa `returnUrl` |

### Resultado
- **Build:** 0 errores
- **Flujo:** Usuario en `Index?pageNumber=3` → cualquier acción (Details/Create/Update/Delete) → Guardar/Cancelar/Regresar → vuelve a `Index?pageNumber=3`
- Desde Details → Editar → Update → Guardar/Cancelar → vuelve a Details

---

## Fase 5 — Toastr Notifications

### Fecha
2026-06-25

### Problema
Los mensajes de éxito/error tras operaciones CRUD (Create, Update, Delete) se mostraban como alertas Bootstrap fijas en `Index.cshtml`, ocupando espacio visual y requiriendo clic manual para cerrarse.

### Solución
Se reemplazaron las alertas estáticas por notificaciones **toastr** con auto-dismiss, barra de progreso y botón de cierre.

### Cambios realizados

| Archivo | Cambio |
|---------|--------|
| `libman.json` | Se agregó `toastr@2.1.4` (provider: unpkg) |
| `wwwroot/lib/toastr/toastr.min.js` | Descargado manualmente desde CDNJS |
| `wwwroot/lib/toastr/toastr.min.css` | Descargado manualmente desde CDNJS |
| `Pages/Shared/_Layout.cshtml` | Se agregó CSS/JS de toastr + script con configuración global + detección de TempData["Success"]/["Error"] para disparar toastr |
| `Views/Usuario/Index.cshtml` | Se eliminaron los bloques `alert alert-success` y `alert alert-danger` de TempData |

### Comportamiento
- **Éxito** (Create/Update/Delete): `toastr["success"]("Operación exitosa!!")`
- **Error**: `toastr["error"]("Error al procesar la operación")`
- **Posición**: `toast-bottom-right`
- **Auto-dismiss**: 5 segundos con barra de progreso
- **Cierre manual**: Botón close disponible

### Verificación
- Build: 0 errores
- Create, Update, Delete exitosos → toastr success
- Errores del servidor → toastr error
