using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class ClienteDetailsViewModel
{
    [Required]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Display(Name = "Nombre del Cliente")]
    [JsonPropertyName("strNombreCliente")]
    public string StrNombreCliente { get; set; } = string.Empty;

    [Display(Name = "Direccion")]
    [JsonPropertyName("strDireccionCliente")]
    public string? StrDireccionCliente { get; set; }

    [Display(Name = "Correo Electronico")]
    [JsonPropertyName("strCorreoElectronico")]
    public string StrCorreoElectronico { get; set; } = string.Empty;

    [Display(Name = "Numero de Telefono")]
    [JsonPropertyName("strNumeroTelefono")]
    public string StrNumeroTelefono { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("rowVersion")]
    public byte[] RowVersion { get; set; } = [];
}
