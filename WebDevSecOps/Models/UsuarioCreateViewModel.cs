using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class UsuarioCreateViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9_ ]+$", ErrorMessage = "El nombre solo permite letras, números y espacios.")]
    [JsonPropertyName("strNombre")]
    [Display(Name = "Nombre")]
    public string StrNombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "La contraseña debe contener mayúsculas, minúsculas, un número y un carácter especial.")]
    [DataType(DataType.Password)]
    [JsonPropertyName("strPWD")]
    [Display(Name = "Contraseña")]
    public string StrPWD { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [StringLength(50, ErrorMessage = "El correo no puede exceder los 50 caracteres.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [JsonPropertyName("strCorreoElectronico")]
    [Display(Name = "Correo Electronico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;
}
