using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class ProductoDetailsViewModel
{
    [Required]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Display(Name = "Nombre del Producto")]
    [JsonPropertyName("strNombreProducto")]
    public string StrNombreProducto { get; set; } = string.Empty;

    [Display(Name = "URL de Imagen")]
    [JsonPropertyName("strURLImagen")]
    public string? StrURLImagen { get; set; }

    [Display(Name = "Descripcion")]
    [JsonPropertyName("strDescripcion")]
    public string? StrDescripcion { get; set; }

    [Display(Name = "Existencia")]
    [JsonPropertyName("intNumeroExistencia")]
    public int IntNumeroExistencia { get; set; }

    [Display(Name = "Precio")]
    [JsonPropertyName("decPrecio")]
    public decimal DecPrecio { get; set; }

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
