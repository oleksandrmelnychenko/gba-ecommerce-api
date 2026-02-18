using GBA.Domain.Entities.Ecommerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Ecommerce;

public sealed class EcommerceDefaultPricingMap : EntityBaseMap<EcommerceDefaultPricing> {
    public override void Map(EntityTypeBuilder<EcommerceDefaultPricing> entity) {
        base.Map(entity);

        entity.ToTable("EcommerceDefaultPricing");

        entity.Property(e => e.PricingId)
            .HasColumnName("PricingID")
            .IsRequired();

        entity.Property(e => e.PromotionalPricingId)
            .HasColumnName("PromotionalPricingID")
            .IsRequired();

        entity.HasOne(e => e.Pricing)
            .WithMany(e => e.DefaultPricings)
            .HasForeignKey(e => e.PricingId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PromotionalPricing)
            .WithMany(e => e.PromotionalPricings)
            .HasForeignKey(e => e.PromotionalPricingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}