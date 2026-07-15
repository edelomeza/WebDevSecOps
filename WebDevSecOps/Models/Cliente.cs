using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class Cliente
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strNombreCliente")]
    public string StrNombreCliente { get; set; } = string.Empty;

    [JsonPropertyName("strDireccionCliente")]
    public string? StrDireccionCliente { get; set; }

    [JsonPropertyName("strCorreoElectronico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;

    [JsonPropertyName("strNumeroTelefono")]
    public string StrNumeroTelefono { get; set; } = string.Empty;

    [JsonPropertyName("rowVersion")]
    public byte[]? RowVersion { get; set; }
}
