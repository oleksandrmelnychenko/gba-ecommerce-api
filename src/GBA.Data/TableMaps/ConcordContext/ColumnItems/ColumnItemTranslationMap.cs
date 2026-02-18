using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ColumnItems;

public sealed class ColumnItemTranslationMap : EntityBaseMap<ColumnItemTranslation> {
    public override void Map(EntityTypeBuilder<ColumnItemTranslation> entity) {
        base.Map(entity);

        entity.ToTable("ColumnItemTranslation");

        entity.Property(e => e.ColumnItemId).HasColumnName("ColumnItemID");

        entity.HasOne(e => e.ColumnItem)
            .WithMany(e => e.ColumnItemTranslations)
            .HasForeignKey(e => e.ColumnItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}