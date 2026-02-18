using GBA.Domain.Entities.Pricings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Pricings;

public sealed class PricingMap : EntityBaseMap<Pricing> {
    public override void Map(EntityTypeBuilder<Pricing> entity) {
        base.Map(entity);

        entity.ToTable("Pricing");

        entity.Property(e => e.Name).HasMaxLength(30);

        entity.Property(e => e.BasePricingId).HasColumnName("BasePricingID");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.Property(e => e.PriceTypeId).HasColumnName("PriceTypeID");

        entity.Property(e => e.CalculatedExtraCharge).HasColumnType("money");

        entity.Property(e => e.ForShares).HasDefaultValueSql("0");

        entity.Property(e => e.ForVat).HasDefaultValueSql("0");

        entity.HasOne(e => e.BasePricing)
            .WithMany(e => e.Pricings)
            .HasForeignKey(e => e.BasePricingId);

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.Pricings)
            .HasForeignKey(e => e.CurrencyId);

        entity.HasOne(e => e.PriceType)
            .WithMany(e => e.Pricings)
            .HasForeignKey(e => e.PriceTypeId);
    }
}