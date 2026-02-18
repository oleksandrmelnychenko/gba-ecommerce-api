using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class OutcomePaymentOrderSupplyPaymentTaskMap : EntityBaseMap<OutcomePaymentOrderSupplyPaymentTask> {
    public override void Map(EntityTypeBuilder<OutcomePaymentOrderSupplyPaymentTask> entity) {
        base.Map(entity);

        entity.ToTable("OutcomePaymentOrderSupplyPaymentTask");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.OutcomePaymentOrderId).HasColumnName("OutcomePaymentOrderID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.HasOne(e => e.OutcomePaymentOrder)
            .WithMany(e => e.OutcomePaymentOrderSupplyPaymentTasks)
            .HasForeignKey(e => e.OutcomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.OutcomePaymentOrderSupplyPaymentTasks)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}