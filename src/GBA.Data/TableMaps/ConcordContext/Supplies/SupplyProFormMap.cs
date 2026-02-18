using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyProFormMap : EntityBaseMap<SupplyProForm> {
    public override void Map(EntityTypeBuilder<SupplyProForm> entity) {
        base.Map(entity);

        entity.ToTable("SupplyProForm");

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.ServiceNumber).HasMaxLength(50);
    }
}