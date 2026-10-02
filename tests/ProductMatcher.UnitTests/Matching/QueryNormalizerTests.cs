using ProductMatcher.Application.Matching;

namespace ProductMatcher.UnitTests.Matching;

public class QueryNormalizerTests
{
    [Theory]
    [InlineData("2 boxes of the large red peppers", "large red peppers")]
    [InlineData("a case of cherry tomatoes please", "cherry tomatoes")]
    [InlineData("3 x Whole Milk", "whole milk")]
    [InlineData("1x salmon fillets", "salmon fillets")]
    [InlineData("two bags of rocket, thanks!", "rocket")]
    [InlineData("can we get some blue roll", "blue roll")]
    [InlineData("Beef mince 5% fat", "beef mince 5% fat")]
    [InlineData("our usual brown onions 25kg", "brown onions 25kg")]
    public void Strips_order_phrasing(string input, string expected)
    {
        Assert.Equal(expected, QueryNormalizer.Normalize(input));
    }

    [Fact]
    public void Keeps_a_quantity_only_query_rather_than_returning_empty()
    {
        Assert.Equal("2 boxes", QueryNormalizer.Normalize("2  Boxes"));
    }
}
