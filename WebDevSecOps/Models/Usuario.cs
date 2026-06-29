using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class Usuario
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strNombre")]
    public string StrNombre { get; set; } = string.Empty;

    [JsonPropertyName("strCorreoElectronico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;

    [JsonPropertyName("dteFechaRegistro")]
    public DateTime DteFechaRegistro { get; set; }

    [JsonPropertyName("rowVersion")]
    public byte[]? RowVersion { get; set; }
}
