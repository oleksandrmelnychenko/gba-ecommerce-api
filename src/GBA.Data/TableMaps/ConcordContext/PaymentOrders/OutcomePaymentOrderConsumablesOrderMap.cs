using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class OutcomePaymentOrderConsumablesOrderMap : EntityBaseMap<OutcomePaymentOrderConsumablesOrder> {
    public override void Map(EntityTypeBuilder<OutcomePaymentOrderConsumablesOrder> entity) {
        base.Map(entity);

        entity.ToTable("OutcomePaymentOrderConsumablesOrder");

        entity.Property(e => e.ConsumablesOrderId).HasColumnName("ConsumablesOrderID");

        entity.Property(e => e.OutcomePaymentOrderId).HasColumnName("OutcomePaymentOrderID");

        entity.HasOne(e => e.ConsumablesOrder)
            .WithMany(e => e.OutcomePaymentOrderConsumablesOrders)
            .HasForeignKey(e => e.ConsumablesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OutcomePaymentOrder)
            .WithMany(e => e.OutcomePaymentOrderConsumablesOrders)
            .HasForeignKey(e => e.OutcomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}