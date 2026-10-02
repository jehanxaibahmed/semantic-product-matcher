using System.Net;
using System.Text;
using System.Text.Json;
using ProductMatcher.Domain.Products;
using ProductMatcher.Infrastructure.Embeddings;
using ProductMatcher.Infrastructure.Embeddings.Caching;

namespace ProductMatcher.UnitTests.Embeddings;

public class OllamaEmbeddingProviderTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, string, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(Uri? Uri, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((request.RequestUri, body));
            return respond(request, body);
        }
    }

    private static HttpResponseMessage Json(object payload, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };

    private static (OllamaEmbeddingProvider Provider, StubHandler Handler) Create(
        Func<HttpRequestMessage, string, HttpResponseMessage> respond, string model = "nomic-embed-text")
    {
        var handler = new StubHandler(respond);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://ollama.test:11434/") };
        return (new OllamaEmbeddingProvider(http, model), handler);
    }

    private static float[][] Embeddings(string body, int dims) =>
        JsonDocument.Parse(body).RootElement.GetProperty("input").EnumerateArray()
            .Select((_, i) => Enumerable.Range(0, dims).Select(d => d == i ? 2f : 0f).ToArray()).ToArray();

    [Fact]
    public async Task Posts_a_batch_to_api_embed_and_pads_to_the_stored_dimension()
    {
        var (provider, handler) = Create((_, body) => Json(new { embeddings = Embeddings(body, 768) }));

        var vectors = await provider.EmbedAsync(["red peppers", "cherry tomatoes"], default);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("http://ollama.test:11434/api/embed", request.Uri!.ToString());
        var doc = JsonDocument.Parse(request.Body).RootElement;
        Assert.Equal("nomic-embed-text", doc.GetProperty("model").GetString());
        Assert.Equal(["red peppers", "cherry tomatoes"], doc.GetProperty("input").EnumerateArray().Select(e => e.GetString()));

        Assert.Equal(2, vectors.Count);
        Assert.All(vectors, v => Assert.Equal(Product.EmbeddingDimensions, v.Length));
        Assert.All(vectors, v => Assert.All(v.Skip(768), x => Assert.Equal(0f, x)));
    }

    [Fact]
    public async Task Normalises_vectors_to_unit_length()
    {
        var (provider, _) = Create((_, body) => Json(new { embeddings = Embeddings(body, 1024) }), "bge-m3");

        var vector = Assert.Single(await provider.EmbedAsync(["x"], default));

        Assert.Equal(1f, MathF.Sqrt(vector.Sum(v => v * v)), 4);
    }

    [Fact]
    public async Task Padding_does_not_change_cosine_similarity()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [3f, 1f, 2f];
        var rawCosine = a.Zip(b, (x, y) => x * y).Sum() / (MathF.Sqrt(a.Sum(x => x * x)) * MathF.Sqrt(b.Sum(x => x * x)));
        var (provider, _) = Create((_, _) => Json(new { embeddings = new[] { a, b } }));

        var v = await provider.EmbedAsync(["a", "b"], default);

        Assert.Equal(rawCosine, v[0].Zip(v[1], (x, y) => x * y).Sum(), 5);
    }

    [Fact]
    public async Task Fails_clearly_when_the_model_returns_more_dimensions_than_stored()
    {
        var (provider, _) = Create((_, body) => Json(new { embeddings = Embeddings(body, 3072) }), "big-model");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.EmbedAsync(["x"], default));

        Assert.Contains("3072", ex.Message);
        Assert.Contains("big-model", ex.Message);
    }

    [Fact]
    public async Task Splits_large_inputs_into_batches_and_keeps_order()
    {
        var (provider, handler) = Create((_, body) => Json(new { embeddings = Embeddings(body, 8) }));
        var texts = Enumerable.Range(0, OllamaEmbeddingProvider.MaxBatchSize + 5).Select(i => $"t{i}").ToList();

        var vectors = await provider.EmbedAsync(texts, default);

        Assert.Equal(texts.Count, vectors.Count);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Empty_input_makes_no_request()
    {
        var (provider, handler) = Create((_, _) => Json(new { embeddings = Array.Empty<float[]>() }));

        Assert.Empty(await provider.EmbedAsync([], default));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Missing_model_gives_a_pull_hint()
    {
        var (provider, _) = Create((_, _) => Json(new { error = "model \"bge-m3\" not found" }, HttpStatusCode.NotFound), "bge-m3");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.EmbedAsync(["x"], default));

        Assert.Contains("ollama pull bge-m3", ex.Message);
    }

    [Fact]
    public async Task Count_mismatch_is_an_error()
    {
        var (provider, _) = Create((_, _) => Json(new { embeddings = new[] { new[] { 1f } } }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.EmbedAsync(["a", "b"], default));
    }

    [Fact]
    public async Task Unreachable_server_is_reported_with_the_address()
    {
        var (provider, _) = Create((_, _) => throw new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.EmbedAsync(["x"], default));

        Assert.Contains("ollama.test:11434", ex.Message);
    }

    [Fact]
    public void Cache_keys_differ_per_model_so_providers_never_share_vectors()
    {
        Assert.NotEqual(
            CachedEmbeddingProvider.CacheKey("nomic-embed-text", "red peppers"),
            CachedEmbeddingProvider.CacheKey("bge-m3", "red peppers"));
        Assert.NotEqual(
            CachedEmbeddingProvider.CacheKey("nomic-embed-text", "red peppers"),
            CachedEmbeddingProvider.CacheKey("text-embedding-3-small", "red peppers"));
    }
}
