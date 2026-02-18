using GBA.Domain.Entities.Sales.OrderItemShiftStatuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class OrderItemBaseShiftStatusMap : EntityBaseMap<OrderItemBaseShiftStatus> {
    public override void Map(EntityTypeBuilder<OrderItemBaseShiftStatus> entity) {
        base.Map(entity);

        entity.ToTable("OrderItemBaseShiftStatus");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Property(e => e.HistoryInvoiceEditId).HasColumnName("HistoryInvoiceEditID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Ignore(e => e.CurrentId);

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.ShiftStatuses)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.OrderItemBaseShiftStatuses)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.OrderItemBaseShiftStatuses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}