using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class VentaDetalle
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("idVenVenta")]
    public int IdVenVenta { get; set; }

    [JsonPropertyName("idProProducto")]
    public int IdProProducto { get; set; }

    [JsonPropertyName("strNombreProducto")]
    public string? StrNombreProducto { get; set; }

    [JsonPropertyName("decPrecio")]
    public decimal DecPrecio { get; set; }

    [JsonPropertyName("intPiezaVenta")]
    public int IntPiezasVenta { get; set; }

    [JsonPropertyName("decTotalVenta")]
    public decimal DecTotalVenta { get; set; }

    [JsonPropertyName("rowVersion")]
    public byte[]? RowVersion { get; set; }
}
