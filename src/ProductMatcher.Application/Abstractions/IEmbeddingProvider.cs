namespace ProductMatcher.Application.Abstractions;

/// <summary>Turns text into embedding vectors.</summary>
public interface IEmbeddingProvider
{
    /// <summary>Identifier of the model, stored with each vector so stale ones can be detected.</summary>
    string Model { get; }

    /// <summary>Returns one vector per input, in the same order.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}
