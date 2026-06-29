using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class UsuarioDetailsViewModel
{
    [Required]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Display(Name = "Nombre Completo")]
    [JsonPropertyName("strNombre")]
    public string StrNombre { get; set; } = string.Empty;

    [Display(Name = "Correo Electrónico")]
    [JsonPropertyName("strCorreoElectronico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;

    [Display(Name = "Fecha de Registro")]
    [JsonPropertyName("dteFechaRegistro")]
    public DateTime DteFechaRegistro { get; set; }

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
