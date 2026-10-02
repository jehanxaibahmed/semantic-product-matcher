using ProductMatcher.Application.Abstractions;
using ProductMatcher.Domain.History;

namespace ProductMatcher.Application.Matching;

/// <summary>Records which product a customer actually meant, so their future results rank it higher.</summary>
public sealed class MatchFeedbackService(
    IProductRepository products,
    IMatchHistoryRepository history,
    TimeProvider clock)
{
    /// <summary>Returns false when the SKU is unknown.</summary>
    public async Task<bool> ConfirmAsync(string customerId, string query, string sku, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId) || customerId.Trim().Length > MatchConfirmation.MaxCustomerIdLength)
        {
            throw new MatchValidationException(
                $"customerId is required and must be at most {MatchConfirmation.MaxCustomerIdLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(query) || query.Length > MatchService.MaxQueryLength)
        {
            throw new MatchValidationException($"query is required and must be at most {MatchService.MaxQueryLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new MatchValidationException("sku is required.");
        }

        var product = await products.GetBySkuAsync(sku, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return false;
        }

        history.Add(new MatchConfirmation(customerId, query, QueryNormalizer.Normalize(query), product.Id, clock.GetUtcNow()));
        await history.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
