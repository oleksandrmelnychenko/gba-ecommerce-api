using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Pricings;

public sealed class PricingTranslationMap : EntityBaseMap<PricingTranslation> {
    public override void Map(EntityTypeBuilder<PricingTranslation> entity) {
        base.Map(entity);

        entity.ToTable("PricingTranslation");

        entity.Property(e => e.Name).HasMaxLength(30);

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.Property(e => e.PricingId).HasColumnName("PricingID");

        entity.HasOne(e => e.Pricing)
            .WithMany(e => e.PricingTranslations)
            .HasForeignKey(e => e.PricingId);

        entity.HasIndex(e => new { e.PricingId, e.CultureCode, e.Deleted });
    }
}