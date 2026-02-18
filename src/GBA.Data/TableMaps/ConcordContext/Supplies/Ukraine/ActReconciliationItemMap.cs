using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class ActReconciliationItemMap : EntityBaseMap<ActReconciliationItem> {
    public override void Map(EntityTypeBuilder<ActReconciliationItem> entity) {
        base.Map(entity);

        entity.ToTable("ActReconciliationItem");

        entity.Property(e => e.ActReconciliationId).HasColumnName("ActReconciliationID");

        entity.Property(e => e.SupplyOrderUkraineItemId).HasColumnName("SupplyOrderUkraineItemID");

        entity.Property(e => e.SupplyInvoiceOrderItemId).HasColumnName("SupplyInvoiceOrderItemID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.CommentPL).HasMaxLength(500);

        entity.Property(e => e.CommentUA).HasMaxLength(500);

        entity.Property(e => e.UnitPrice).HasColumnType("money");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.ToOperationQty);

        entity.Ignore(e => e.Availabilities);

        entity.Ignore(e => e.Comment);

        entity.Ignore(e => e.Reason);

        entity.HasOne(e => e.ActReconciliation)
            .WithMany(e => e.ActReconciliationItems)
            .HasForeignKey(e => e.ActReconciliationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ActReconciliationItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraineItem)
            .WithMany(e => e.ActReconciliationItems)
            .HasForeignKey(e => e.SupplyOrderUkraineItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyInvoiceOrderItem)
            .WithMany(e => e.ActReconciliationItems)
            .HasForeignKey(e => e.SupplyInvoiceOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}