using System.Net;
using System.Text.Json;
using WebDevSecOps.Models;

namespace WebDevSecOps.UnitTests.Common;

public static class ContractTestData
{
    public static LoginRequest ValidLoginRequest => new()
    {
        Username = "admin",
        Password = "FakePass1!"
    };

    public static LoginRequest LoginRequestWithEmptyValues => new()
    {
        Username = "",
        Password = ""
    };

    public static LoginRequest LoginRequestWithSpecialChars => new()
    {
        Username = "userñáéíóú",
        Password = "p@ss#W0rD!\"'&<>"
    };

    public static LoginResponse ValidLoginResponse => new()
    {
        Token = "eyJhbGciOiJIUzI1NiJ9.eyJwdXJwb3NlIjoidGVzdC1vbmx5LW5vdC1hLXJlYWwtc2VjcmV0In0.dGhpcy1pcy1hLXRlc3Qtc2lnbmF0dXJl",
        ExpiresIn = 3600
    };

    public static LoginResponse LoginResponseWithSpecialCharsToken => new()
    {
        Token = "tok+en/with=special!chars@and#symbols$%^&*()",
        ExpiresIn = 1800
    };

    public static ErrorResponse UnauthorizedError => new()
    {
        Title = "Unauthorized",
        Detail = "Credenciales inv\u00e1lidas. Verifique su usuario y contrase\u00f1a.",
        Status = 401
    };

    public static ErrorResponse ValidationError => new()
    {
        Title = "Validation Error",
        Detail = "El campo Usuario es obligatorio.",
        Status = 400
    };

    public static ErrorResponse ServerError => new()
    {
        Title = "Internal Server Error",
        Detail = "Ha ocurrido un error interno en el servidor.",
        Status = 500
    };

    public static ErrorResponse ErrorWithMissingDetail => new()
    {
        Title = "Custom error",
        Detail = "",
        Status = 400
    };

    public static ErrorResponse ErrorWithEmptyFields => new()
    {
        Title = "",
        Detail = "",
        Status = 400
    };

    public static object ResponseWithExtraFields => new
    {
        token = "valid-token-extra",
        expiresIn = 7200,
        refreshToken = "refresh-value",
        userRole = "admin"
    };

    public static object ResponseWithNullToken => new
    {
        token = (string?)null,
        expiresIn = 3600
    };

    public static object ResponseWithEmptyToken => new
    {
        token = "",
        expiresIn = 3600
    };

    public static object ResponseMissingTokenField => new
    {
        expiresIn = 3600
    };

    public const string InvalidJson = "not-valid-json-at-all";

    // ========================================================================
    // Usuario domain model
    // ========================================================================

    private static readonly byte[] _rowVersion = [0x01, 0x02, 0x03, 0x04];

    public static Usuario ValidUsuario => new()
    {
        Id = 1,
        StrNombre = "Juan P\u00e9rez",
        StrCorreoElectronico = "juan.perez@example.com",
        DteFechaRegistro = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
        RowVersion = _rowVersion
    };

    public static object ResponseUsuarioMissingFields => new
    {
        id = 1,
        strNombre = "Incomplete"
    };

    // ========================================================================
    // PaginatedResponse<Usuario>
    // ========================================================================

    public static PaginatedResponse<Usuario> ValidPaginatedResponse => new()
    {
        Items = [ValidUsuario],
        TotalCount = 1,
        PageNumber = 1,
        PageSize = 10,
        TotalPages = 1
    };

    public static PaginatedResponse<Usuario> EmptyPaginatedResponse => new()
    {
        Items = [],
        TotalCount = 0,
        PageNumber = 1,
        PageSize = 10,
        TotalPages = 0
    };

    // ========================================================================
    // View models
    // ========================================================================

    public static UsuarioCreateViewModel ValidCreateViewModel => new()
    {
        StrNombre = "Nuevo Usuario",
        StrPWD = "FakePass1!",
        StrCorreoElectronico = "nuevo@example.com"
    };

    public static UsuarioUpdateViewModel ValidUpdateViewModel => new()
    {
        Id = 1,
        StrNombre = "Usuario Actualizado",
        StrPWD = null,
        StrCorreoElectronico = "actualizado@example.com",
        RowVersion = _rowVersion
    };

