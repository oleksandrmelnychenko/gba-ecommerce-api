using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class ConsumableProductCategoryTranslationMap : EntityBaseMap<ConsumableProductCategoryTranslation> {
    public override void Map(EntityTypeBuilder<ConsumableProductCategoryTranslation> entity) {
        base.Map(entity);

        entity.ToTable("ConsumableProductCategoryTranslation");

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.Description).HasMaxLength(450);

        entity.Property(e => e.ConsumableProductCategoryId).HasColumnName("ConsumableProductCategoryID");

        entity.HasOne(e => e.ConsumableProductCategory)
            .WithMany(e => e.ConsumableProductCategoryTranslations)
            .HasForeignKey(e => e.ConsumableProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}