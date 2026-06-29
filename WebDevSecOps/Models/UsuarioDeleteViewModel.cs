using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class UsuarioDeleteViewModel
{
    [Required]
    [JsonRequired]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Display(Name = "Nombre")]
    public string StrNombre { get; set; } = string.Empty;

    [Display(Name = "Correo Electrónico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
