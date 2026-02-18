using GBA.Domain.Entities.Pricings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Pricings;

public sealed class ProviderPricingMap : EntityBaseMap<ProviderPricing> {
    public override void Map(EntityTypeBuilder<ProviderPricing> entity) {
        base.Map(entity);

        entity.ToTable("ProviderPricing");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.Property(e => e.BasePricingId).HasColumnName("BasePricingID");

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.ProviderPricings)
            .HasForeignKey(e => e.CurrencyId);

        entity.HasOne(e => e.Pricing)
            .WithMany(e => e.ProviderPricings)
            .HasForeignKey(e => e.BasePricingId);
    }
}