using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;
using ProductMatcher.Domain.Products;
using ProductMatcher.Infrastructure.Embeddings.Caching;

namespace ProductMatcher.Infrastructure.Persistence.Configurations;

internal sealed class EmbeddingCacheEntryConfiguration : IEntityTypeConfiguration<EmbeddingCacheEntry>
{
    public void Configure(EntityTypeBuilder<EmbeddingCacheEntry> builder)
    {
        builder.ToTable("embedding_cache");
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).HasMaxLength(64).IsFixedLength();
        builder.Property(e => e.Model).HasMaxLength(128).IsRequired();
        builder.Property(e => e.Text).HasMaxLength(4096).IsRequired();
        builder.Property(e => e.Embedding)
            .HasColumnType($"vector({Product.EmbeddingDimensions})")
            .HasConversion(v => new Vector(v), v => v.ToArray());
        builder.HasIndex(e => e.CreatedAt);
    }
}
