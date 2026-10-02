using ProductMatcher.Domain.Products;

namespace ProductMatcher.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Sku_is_normalised_to_upper_case()
    {
        var product = new Product("  fp-1001 ", "Red Peppers", "Fresh Produce", "5kg box", null);

        Assert.Equal("FP-1001", product.Sku);
    }

    [Fact]
    public void Search_text_combines_descriptive_fields()
    {
        var product = new Product("FP-1001", "Red Peppers Large", "Fresh Produce", "5kg box", "Large red bell peppers");

        Assert.Equal("Red Peppers Large. Fresh Produce. 5kg box. Large red bell peppers", product.SearchText);
    }

    [Fact]
    public void Changing_details_clears_the_embedding()
    {
        var product = new Product("FP-1001", "Red Peppers", "Fresh Produce", "5kg box", null);
        product.SetEmbedding(new float[Product.EmbeddingDimensions], "m", DateTimeOffset.UnixEpoch);

        var changed = product.UpdateDetails("Red Peppers Large", "Fresh Produce", "5kg box", null);

        Assert.True(changed);
        Assert.False(product.HasEmbedding);
        Assert.Null(product.EmbeddingModel);
    }

    [Fact]
    public void Identical_details_report_no_change_and_keep_the_embedding()
    {
        var product = new Product("FP-1001", "Red Peppers", "Fresh Produce", "5kg box", null);
        product.SetEmbedding(new float[Product.EmbeddingDimensions], "m", DateTimeOffset.UnixEpoch);

        var changed = product.UpdateDetails(" Red Peppers ", "Fresh Produce", "5kg box", "  ");

        Assert.False(changed);
        Assert.True(product.HasEmbedding);
    }

    [Fact]
    public void Embedding_with_wrong_dimensions_is_rejected()
    {
        var product = new Product("FP-1001", "Red Peppers", "Fresh Produce", "5kg box", null);

        Assert.Throws<ArgumentException>(() => product.SetEmbedding(new float[3], "m", DateTimeOffset.UnixEpoch));
    }
}
