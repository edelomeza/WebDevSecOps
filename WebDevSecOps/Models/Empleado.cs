using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class Empleado
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strNombre")]
    public string StrNombre { get; set; } = string.Empty;

    [JsonPropertyName("strAPaterno")]
    public string? StrAPaterno { get; set; }

    [JsonPropertyName("strAMaterno")]
    public string? StrAMaterno { get; set; }

    [JsonPropertyName("strCURP")]
    public string? StrCURP { get; set; }

    [JsonPropertyName("idEmpCatTipoEmpleado")]
    public int? IdEmpCatTipoEmpleado { get; set; }

    [JsonPropertyName("rowVersion")]
    public byte[]? RowVersion { get; set; }
}
