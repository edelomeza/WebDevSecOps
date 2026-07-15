# M�dulo Venta � Memoria del proyecto

## Rama: Venta

---

## Resumen del m�dulo

M�dulo CRUD parcial para la gesti�n de ventas. Consume los endpoints de la API WebAPIDevSecOps (/api/v1/Venta, /api/v1/Venta/buscar, /api/v1/EstadoVenta, /api/v1/Cliente/autocomplete, /api/v1/Usuario/autocomplete).

### Acciones implementadas

| Acci�n | M�todo | Ruta | Prop�sito |
|--------|--------|------|-----------|
| Index | GET | /Venta/Index | Listado paginado con filtros (texto + fechas) |
| Create | GET | /Venta/Create | Formulario de nueva venta |
| Create | POST | /Venta/Create | Procesa y guarda la venta |
| ClientesAutocomplete | GET | /Venta/ClientesAutocomplete?texto=&maxResultados=10 | JSON para autocomplete de clientes |
| UsuariosAutocomplete | GET | /Venta/UsuariosAutocomplete?texto=&maxResultados=10 | JSON para autocomplete de usuarios |

---

## Endpoints API consumidos

| M�todo | Endpoint | Uso |
|--------|----------|-----|
| GET | /api/v1/Venta?PageNumber=&PageSize= | Listado paginado |
| GET | /api/v1/Venta/buscar?strClaveVenta=&strNombreCliente=&dteFechaInicio=&dteFechaFin=&PageNumber=&PageSize= | B�squeda con filtros |
| POST | /api/v1/Venta | Crear venta (body: { idCliCliente, idSegUsuario }) |
| GET | /api/v1/EstadoVenta?PageNumber=&PageSize= | Cat�logo de estados (read-only) |
| GET | /api/v1/Cliente/autocomplete?texto=&maxResultados= | Autocomplete de clientes |
| GET | /api/v1/Usuario/autocomplete?texto=&maxResultados= | Autocomplete de usuarios |

---

## Pantalla principal (Index)

### Filtros
- **Texto �nico**: Busca simult�neamente por strClaveVenta y strNombreCliente (se env�a el mismo valor a ambos par�metros)
- **Rango de fechas**: dteFechaInicio (Desde) y dteFechaFin (Hasta) � controles input type="date"
- La paginaci�n preserva todos los filtros en los enlaces

### Columnas de la tabla
| Cabecera | Propiedad | Origen |
|----------|-----------|--------|
| Fecha y Hora | DteFechaHoraCompra | VenVentaDto |
| Clave Venta | StrClaveVenta | VenVentaDto |
| Nombre Cliente | StrNombreCliente | VenVentaDto (resuelto desde CliCliente) |
| Estado Venta | StrEstado | VenCatEstado resuelto v�a ViewBag.EstadoVentaDict |

### Paginaci�n
- pageSize fijo en 10 registros por p�gina (default)
- Controles Anterior/Siguiente + numeraci�n de p�ginas
- Texto informativo: "Mostrando p�gina X de Y (Z registros)"

---

## Pantalla Create

### Campos del formulario
- **Cliente**: Campo de b�squeda con autocomplete (m�n. 2 caracteres). El usuario escribe, JS consulta /Venta/ClientesAutocomplete?texto=..., despliega resultados en dropdown, al seleccionar se llena IdCliCliente (hidden)
- **Usuario**: Mismo comportamiento con /Venta/UsuariosAutocomplete?texto=..., llena IdSegUsuario (hidden)

### Datos enviados a la API
`json
{
  "idCliCliente": 1,
  "idSegUsuario": 1
}
`
- idVenCatEstado: Asignado por la API
- dteFechaHoraCompra: Asignado por la API
- strClaveVenta: Calculado por la API

### Manejo de errores (POST)
- �xito ? TempData["Success"] + redirect a Index
- Error de campo ? ModelState.AddModelError con mapeo (idCliCliente ? IdCliCliente, idSegUsuario ? IdSegUsuario)
- Error general ? ModelState.AddModelError(string.Empty, mensaje)
- Recarga la vista con el modelo para reintentar

