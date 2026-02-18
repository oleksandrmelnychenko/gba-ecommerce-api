using GBA.Domain.Entities.Supplies.Protocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrderPaymentDeliveryProtocolKeyMap : EntityBaseMap<SupplyOrderPaymentDeliveryProtocolKey> {
    public override void Map(EntityTypeBuilder<SupplyOrderPaymentDeliveryProtocolKey> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderPaymentDeliveryProtocolKey");
    }
}