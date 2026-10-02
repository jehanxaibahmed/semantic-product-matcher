using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductMatcher.Domain.History;
using ProductMatcher.Domain.Products;

namespace ProductMatcher.Infrastructure.Persistence.Configurations;

internal sealed class MatchConfirmationConfiguration : IEntityTypeConfiguration<MatchConfirmation>
{
    public void Configure(EntityTypeBuilder<MatchConfirmation> builder)
    {
        builder.ToTable("match_history");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.CustomerId).HasMaxLength(MatchConfirmation.MaxCustomerIdLength).IsRequired();
        builder.Property(m => m.Query).HasMaxLength(512).IsRequired();
        builder.Property(m => m.NormalizedQuery).HasMaxLength(512).IsRequired();

        builder.HasOne<Product>().WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.CustomerId, m.NormalizedQuery });
        builder.HasIndex(m => new { m.CustomerId, m.ProductId });
        builder.HasIndex(m => new { m.CustomerId, m.ConfirmedAt });
    }
}
