using System.Text.Json.Serialization;

namespace ProjetDelta.Data.DataClasses
{
    public class ArtefactDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("nom")]
        public string? Nom { get; set; }

        [JsonPropertyName("valeur")]
        public float? Valeur { get; set; }

        [JsonPropertyName("valeur2")]
        public float? Valeur2 { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("rarity")]
        public string? Rarity { get; set; }

        // Bonuses d'attributs optionnels pour les artefacts generiques
        [JsonPropertyName("force")]
        public float? Force { get; set; }

        [JsonPropertyName("dex")]
        public float? Dex { get; set; }

        [JsonPropertyName("constitution")]
        public float? Constitution { get; set; }

        [JsonPropertyName("intel")]
        public float? Intel { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }
    }
}
