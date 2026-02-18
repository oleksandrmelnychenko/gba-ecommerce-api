using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders.PaymentMovements;

public sealed class PaymentCostMovementOperationMap : EntityBaseMap<PaymentCostMovementOperation> {
    public override void Map(EntityTypeBuilder<PaymentCostMovementOperation> entity) {
        base.Map(entity);

        entity.ToTable("PaymentCostMovementOperation");

        entity.Property(e => e.ConsumablesOrderItemId).HasColumnName("ConsumablesOrderItemID");

        entity.Property(e => e.DepreciatedConsumableOrderItemId).HasColumnName("DepreciatedConsumableOrderItemID");

        entity.Property(e => e.CompanyCarFuelingId).HasColumnName("CompanyCarFuelingID");

        entity.Property(e => e.PaymentCostMovementId).HasColumnName("PaymentCostMovementID");

        entity.HasOne(e => e.PaymentCostMovement)
            .WithMany(e => e.PaymentCostMovementOperations)
            .HasForeignKey(e => e.PaymentCostMovementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumablesOrderItem)
            .WithOne(e => e.PaymentCostMovementOperation)
            .HasForeignKey<PaymentCostMovementOperation>(e => e.ConsumablesOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DepreciatedConsumableOrderItem)
            .WithOne(e => e.PaymentCostMovementOperation)
            .HasForeignKey<PaymentCostMovementOperation>(e => e.DepreciatedConsumableOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CompanyCarFueling)
            .WithOne(e => e.PaymentCostMovementOperation)
            .HasForeignKey<PaymentCostMovementOperation>(e => e.CompanyCarFuelingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}