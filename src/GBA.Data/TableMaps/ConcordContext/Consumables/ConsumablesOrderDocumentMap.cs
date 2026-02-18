using GBA.Domain.Entities.Consumables.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class ConsumablesOrderDocumentMap : EntityBaseMap<ConsumablesOrderDocument> {
    public override void Map(EntityTypeBuilder<ConsumablesOrderDocument> entity) {
        base.Map(entity);

        entity.ToTable("ConsumablesOrderDocument");

        entity.Property(e => e.ConsumablesOrderId).HasColumnName("ConsumablesOrderID");
    }
}