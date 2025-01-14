using System.Text.Json.Serialization;

namespace Blazor.AI.SeedData;

public class RAGSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("contentVector")]
    public float[] ContentVector { get; set; }
}
