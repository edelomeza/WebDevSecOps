using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class Venta
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("idCliCliente")]
    public int IdCliCliente { get; set; }

    [JsonPropertyName("strNombreCliente")]
    public string? StrNombreCliente { get; set; }

    [JsonPropertyName("idSegUsuario")]
    public int IdSegUsuario { get; set; }

    [JsonPropertyName("strNombreUsuario")]
    public string? StrNombreUsuario { get; set; }

    [JsonPropertyName("idVenCatEstado")]
    public int IdVenCatEstado { get; set; }

    [JsonPropertyName("strEstado")]
    public string? StrEstado { get; set; }

    [JsonPropertyName("dteFechaHoraCompra")]
    public DateTime? DteFechaHoraCompra { get; set; }

    [JsonPropertyName("strClaveVenta")]
    public string StrClaveVenta { get; set; } = string.Empty;

    [JsonPropertyName("rowVersion")]
    public byte[]? RowVersion { get; set; }
}
