using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class ClienteUpdateViewModel
{
    [Required]
    [JsonRequired]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ ]+$", ErrorMessage = "El nombre solo permite letras, numeros y espacios.")]
    [JsonPropertyName("strNombreCliente")]
    [Display(Name = "Nombre del Cliente")]
    public string StrNombreCliente { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "La direccion no puede exceder los 200 caracteres.")]
    [JsonPropertyName("strDireccionCliente")]
    [Display(Name = "Direccion")]
    public string? StrDireccionCliente { get; set; }

    [Required(ErrorMessage = "El correo electronico es obligatorio.")]
    [StringLength(100, ErrorMessage = "El correo no puede exceder los 100 caracteres.")]
    [EmailAddress(ErrorMessage = "El formato del correo no es valido.")]
    [JsonPropertyName("strCorreoElectronico")]
    [Display(Name = "Correo Electronico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;

    [Required(ErrorMessage = "El numero de telefono es obligatorio.")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "El telefono debe tener exactamente 10 digitos.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "El telefono debe tener exactamente 10 digitos numericos.")]
    [JsonPropertyName("strNumeroTelefono")]
    [Display(Name = "Numero de Telefono")]
    public string StrNumeroTelefono { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
