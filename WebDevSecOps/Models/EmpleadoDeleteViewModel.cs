using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class EmpleadoDeleteViewModel
{
    [Required]
    [JsonRequired]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Display(Name = "Nombre")]
    public string StrNombre { get; set; } = string.Empty;

    [Display(Name = "A. Paterno")]
    public string? StrAPaterno { get; set; }

    [Display(Name = "A. Materno")]
    public string? StrAMaterno { get; set; }

    [Display(Name = "CURP")]
    public string? StrCURP { get; set; }

    [Display(Name = "Tipo Empleado")]
    public string? StrValorTipoEmpleado { get; set; }

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
