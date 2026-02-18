using GBA.Domain.Entities.Supplies.Protocols;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyInformationDeliveryProtocolKeyMap : EntityBaseMap<SupplyInformationDeliveryProtocolKey> {
    public override void Map(EntityTypeBuilder<SupplyInformationDeliveryProtocolKey> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInformationDeliveryProtocolKey");
    }
}