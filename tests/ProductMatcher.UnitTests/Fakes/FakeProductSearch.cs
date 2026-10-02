using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Matching;

namespace ProductMatcher.UnitTests.Fakes;

internal sealed class FakeProductSearch(params MatchCandidate[] candidates) : IProductSearch
{
    public int Calls { get; private set; }

    public List<int> RequestedTopK { get; } = [];

    public Task<IReadOnlyList<MatchCandidate>> SearchAsync(
        float[] queryVector, int topK, IReadOnlyCollection<Guid> alwaysInclude, CancellationToken cancellationToken)
    {
        Calls++;
        RequestedTopK.Add(topK);
        var nearest = candidates.OrderByDescending(c => c.Similarity).Take(topK);
        var pinned = candidates.Where(c => alwaysInclude.Contains(c.ProductId));
        return Task.FromResult<IReadOnlyList<MatchCandidate>>(
            nearest.Union(pinned).OrderByDescending(c => c.Similarity).ToList());
    }
}
