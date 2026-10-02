using ProductMatcher.Application.Matching;

namespace ProductMatcher.Application.Abstractions;

/// <summary>Nearest-neighbour search over product embeddings.</summary>
public interface IProductSearch
{
    /// <summary>Returns up to <paramref name="topK"/> products ordered by cosine similarity, highest first.</summary>
    Task<IReadOnlyList<MatchCandidate>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken);
}
