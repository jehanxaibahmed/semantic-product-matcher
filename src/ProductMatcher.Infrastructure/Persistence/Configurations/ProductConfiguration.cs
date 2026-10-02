using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).HasMaxLength(64).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Unit).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2048);
        builder.Property(p => p.SearchText).HasMaxLength(4096).IsRequired();
        builder.Property(p => p.EmbeddingModel).HasMaxLength(128);

        // The domain keeps a plain float[]; pgvector's Vector type stays inside Infrastructure.
        builder.Property(p => p.Embedding)
            .HasColumnType($"vector({Product.EmbeddingDimensions})")
            .HasConversion(
                v => v == null ? null : new Vector(v),
                v => v == null ? null : v.ToArray());

        builder.HasIndex(p => p.Embedding)
            .HasMethod("hnsw")
            .HasOperators("vector_cosine_ops");

        builder.Ignore(p => p.HasEmbedding);
    }
}
