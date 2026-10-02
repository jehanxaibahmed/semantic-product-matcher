using ProductMatcher.Application.Abstractions;
using ProductMatcher.Application.Matching;

namespace ProductMatcher.UnitTests.Fakes;

internal sealed class FakeProductSearch(params MatchCandidate[] candidates) : IProductSearch
{
    public int Calls { get; private set; }

    public Task<IReadOnlyList<MatchCandidate>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult<IReadOnlyList<MatchCandidate>>(candidates.Take(topK).ToList());
    }
}
