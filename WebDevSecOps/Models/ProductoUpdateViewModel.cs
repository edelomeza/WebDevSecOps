using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class ProductoUpdateViewModel
{
    [Required]
    [JsonRequired]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ ]+$", ErrorMessage = "El nombre solo permite letras, numeros y espacios.")]
    [JsonPropertyName("strNombreProducto")]
    [Display(Name = "Nombre del Producto")]
    public string StrNombreProducto { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "La URL de la imagen no puede exceder los 300 caracteres.")]
    [Url(ErrorMessage = "La URL de la imagen no es valida.")]
    [JsonPropertyName("strURLImagen")]
    [Display(Name = "URL de Imagen")]
    public string? StrURLImagen { get; set; }

    [StringLength(250, ErrorMessage = "La descripcion no puede exceder los 250 caracteres.")]
    [JsonPropertyName("strDescripcion")]
    [Display(Name = "Descripcion")]
    public string? StrDescripcion { get; set; }

    [Required(ErrorMessage = "La existencia es obligatoria.")]
    [Range(0, 2147483647, ErrorMessage = "La existencia debe estar entre 0 y 2147483647.")]
    [JsonPropertyName("intNumeroExistencia")]
    [Display(Name = "Existencia")]
    public int IntNumeroExistencia { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, 9999999.99, ErrorMessage = "El precio debe estar entre 0.01 y 9999999.99.")]
    [DataType(DataType.Currency)]
    [JsonPropertyName("decPrecio")]
    [Display(Name = "Precio")]
    public decimal DecPrecio { get; set; }

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
