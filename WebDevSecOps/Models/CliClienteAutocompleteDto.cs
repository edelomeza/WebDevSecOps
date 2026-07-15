using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class CliClienteAutocompleteDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strNombreCliente")]
    public string StrNombreCliente { get; set; } = string.Empty;
}
