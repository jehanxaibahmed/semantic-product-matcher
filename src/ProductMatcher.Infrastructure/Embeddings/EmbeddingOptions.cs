namespace ProductMatcher.Infrastructure.Embeddings;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embeddings";

    /// <summary><c>OpenAI</c>, <c>Ollama</c> or <c>Local</c>. Local needs no API key and is used for tests and offline runs; Ollama uses a locally hosted model.</summary>
    public string Provider { get; set; } = "Local";

    public string Model { get; set; } = "text-embedding-3-small";

    /// <summary>Falls back to the <c>OPENAI_API_KEY</c> environment variable.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Optional custom endpoint for OpenAI-compatible providers.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Ollama server address, used when <c>Provider</c> is <c>Ollama</c>.</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Per-request timeout for Ollama. The first call can be slow while the model loads.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Runs the background job that embeds new and changed products.</summary>
    public bool BackgroundJob { get; set; } = true;

    public EmbeddingCacheOptions Cache { get; set; } = new();
}

public sealed class EmbeddingCacheOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum vectors kept in memory (about 6 KB each at 1536 dimensions).</summary>
    public int MemoryEntries { get; set; } = 10_000;
}
