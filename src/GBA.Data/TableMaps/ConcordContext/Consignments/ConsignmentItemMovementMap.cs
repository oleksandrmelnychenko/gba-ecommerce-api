using GBA.Domain.Entities.Consignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consignments;

public sealed class ConsignmentItemMovementMap : EntityBaseMap<ConsignmentItemMovement> {
    public override void Map(EntityTypeBuilder<ConsignmentItemMovement> entity) {
        base.Map(entity);

        entity.ToTable("ConsignmentItemMovement");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.Property(e => e.ProductIncomeItemId).HasColumnName("ProductIncomeItemID");

        entity.Property(e => e.DepreciatedOrderItemId).HasColumnName("DepreciatedOrderItemID");

        entity.Property(e => e.SupplyReturnItemId).HasColumnName("SupplyReturnItemID");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.ProductTransferItemId).HasColumnName("ProductTransferItemID");

        entity.Property(e => e.OrderItemBaseShiftStatusId).HasColumnName("OrderItemBaseShiftStatusID");

        entity.Property(e => e.TaxFreeItemId).HasColumnName("TaxFreeItemID");

        entity.Property(e => e.SadItemId).HasColumnName("SadItemID");

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.ConsignmentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.ProductIncomeItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.ProductIncomeItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DepreciatedOrderItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.DepreciatedOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyReturnItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.SupplyReturnItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ReSaleItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.ReSaleItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductTransferItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.ProductTransferItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrderItemBaseShiftStatus)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.OrderItemBaseShiftStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFreeItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.TaxFreeItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SadItem)
            .WithMany(e => e.ConsignmentItemMovements)
            .HasForeignKey(e => e.SadItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}