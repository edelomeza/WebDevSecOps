using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class VentaAgregarProductoViewModel
{
    [Required(ErrorMessage = "La venta es obligatoria.")]
    [JsonRequired]
    [JsonPropertyName("idVenVenta")]
    public int IdVenVenta { get; set; }

    [Required(ErrorMessage = "El producto es obligatorio.")]
    [JsonRequired]
    [JsonPropertyName("idProProducto")]
    public int IdProProducto { get; set; }

    [Required(ErrorMessage = "El numero de piezas es obligatorio.")]
    [Range(1, int.MaxValue, ErrorMessage = "Las piezas deben ser al menos 1.")]
    [JsonPropertyName("intPiezaVenta")]
    public int IntPiezasVenta { get; set; }
}
