using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class Producto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strNombreProducto")]
    public string StrNombreProducto { get; set; } = string.Empty;

    [JsonPropertyName("strURLImagen")]
    public string? StrURLImagen { get; set; }

    [JsonPropertyName("strDescripcion")]
    public string? StrDescripcion { get; set; }

    [JsonPropertyName("intNumeroExistencia")]
    public int IntNumeroExistencia { get; set; }

    [JsonPropertyName("decPrecio")]
    public decimal DecPrecio { get; set; }

    [JsonPropertyName("rowVersion")]
    public byte[]? RowVersion { get; set; }
}
