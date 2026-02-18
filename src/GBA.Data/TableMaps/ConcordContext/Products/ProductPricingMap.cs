using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductPricingMap : EntityBaseMap<ProductPricing> {
    public override void Map(EntityTypeBuilder<ProductPricing> entity) {
        base.Map(entity);

        entity.ToTable("ProductPricing");

        entity.Property(e => e.PricingId).HasColumnName("PricingID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.Price).HasColumnType("money");

        entity.HasOne(e => e.Pricing)
            .WithMany(e => e.ProductPricings)
            .HasForeignKey(e => e.PricingId);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductPricings)
            .HasForeignKey(e => e.ProductId);

        entity.HasIndex(e => new { e.Deleted, e.ProductId });

        entity.HasIndex(e => new { e.Deleted, e.PricingId });
    }
}