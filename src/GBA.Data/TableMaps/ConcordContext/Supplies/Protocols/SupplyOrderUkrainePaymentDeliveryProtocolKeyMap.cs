using GBA.Domain.Entities.Supplies.Protocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrderUkrainePaymentDeliveryProtocolKeyMap : EntityBaseMap<SupplyOrderUkrainePaymentDeliveryProtocolKey> {
    public override void Map(EntityTypeBuilder<SupplyOrderUkrainePaymentDeliveryProtocolKey> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderUkrainePaymentDeliveryProtocolKey");

        entity.Property(e => e.Key).HasMaxLength(150);
    }
}