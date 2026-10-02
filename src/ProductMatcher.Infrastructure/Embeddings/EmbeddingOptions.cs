namespace ProductMatcher.Infrastructure.Embeddings;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embeddings";

    /// <summary><c>OpenAI</c> or <c>Local</c>. Local needs no API key and is used for tests and offline runs.</summary>
    public string Provider { get; set; } = "Local";

    public string Model { get; set; } = "text-embedding-3-small";

    /// <summary>Falls back to the <c>OPENAI_API_KEY</c> environment variable.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Runs the background job that embeds new and changed products.</summary>
    public bool BackgroundJob { get; set; } = true;
}
