using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class VentaCreateViewModel
{
    [Required(ErrorMessage = "El cliente es obligatorio.")]
    [JsonRequired]
    [JsonPropertyName("idCliCliente")]
    [Display(Name = "Cliente")]
    public int IdCliCliente { get; set; }

    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [JsonRequired]
    [JsonPropertyName("idSegUsuario")]
    [Display(Name = "Usuario")]
    public int IdSegUsuario { get; set; }

    public string? StrNombreCliente { get; set; }

    public string? StrNombreUsuario { get; set; }
}
