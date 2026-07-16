using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class VentaProductosViewModel
{
    public int Id { get; set; }

    [Display(Name = "Fecha y Hora")]
    public DateTime? DteFechaHoraCompra { get; set; }

    [Display(Name = "Clave Venta")]
    public string? StrClaveVenta { get; set; }

    [Display(Name = "Cliente")]
    public string? StrNombreCliente { get; set; }

    [Display(Name = "Usuario")]
    public string? StrNombreUsuario { get; set; }

    [Display(Name = "Estado")]
    public int IdVenCatEstado { get; set; }

    public byte[]? RowVersion { get; set; }

    public List<VentaDetalle> Detalles { get; set; } = [];
}
