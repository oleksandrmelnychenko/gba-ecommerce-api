using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Filters;

public sealed class FilterItemTranslationMap : EntityBaseMap<FilterItemTranslation> {
    public override void Map(EntityTypeBuilder<FilterItemTranslation> entity) {
        base.Map(entity);

        entity.ToTable("FilterItemTranslation");

        entity.Property(e => e.FilterItemId).HasColumnName("FilterItemID");

        entity.HasOne(e => e.FilterItem)
            .WithMany(e => e.FilterItemTranslations)
            .HasForeignKey(e => e.FilterItemId);
    }
}