using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class ProProductoAutocompleteDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strTextoAutocomplete")]
    public string StrTextoAutocomplete { get; set; } = string.Empty;
}
