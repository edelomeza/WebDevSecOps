using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    [Display(Name = "Usuario")]
    [JsonPropertyName("user")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordar sesión")]
    [JsonPropertyName("rememberMe")]
    public bool RememberMe { get; set; }
}
