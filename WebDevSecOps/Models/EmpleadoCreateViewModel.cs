using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class EmpleadoCreateViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9_ ]+$", ErrorMessage = "El nombre solo permite letras, numeros y espacios.")]
    [JsonPropertyName("strNombre")]
    [Display(Name = "Nombre")]
    public string StrNombre { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "A. Paterno no puede exceder los 50 caracteres.")]
    [JsonPropertyName("strAPaterno")]
    [Display(Name = "A. Paterno")]
    public string? StrAPaterno { get; set; }

    [StringLength(50, ErrorMessage = "A. Materno no puede exceder los 50 caracteres.")]
    [JsonPropertyName("strAMaterno")]
    [Display(Name = "A. Materno")]
    public string? StrAMaterno { get; set; }

    [StringLength(18, ErrorMessage = "CURP debe tener 18 caracteres.")]
    [JsonPropertyName("strCURP")]
    [Display(Name = "CURP")]
    public string? StrCURP { get; set; }

    [JsonPropertyName("idEmpCatTipoEmpleado")]
    [Display(Name = "Tipo Empleado")]
    public int? IdEmpCatTipoEmpleado { get; set; }
}
