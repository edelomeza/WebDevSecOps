using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class SegUsuarioAutocompleteDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strNombre")]
    public string StrNombre { get; set; } = string.Empty;
}
