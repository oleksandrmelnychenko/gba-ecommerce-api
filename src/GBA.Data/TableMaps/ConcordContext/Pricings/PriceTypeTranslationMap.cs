using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Pricings;

public sealed class PriceTypeTranslationMap : EntityBaseMap<PriceTypeTranslation> {
    public override void Map(EntityTypeBuilder<PriceTypeTranslation> entity) {
        base.Map(entity);

        entity.ToTable("PriceTypeTranslation");

        entity.Property(e => e.PriceTypeId).HasColumnName("PriceTypeID");

        entity.Property(e => e.Name).HasMaxLength(50);

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.HasOne(e => e.PriceType)
            .WithMany(e => e.PriceTypeTranslations)
            .HasForeignKey(e => e.PriceTypeId);
    }
}