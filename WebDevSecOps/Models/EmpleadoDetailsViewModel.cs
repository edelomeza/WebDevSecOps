using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class EmpleadoDetailsViewModel
{
    [Required]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Display(Name = "Nombre")]
    [JsonPropertyName("strNombre")]
    public string StrNombre { get; set; } = string.Empty;

    [Display(Name = "A. Paterno")]
    [JsonPropertyName("strAPaterno")]
    public string? StrAPaterno { get; set; }

    [Display(Name = "A. Materno")]
    [JsonPropertyName("strAMaterno")]
    public string? StrAMaterno { get; set; }

    [Display(Name = "CURP")]
    [JsonPropertyName("strCURP")]
    public string? StrCURP { get; set; }

    [Display(Name = "Tipo Empleado")]
    public string? StrValorTipoEmpleado { get; set; }

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
