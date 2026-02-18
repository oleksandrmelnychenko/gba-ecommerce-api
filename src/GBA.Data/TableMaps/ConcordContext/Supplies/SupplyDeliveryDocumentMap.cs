using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyDeliveryDocumentMap : EntityBaseMap<SupplyDeliveryDocument> {
    public override void Map(EntityTypeBuilder<SupplyDeliveryDocument> entity) {
        base.Map(entity);

        entity.ToTable("SupplyDeliveryDocument");
    }
}