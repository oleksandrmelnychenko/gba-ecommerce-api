using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class ConsumableProductTranslationMap : EntityBaseMap<ConsumableProductTranslation> {
    public override void Map(EntityTypeBuilder<ConsumableProductTranslation> entity) {
        base.Map(entity);

        entity.ToTable("ConsumableProductTranslation");

        entity.Property(e => e.ConsumableProductId).HasColumnName("ConsumableProductID");

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.HasOne(e => e.ConsumableProduct)
            .WithMany(e => e.ConsumableProductTranslations)
            .HasForeignKey(e => e.ConsumableProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}