    public static object ValidDeleteRequestBody => new
    {
        id = 1,
        rowVersion = _rowVersion
    };

    // ========================================================================
    // ApiErrorResponse variants
    // ========================================================================

    public static ApiErrorResponse ValidationApiError => new()
    {
        Title = "Validation Error",
        Detail = "One or more validation errors occurred.",
        Status = 400,
        Errors = new Dictionary<string, string[]>
        {
            ["strNombre"] = ["El nombre es obligatorio."]
        }
    };

    public static ApiErrorResponse ConflictApiError => new()
    {
        Title = "Conflict",
        Detail = "The record was modified by another user.",
        Status = 409
    };

    public static ApiErrorResponse GeneralApiError => new()
    {
        Title = "Bad Request",
        Detail = "Invalid data provided.",
        Status = 400,
        Errors = null
    };

    // ========================================================================
    // Constantes
    // ========================================================================

    // ========================================================================
    // Empleado domain model
    // ========================================================================

    public static EmpCatTipoEmpleado ValidTipoEmpleado => new()
    {
        Id = 1,
        StrValor = "Administrativo",
        StrDescripcion = "Personal administrativo"
    };

    public static Empleado ValidEmpleado => new()
    {
        Id = 1,
        StrNombre = "Carlos Lopez",
        StrAPaterno = "Lopez",
        StrAMaterno = "Garcia",
        StrCURP = "LOPC800101HDFRRN09",
        IdEmpCatTipoEmpleado = 1,
        RowVersion = _rowVersion
    };

    public static PaginatedResponse<Empleado> ValidEmpleadoPaginatedResponse => new()
    {
        Items = [ValidEmpleado],
        TotalCount = 1,
        PageNumber = 1,
        PageSize = 10,
        TotalPages = 1
    };

    public static PaginatedResponse<Empleado> EmptyEmpleadoPaginatedResponse => new()
    {
        Items = [],
        TotalCount = 0,
        PageNumber = 1,
        PageSize = 10,
        TotalPages = 0
    };

    public static PaginatedResponse<EmpCatTipoEmpleado> ValidTipoEmpleadoPaginatedResponse => new()
    {
        Items = [ValidTipoEmpleado],
        TotalCount = 1,
        PageNumber = 1,
        PageSize = 100,
        TotalPages = 1
    };

    public static EmpleadoCreateViewModel ValidEmpleadoCreateViewModel => new()
    {
        StrNombre = "Nuevo Empleado",
        StrAPaterno = "Paterno",
        StrAMaterno = "Materno",
        StrCURP = "XXXX000101HDFRRN09",
        IdEmpCatTipoEmpleado = 1
    };

    public static EmpleadoUpdateViewModel ValidEmpleadoUpdateViewModel => new()
    {
        Id = 1,
        StrNombre = "Empleado Actualizado",
        StrAPaterno = "Paterno",
        StrAMaterno = "Materno",
        StrCURP = "XXXX000101HDFRRN09",
        IdEmpCatTipoEmpleado = 1,
        RowVersion = _rowVersion
    };

    public static object ValidEmpleadoDeleteRequestBody => new
    {
        id = 1,
        rowVersion = _rowVersion
    };

    public static ApiErrorResponse EmpleadoValidationApiError => new()
    {
        Title = "Validation Error",
        Detail = "One or more validation errors occurred.",
        Status = 400,
        Errors = new Dictionary<string, string[]>
        {
            ["strNombre"] = ["El nombre es obligatorio."]
        }
    };

    public static ApiErrorResponse EmpleadoConflictApiError => new()
    {
        Title = "Conflict",
        Detail = "The record was modified by another user.",
        Status = 409
    };

    public const string TestToken = "eyJhbGciOiJIUzI1NiJ9.dGVzdC1kYXRhLW5vdC1hLXJlYWwtc2VjcmV0";
    public const string ConflictMessage = "El registro fue modificado por otro usuario. Recargue la p\u00e1gina e intente nuevamente.";
    public const string MissingTokenMessage = "Error de autenticaci\u00f3n. Inicie sesi\u00f3n nuevamente.";
    public const string ConnectionErrorMessage = "Error de conexi\u00f3n con el servidor. Verifique su conexi\u00f3n e intente nuevamente.";
    public const string UnexpectedErrorMessage = "Error inesperado. Intente nuevamente.";
}
