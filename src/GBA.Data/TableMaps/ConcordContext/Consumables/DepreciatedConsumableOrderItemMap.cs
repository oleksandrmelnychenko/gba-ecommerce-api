using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class DepreciatedConsumableOrderItemMap : EntityBaseMap<DepreciatedConsumableOrderItem> {
    public override void Map(EntityTypeBuilder<DepreciatedConsumableOrderItem> entity) {
        base.Map(entity);

        entity.ToTable("DepreciatedConsumableOrderItem");

        entity.Property(e => e.ConsumablesOrderItemId).HasColumnName("ConsumablesOrderItemID");

        entity.Property(e => e.DepreciatedConsumableOrderId).HasColumnName("DepreciatedConsumableOrderID");

        entity.Ignore(e => e.TotalPrice);

        entity.Ignore(e => e.Currency);

        entity.HasOne(e => e.ConsumablesOrderItem)
            .WithMany(e => e.DepreciatedConsumableOrderItems)
            .HasForeignKey(e => e.ConsumablesOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DepreciatedConsumableOrder)
            .WithMany(e => e.DepreciatedConsumableOrderItems)
            .HasForeignKey(e => e.DepreciatedConsumableOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}