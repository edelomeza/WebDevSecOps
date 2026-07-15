using System.Text.Json.Serialization;

namespace WebDevSecOps.Models;

public class VenCatEstado
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("strValor")]
    public string StrValor { get; set; } = string.Empty;

    [JsonPropertyName("strDescripcion")]
    public string? StrDescripcion { get; set; }
}
