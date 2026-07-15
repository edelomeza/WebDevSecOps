using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class ProductoDeleteViewModel
{
    [Required]
    [JsonRequired]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Display(Name = "Nombre del Producto")]
    public string StrNombreProducto { get; set; } = string.Empty;

    [Display(Name = "Descripcion")]
    public string? StrDescripcion { get; set; }

    [Display(Name = "Existencia")]
    [JsonRequired]
    public int IntNumeroExistencia { get; set; }

    [Display(Name = "Precio")]
    [JsonRequired]
    public decimal DecPrecio { get; set; }

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
