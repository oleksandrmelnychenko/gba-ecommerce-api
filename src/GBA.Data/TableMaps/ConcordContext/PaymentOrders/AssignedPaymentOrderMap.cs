using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class AssignedPaymentOrderMap : EntityBaseMap<AssignedPaymentOrder> {
    public override void Map(EntityTypeBuilder<AssignedPaymentOrder> entity) {
        base.Map(entity);

        entity.ToTable("AssignedPaymentOrder");

        entity.Property(e => e.RootOutcomePaymentOrderId).HasColumnName("RootOutcomePaymentOrderID");

        entity.Property(e => e.RootIncomePaymentOrderId).HasColumnName("RootIncomePaymentOrderID");

        entity.Property(e => e.AssignedOutcomePaymentOrderId).HasColumnName("AssignedOutcomePaymentOrderID");

        entity.Property(e => e.AssignedIncomePaymentOrderId).HasColumnName("AssignedIncomePaymentOrderID");

        entity.HasOne(e => e.AssignedOutcomePaymentOrder)
            .WithOne(e => e.RootAssignedPaymentOrder)
            .HasForeignKey<AssignedPaymentOrder>(e => e.AssignedOutcomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AssignedIncomePaymentOrder)
            .WithOne(e => e.RootAssignedPaymentOrder)
            .HasForeignKey<AssignedPaymentOrder>(e => e.AssignedIncomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RootOutcomePaymentOrder)
            .WithMany(e => e.AssignedPaymentOrders)
            .HasForeignKey(e => e.RootOutcomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RootIncomePaymentOrder)
            .WithMany(e => e.AssignedPaymentOrders)
            .HasForeignKey(e => e.RootIncomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}