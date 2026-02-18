using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Filters;

public sealed class FilterOperationItemTranslationMap : EntityBaseMap<FilterOperationItemTranslation> {
    public override void Map(EntityTypeBuilder<FilterOperationItemTranslation> entity) {
        base.Map(entity);

        entity.ToTable("FilterOperationItemTranslation");

        entity.Property(e => e.FilterOperationItemId).HasColumnName("FilterOperationItemID");

        entity.HasOne(e => e.FilterOperationItem)
            .WithMany(e => e.FilterOperatorItemTranslations)
            .HasForeignKey(e => e.FilterOperationItemId);
    }
}