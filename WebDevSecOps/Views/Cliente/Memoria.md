# Módulo Cliente — Resumen de implementación

## Fecha
2026-07-14

## Archivos creados

| Archivo | Propósito |
|---------|-----------|
| `Controllers/ClienteController.cs` | 8 acciones CRUD con `[Authorize]`, patrón returnUrl |
| `Services/IClienteService.cs` | Interfaz del servicio (6 métodos) |
| `Services/ClienteService.cs` | HttpClient contra `/api/v1/Cliente` |
| `Models/Cliente.cs` | Modelo de dominio |
| `Models/ClienteCreateViewModel.cs` | ViewModel para creación |
| `Models/ClienteUpdateViewModel.cs` | ViewModel para actualización |
| `Models/ClienteDetailsViewModel.cs` | ViewModel para detalles |
| `Models/ClienteDeleteViewModel.cs` | ViewModel para eliminación |
| `Views/Cliente/Index.cshtml` | Listado con paginación y búsqueda |
| `Views/Cliente/Create.cshtml` | Formulario de creación |
| `Views/Cliente/Details.cshtml` | Vista de detalles |
| `Views/Cliente/Update.cshtml` | Formulario de actualización |
| `Views/Cliente/Delete.cshtml` | Confirmación de eliminación |

## Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `Program.cs:96-103` | Registro DI: `AddHttpClient<IClienteService, ClienteService>` con resiliencia |
| `Pages/Shared/_Layout.cshtml:35-37` | Ítem de menú "Clientes" después de "Empleados" |
| `tests/WebDevSecOps.UnitTests/Common/ContractTestData.cs` | Datos de prueba del contrato (ValidCliente, VMs, errores) |

## Endpoints API consumidos

| Método | Endpoint | Propósito |
|--------|----------|-----------|
| `GET` | `/api/v1/Cliente?PageNumber=&PageSize=` | Listado paginado |
| `POST` | `/api/v1/Cliente` | Crear cliente |
| `GET` | `/api/v1/Cliente/{id}` | Obtener por ID |
| `PUT` | `/api/v1/Cliente/{id}` | Actualizar cliente |
| `DELETE` | `/api/v1/Cliente/{id}` | Eliminar cliente |
| `GET` | `/api/v1/Cliente/buscar?texto=&PageNumber=&PageSize=` | Búsqueda por texto |

## DTOs mapeados desde OpenAPI

| DTO | Propiedades |
|-----|-------------|
| `CliClienteCreateDto` | strNombreCliente (req, max 100), strDireccionCliente (opc, max 200), strCorreoElectronico (req), strNumeroTelefono (req, 10 dígitos) |
| `CliClienteUpdateDto` | id, strNombreCliente, strDireccionCliente, strCorreoElectronico, strNumeroTelefono, rowVersion |
| `CliClienteDeleteDto` | id, rowVersion |
| `CliClienteDto` | id, strNombreCliente, strDireccionCliente, strCorreoElectronico, strNumeroTelefono, rowVersion |

## Correcciones posteriores

- Acentos en mensajes de error (`ClienteService.cs` líneas 166, 220, 278): `autenticacion` → `autenticación`, `sesion` → `sesión`

## Build y tests

- **Build**: 0 errores, 0 warnings
- **Tests unitarios**: 107/107 exitosos
- **Tests integración (EmpleadoApiClient)**: 30/30 exitosos