---

## Archivos del m�dulo

### Modelos
| Archivo | Prop�sito |
|---------|-----------|
| Models/Venta.cs | Entidad de dominio (VenVentaDto) |
| Models/VenCatEstado.cs | Entidad de cat�logo (VenCatEstadoDto) |
| Models/VentaCreateViewModel.cs | ViewModel para crear venta |
| Models/CliClienteAutocompleteDto.cs | DTO para autocomplete de cliente |
| Models/SegUsuarioAutocompleteDto.cs | DTO para autocomplete de usuario |

### Servicios
| Archivo | Prop�sito |
|---------|-----------|
| Services/IVentaService.cs | Interfaz: GetVentasAsync, SearchVentasAsync, CreateVentaAsync |
| Services/VentaService.cs | Implementaci�n HttpClient |
| Services/IEstadoVentaService.cs | Interfaz cat�logo (read-only) |
| Services/EstadoVentaService.cs | Implementaci�n HttpClient |
| IClienteService.cs / ClienteService.cs | + AutocompleteClientesAsync |
| IUsuarioService.cs / UsuarioService.cs | + AutocompleteUsuariosAsync |

### Controlador
| Archivo | Acciones |
|---------|----------|
| Controllers/VentaController.cs | Index, Create (GET/POST), ClientesAutocomplete, UsuariosAutocomplete |

### Vistas
| Archivo | Prop�sito |
|---------|-----------|
| Views/Venta/Index.cshtml | Listado con filtros + tabla paginada |
| Views/Venta/Create.cshtml | Formulario con autocomplete JS |

### Modificaciones a otros archivos
| Archivo | Cambio |
|---------|--------|
| Program.cs | DI: IVentaService + IEstadoVentaService |
| Pages/Shared/_Layout.cshtml | Men� "Ventas" despu�s de Productos |

---

## Pruebas

### Unitarias (WebDevSecOps.UnitTests)
| Archivo | Tests |
|---------|-------|
| Pages/Ventas/IndexTests.cs | 8 tests: paginaci�n, filtros, servicio nulo, carga de estado, etc. |
| Pages/Ventas/CreateTests.cs | 8 tests: create get/post, validaci�n, errores, autocomplete >2 chars, field errors |

### Integraci�n (WebDevSecOps.IntegrationTests)
| Archivo | Tests |
|---------|-------|
| Services/VentaApiClientTests.cs | 9 tests: URLs correctas (list/search/create), serializaci�n, bearer token, errores HTTP, missing token |

### Seguridad (WebDevSecOps.SecurityTests)
| Archivo | Tests |
|---------|-------|
| SAST/VentaSecurityTests.cs | 12 tests: autenticaci�n requerida (Index, Create, autocomplete), validaci�n campos obligatorios, tipo inv�lido |

### Datos de prueba
| Archivo | Datos agregados |
|---------|----------------|
| Common/ContractTestData.cs | ValidVenCatEstado, ValidVenta, ValidVentaPaginatedResponse, ValidVentaCreateViewModel, ValidClienteAutocompleteList, ValidUsuarioAutocompleteList, VentaValidationApiError |

---

## Convenciones aplicadas (heredadas de Empleado)

- [Authorize] en el controlador
- TempData["Success"] / TempData["Error"] para notificaciones Toastr
- OperationResult<T> para resultados de operaciones
- ViewBag.EstadoVentaDict (Dictionary) para resolver cat�logos en tabla (evita N+1)
- returnUrl opcional para redirect condicional
- Manejo de ModelState.AddModelError con mapeo de nombres de campo (snake ? Pascal)
- SafeResponseLogger.LogResponseFailure para errores de API
- Paginaci�n con preservaci�n de query params
- Autocomplete con m�nimo 2 caracteres
- JS con nonce para seguridad CSP
