using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrderNumberMap : EntityBaseMap<SupplyOrderNumber> {
    public override void Map(EntityTypeBuilder<SupplyOrderNumber> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderNumber");
    }
}