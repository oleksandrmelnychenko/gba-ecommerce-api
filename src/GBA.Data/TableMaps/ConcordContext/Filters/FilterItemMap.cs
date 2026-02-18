using GBA.Domain.FilterEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Filters;

public sealed class FilterItemMap : EntityBaseMap<FilterItem> {
    public override void Map(EntityTypeBuilder<FilterItem> entity) {
        base.Map(entity);

        entity.ToTable("FilterItem");

        entity.Ignore(e => e.FilterOperationItem);
    }
}