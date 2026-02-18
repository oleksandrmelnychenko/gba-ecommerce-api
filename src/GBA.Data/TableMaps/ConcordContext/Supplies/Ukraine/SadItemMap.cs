using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SadItemMap : EntityBaseMap<SadItem> {
    public override void Map(EntityTypeBuilder<SadItem> entity) {
        base.Map(entity);

        entity.ToTable("SadItem");

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.UnitPrice).HasColumnType("money");

        entity.Property(e => e.SupplyOrderUkraineCartItemId).HasColumnName("SupplyOrderUkraineCartItemID");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

        entity.Property(e => e.SadId).HasColumnName("SadID");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.TotalAmountWithMargin);

        entity.Ignore(e => e.GrossWeight);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.Ignore(e => e.TotalVatAmount);

        entity.Ignore(e => e.TotalVatAmountWithMargin);

        entity.Ignore(e => e.ProductSpecification);

        entity.Ignore(e => e.UkProductSpecification);

        entity.HasOne(e => e.SupplyOrderUkraineCartItem)
            .WithMany(e => e.SadItems)
            .HasForeignKey(e => e.SupplyOrderUkraineCartItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.SadItems)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Supplier)
            .WithMany(e => e.SadItems)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sad)
            .WithMany(e => e.SadItems)
            .HasForeignKey(e => e.SadId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.SadItems)
            .HasForeignKey(e => e.ConsignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}