using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class SupplyServiceNumberMap : EntityBaseMap<SupplyServiceNumber> {
    public override void Map(EntityTypeBuilder<SupplyServiceNumber> entity) {
        base.Map(entity);

        entity.ToTable("SupplyServiceNumber");

        entity.Property(e => e.Number).HasMaxLength(50);
    }
}