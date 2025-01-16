using System.Text.Json.Serialization;

namespace Blazor.AI.SeedData;

public class RAGSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("contentVector")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public float[] ContentVector { get; set; } = [];
}
