namespace ProductMatcher.Domain.Products;

/// <summary>A catalogue item that customer-written product names are matched against.</summary>
public sealed class Product
{
    /// <summary>Vector size stored in the database; matches OpenAI text-embedding-3-small.</summary>
    public const int EmbeddingDimensions = 1536;

    private Product()
    {
        Sku = Name = Category = Unit = SearchText = string.Empty;
    }

    public Product(string sku, string name, string category, string unit, string? description)
        : this()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        Id = Guid.CreateVersion7();
        Sku = NormalizeSku(sku);
        UpdateDetails(name, category, unit, description);
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public string Category { get; private set; }

    public string Unit { get; private set; }

    public string? Description { get; private set; }

    /// <summary>The text the embedding is computed from.</summary>
    public string SearchText { get; private set; }

#pragma warning disable CA1819 // Embeddings are value data handed straight to the vector store.
    public float[]? Embedding { get; private set; }
#pragma warning restore CA1819

    public string? EmbeddingModel { get; private set; }

    public DateTimeOffset? EmbeddedAt { get; private set; }

    public bool HasEmbedding => Embedding is not null;

    /// <summary>Updates descriptive fields. Returns true when anything changed; a changed search text clears the embedding.</summary>
    public bool UpdateDetails(string name, string category, string unit, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var newName = name.Trim();
        var newCategory = category?.Trim() ?? string.Empty;
        var newUnit = unit?.Trim() ?? string.Empty;
        var newDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (newName == Name && newCategory == Category && newUnit == Unit && newDescription == Description)
        {
            return false;
        }

        Name = newName;
        Category = newCategory;
        Unit = newUnit;
        Description = newDescription;

        var newSearchText = BuildSearchText(newName, newCategory, newUnit, newDescription);
        if (newSearchText != SearchText)
        {
            SearchText = newSearchText;
            Embedding = null;
            EmbeddingModel = null;
            EmbeddedAt = null;
        }

        return true;
    }

    public void SetEmbedding(float[] embedding, string model, DateTimeOffset embeddedAt)
    {
        ArgumentNullException.ThrowIfNull(embedding);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        if (embedding.Length != EmbeddingDimensions)
        {
            throw new ArgumentException(
                $"Embedding must have {EmbeddingDimensions} dimensions but had {embedding.Length}.", nameof(embedding));
        }

        Embedding = embedding;
        EmbeddingModel = model;
        EmbeddedAt = embeddedAt;
    }

    /// <summary>SKUs are compared case-insensitively, so they are stored upper case.</summary>
    public static string NormalizeSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        return sku.Trim().ToUpperInvariant();
    }

    private static string BuildSearchText(string name, string category, string unit, string? description)
    {
        var parts = new[] { name, category, unit, description }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(". ", parts);
    }
}
