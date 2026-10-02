using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ProductMatcher.Application.Abstractions;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.Infrastructure.Embeddings;

/// <summary>
/// Embeds with a locally hosted Ollama model through its native <c>/api/embed</c> endpoint.
/// Local models are smaller than the stored <c>vector(1536)</c> column, so vectors are zero-padded.
/// Padding with zeros leaves dot products, norms and therefore cosine similarity unchanged.
/// </summary>
internal sealed class OllamaEmbeddingProvider(HttpClient http, string model) : IEmbeddingProvider
{
    internal const int MaxBatchSize = 64;

    public string Model { get; } = model;

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        var results = new List<float[]>(texts.Count);
        for (var offset = 0; offset < texts.Count; offset += MaxBatchSize)
        {
            var batch = texts.Skip(offset).Take(MaxBatchSize).ToArray();
            results.AddRange(await EmbedBatchAsync(batch, cancellationToken));
        }

        return results;
    }

    internal static float[] PadAndNormalise(float[] vector, string model)
    {
        if (vector.Length == 0)
        {
            throw new InvalidOperationException($"Ollama model '{model}' returned an empty embedding. Is it an embedding model?");
        }

        if (vector.Length > Product.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"Ollama model '{model}' returned {vector.Length} dimensions, but the database stores at most {Product.EmbeddingDimensions}. " +
                "Use a model with 1536 dimensions or fewer.");
        }

        var padded = new float[Product.EmbeddingDimensions];
        vector.CopyTo(padded, 0);

        double sumSquares = 0;
        foreach (var v in padded)
        {
            sumSquares += (double)v * v;
        }

        var norm = Math.Sqrt(sumSquares);
        if (norm > 0 && Math.Abs(norm - 1) > 1e-4)
        {
            for (var i = 0; i < padded.Length; i++)
            {
                padded[i] = (float)(padded[i] / norm);
            }
        }

        return padded;
    }

    private async Task<IReadOnlyList<float[]>> EmbedBatchAsync(string[] batch, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("api/embed", new EmbedRequest(Model, batch), cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Could not reach Ollama at {http.BaseAddress}. Is it running? ({ex.Message})", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"Ollama at {http.BaseAddress} timed out after {http.Timeout.TotalSeconds:0}s embedding with '{Model}'.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var hint = (int)response.StatusCode == 404 ? $" Try: ollama pull {Model}" : "";
                throw new InvalidOperationException(
                    $"Ollama returned {(int)response.StatusCode} for model '{Model}': {Truncate(body)}.{hint}");
            }

            var payload = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Ollama returned an empty response.");
            if (payload.Embeddings is null || payload.Embeddings.Count != batch.Length)
            {
                throw new InvalidOperationException(
                    $"Ollama returned {payload.Embeddings?.Count ?? 0} embeddings for {batch.Length} inputs.");
            }

            return payload.Embeddings.Select(v => PadAndNormalise(v, Model)).ToList();
        }
    }

    private static string Truncate(string s) => s.Length <= 300 ? s : s[..300] + "...";

    private sealed record EmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string[] Input);

    private sealed record EmbedResponse(
        [property: JsonPropertyName("embeddings")] List<float[]>? Embeddings);
}
