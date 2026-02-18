using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.DeliveryProductProtocols;

public sealed class DeliveryProductProtocolNumberMap : EntityBaseMap<DeliveryProductProtocolNumber> {
    public override void Map(EntityTypeBuilder<DeliveryProductProtocolNumber> entity) {
        base.Map(entity);

        entity.ToTable("DeliveryProductProtocolNumber");

        entity.Property(e => e.Number).HasMaxLength(20);
    }
}