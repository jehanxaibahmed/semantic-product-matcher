using System.Globalization;
using System.Text;
using ProductMatcher.Domain.Products;
using Microsoft.Extensions.AI;

namespace ProductMatcher.Infrastructure.Embeddings;

/// <summary>
/// Deterministic, offline embedder using the hashing trick over words and character trigrams.
/// It catches spelling variants and word overlap but not synonyms. It is a baseline for tests
/// and benchmarks, not a replacement for a real model.
/// </summary>
internal sealed class LocalHashingEmbeddingProvider : IEmbeddingGenerator<string, Embedding<float>>
{
    private const float WordWeight = 1.0f;
    private const float TrigramWeight = 0.5f;

    public EmbeddingGeneratorMetadata Metadata { get; } = new("local-hashing-v1");

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var embeddings = values.Select(text => new Embedding<float>(Embed(text))).ToList();
        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }
    
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return Metadata;
        }
        return this.GetType() == serviceType ? this : null;
    }
    
    public void Dispose() {}

    internal static float[] Embed(string text)
    {
        var vector = new float[Product.EmbeddingDimensions];
        foreach (var word in Tokenize(text))
        {
            Add(vector, "w:" + word, WordWeight);
            var padded = "#" + word + "#";
            for (var i = 0; i + 3 <= padded.Length; i++)
            {
                Add(vector, string.Concat("t:", padded.AsSpan(i, 3)), TrigramWeight);
            }
        }

        Normalize(vector);
        return vector;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var builder = new StringBuilder();
        foreach (var ch in text.ToLower(CultureInfo.InvariantCulture))
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (builder.Length > 0)
            {
                yield return builder.ToString();
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static void Add(float[] vector, string feature, float weight)
    {
        var hash = Fnv1a(feature);
        var index = (int)(hash % (uint)vector.Length);
        var sign = (hash & 0x8000_0000) == 0 ? 1f : -1f;
        vector[index] += sign * weight;
    }

    private static uint Fnv1a(string value)
    {
        var hash = 2166136261u;
        foreach (var ch in value)
        {
            hash ^= ch;
            hash *= 16777619u;
        }

        return hash;
    }

    private static void Normalize(float[] vector)
    {
        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm == 0)
        {
            return;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }
    }
}
