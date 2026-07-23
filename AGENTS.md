# WebDevSecOps — Memoria del proyecto

## Convenciones generales
- **Idioma**: Código fuente y mensajes UX en español (controladores, vistas, validaciones)
- **Patrón CRUD**: Controlador → Servicio (HttpClient) → API externa, siguiendo el mismo patrón que `UsuarioController`/`IUsuarioService`
- **Mensajes al usuario**: Toastr mediante `TempData["Success"]`, `TempData["Error"]`, `TempData["Warning"]`
- **Resultado de operaciones**: `OperationResult<T>` (genérico) en `Models/OperationResult.cs`
- **Concurrencia optimista**: `RowVersion` (byte[]) en DeleteViewModel, pasado al servicio para el DELETE de la API
- **ReturnUrl**: Patrón opcional en acciones POST para redirect condicional

## Módulo Empleado (implementado)
### Archivos clave
| Archivo | Propósito |
|---------|-----------|
| `Controllers/EmpleadoController.cs` | 8 acciones (Index, Create GET/POST, Details, Update GET/POST, Delete GET/POST) |
| `Services/IEmpleadoService.cs`, `EmpleadoService.cs` | 6 métodos HttpClient contra `/api/v1/Empleado` |
| `Services/ITipoEmpleadoService.cs`, `TipoEmpleadoService.cs` | Catálogo read-only (2 métodos) |
| `Models/Empleado.cs` | Modelo de dominio |
| `Models/EmpCatTipoEmpleado.cs` | Modelo de catálogo (read-only) |
| `Models/Empleado{Create,Update,Details,Delete}ViewModel.cs` | ViewModels por acción |
| `Views/Empleado/{Index,Create,Details,Update,Delete}.cshtml` | Vistas Razor |

### Endpoints API consumidos
- `GET /api/v1/Empleado?PageNumber=&PageSize=`
- `POST /api/v1/Empleado`
- `GET /api/v1/Empleado/{id}`
- `PUT /api/v1/Empleado/{id}`
- `DELETE /api/v1/Empleado/{id}`
- `GET /api/v1/Empleado/buscar?texto=&idTipoEmpleado=`
- `GET /api/v1/TipoEmpleado?PageNumber=&PageSize=`
- `GET /api/v1/TipoEmpleado/{id}`

### Filtros en Index
- **Texto**: búsqueda por nombre/apellido vía `SearchEmpleadosAsync`
- **TipoEmpleado**: dropdown poblado desde `ViewBag.TipoEmpleadoList` (SelectList)
- **Paginación**: `pagina` (default 1) y `pageSize` (default 10)
- El controlador llama a `SearchEmpleadosAsync` si `texto` o `idTipoEmpleado` tienen valor, sino a `GetEmpleadosAsync`

### Dropdown TipoEmpleado
- Poblado en `CargarTipoEmpleadosAsync()` helper en el controlador
- Almacenado en `ViewBag.TipoEmpleadoList` como `SelectList` (value=`Id`, text=`StrValor`)
- También se guarda un `Dictionary<int, string>` en `ViewBag.TipoEmpleadoDict` para resolución en Index sin N+1
- `pageSize=100` para obtener todos los registros del catálogo

### Pruebas
| Proyecto | Archivos |
|----------|----------|
| `WebDevSecOps.UnitTests` | `Pages/Empleados/{IndexTests,CreateTests,UpdateTests,DeleteTests}.cs` |
| `WebDevSecOps.IntegrationTests` | `Services/{EmpleadoApiClientTests,TipoEmpleadoApiClientTests}.cs` |
| `WebDevSecOps.SecurityTests` | `SAST/EmpleadoSecurityTests.cs` |

### Comandos
```powershell
dotnet build                           # build completo (0 errors, 0 warnings)
dotnet test tests\WebDevSecOps.UnitTests
dotnet test tests\WebDevSecOps.IntegrationTests
dotnet test tests\WebDevSecOps.SecurityTests
```

### Patrón para nuevos CRUDs
1. Crear modelo de dominio en `Models/`
2. Crear ViewModels en `Models/` por acción
3. Crear interfaz e implementación de servicio en `Services/`
4. Registrar en `Program.cs` con `AddHttpClient<IServicio, Servicio>()...AddStandardResilienceHandler()`
5. Crear controlador en `Controllers/` heredando `Controller` con `[Authorize]`
6. Crear vistas en `Views/{Nombre}/`
7. Agregar ítem de menú en `Pages/Shared/_Layout.cshtml` (después de "Usuarios")
8. Agregar datos de prueba en `tests/WebDevSecOps.UnitTests/Common/ContractTestData.cs`
9. Crear tests unitarios, de integración y seguridad

## Rediseño UI — Julio 2026

### Sidebar
- **Fondo**: blanco (`#ffffff`), texto verde `#109E63`
- **Active**: barra izquierda `#109E63`, fondo `rgba(16,158,99,0.1)`, texto `#0d7d4f` (negrita)
- **Archivo**: `wwwroot/css/site.css` (sección `.sidebar`)
- **Layout**: `Pages/Shared/_Layout.cshtml`

### Tablas estilo jTable
| Clase CSS | Propósito |
|-----------|-----------|
| `.card-table` | Card con sombra que envuelve la tabla |
| `.table-jtable` | Tabla con headers uppercase, borde inferior verde, hover suave |
| `.btn-icon` | Botón cuadrado 32×32 solo con icono FA |
| `.pagination-jtable` | Paginación con bordes redondeados, verde activo |
| `.skeleton-table-row` / `.skeleton-cell` | Esqueleto de carga (5 filas animadas) |

### Iconografía FontAwesome
| Acción | Icono | Clase btn |
|--------|-------|-----------|
| Detalles | `fa-eye` | `btn-outline-info` |
| Actualizar/Editar | `fa-pen-to-square` | `btn-outline-warning` / `btn-outline-success` |
| Eliminar | `fa-trash-can` | `btn-outline-danger` |
| Guardar | `fa-save` | `btn-outline-success` |
| Cancelar/Regresar | `fa-times` / `fa-arrow-left` | `btn-outline-secondary` |

### Vistas modificadas (22 archivos)

| Tipo | Vistas |
|------|--------|
| **Index** (6) | `Empleado`, `Usuario`, `Producto`, `Cliente`, `Venta/Index`, `Venta/Productos` |
| **Create** (5) | `Empleado`, `Usuario`, `Producto`, `Cliente`, `Venta` |
| **Update** (4) | `Empleado`, `Usuario`, `Producto`, `Cliente` |
| **Delete** (4) | `Empleado`, `Usuario`, `Producto`, `Cliente` |
| **Details** (4) | `Empleado`, `Usuario`, `Producto`, `Cliente` |

### Skeleton loader
- Cada vista Index tiene HTML de skeleton estático (5 filas) oculto tras `d-none`
- `site.js` lo muestra 500ms en `DOMContentLoaded`, luego revela la tabla real

### JavaScript
- `wwwroot/js/site.js`: skeleton toggle + active link dinámico por URL
- CDN: jQuery, Bootstrap bundle, Toastr, FontAwesome 6 (ya instalados)
