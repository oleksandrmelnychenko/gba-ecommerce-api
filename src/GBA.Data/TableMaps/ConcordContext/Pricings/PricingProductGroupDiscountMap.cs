using GBA.Domain.Entities.Pricings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Pricings;

public sealed class PricingProductGroupDiscountMap : EntityBaseMap<PricingProductGroupDiscount> {
    public override void Map(EntityTypeBuilder<PricingProductGroupDiscount> entity) {
        base.Map(entity);

        entity.ToTable("PricingProductGroupDiscount");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.CalculatedExtraCharge).HasColumnType("money");

        entity.Property(e => e.ProductGroupId).HasColumnName("ProductGroupID");

        entity.Property(e => e.PricingId).HasColumnName("PricingID");

        entity.Property(e => e.BasePricingId).HasColumnName("BasePricingID");

        entity.HasOne(e => e.ProductGroup)
            .WithMany(e => e.PricingProductGroupDiscounts)
            .HasForeignKey(e => e.ProductGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Pricing)
            .WithMany(e => e.PricingProductGroupDiscounts)
            .HasForeignKey(e => e.PricingId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.BasePricing)
            .WithMany(e => e.SubPricingProductGroupDiscounts)
            .HasForeignKey(e => e.BasePricingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}