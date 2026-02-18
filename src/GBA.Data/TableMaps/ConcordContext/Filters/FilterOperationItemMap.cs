using GBA.Domain.FilterEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Filters;

public sealed class FilterOperationItemMap : EntityBaseMap<FilterOperationItem> {
    public override void Map(EntityTypeBuilder<FilterOperationItem> entity) {
        base.Map(entity);

        entity.ToTable("FilterOperationItem");

        entity.Property(e => e.Name).HasMaxLength(25);

        entity.Property(e => e.SQL).HasMaxLength(25);
    }
}