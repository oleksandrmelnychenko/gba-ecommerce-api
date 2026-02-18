using GBA.Domain.FilterEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ColumnItems;

public sealed class ColumnItemMap : EntityBaseMap<ColumnItem> {
    public override void Map(EntityTypeBuilder<ColumnItem> entity) {
        base.Map(entity);

        entity.ToTable("ColumnItem");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.Template).HasDefaultValue(string.Empty);
    }
}