using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SupplyOrderUkraineItemMap : EntityBaseMap<SupplyOrderUkraineItem> {
    public override void Map(EntityTypeBuilder<SupplyOrderUkraineItem> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderUkraineItem");

        entity.Property(e => e.UnitPrice).HasColumnType("decimal(30,14)");

        entity.Property(e => e.GrossUnitPrice).HasColumnType("decimal(30,14)");

        entity.Property(e => e.UnitPriceLocal).HasColumnType("decimal(30,14)");

        entity.Property(e => e.GrossUnitPriceLocal).HasColumnType("decimal(30,14)");

        entity.Property(e => e.AccountingGrossUnitPrice).HasColumnType("decimal(30,14)");

        entity.Property(e => e.UnitDeliveryAmount).HasColumnType("decimal(30,14)");

        entity.Property(e => e.UnitDeliveryAmountLocal).HasColumnType("decimal(30,14)");

        entity.Property(e => e.AccountingGrossUnitPriceLocal).HasColumnType("decimal(30,14)");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ProductSpecificationId).HasColumnName("ProductSpecificationID");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.Property(e => e.PackingListPackageOrderItemId).HasColumnName("PackingListPackageOrderItemID");

        entity.Property(e => e.VatPercent).HasColumnType("money");

        entity.Property(e => e.VatAmount).HasColumnType("decimal(30,14)");

        entity.Property(e => e.VatAmountLocal).HasColumnType("decimal(30,14)");

        entity.Ignore(e => e.QtyDifferent);

        entity.Ignore(e => e.ToIncomeQty);

        entity.Ignore(e => e.NetPrice);

        entity.Ignore(e => e.GrossPrice);

        entity.Ignore(e => e.NetPriceLocal);

        entity.Ignore(e => e.GrossPriceLocal);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.IsUpdated);

        entity.Ignore(e => e.ProductIncomeItem);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.Ignore(e => e.AccountingGrossPrice);

        entity.Ignore(e => e.AccountingGrossPriceLocal);

        entity.Ignore(e => e.DeliveryAmount);

        entity.Ignore(e => e.DeliveryAmountLocal);

        entity.Ignore(e => e.DeliveryExpenseAmount);

        entity.Ignore(e => e.AccountingDeliveryExpenseAmount);

        entity.Ignore(e => e.ManagementCost);

        entity.Ignore(e => e.AccountingCost);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.SupplyOrderUkraineItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraine)
            .WithMany(e => e.SupplyOrderUkraineItems)
            .HasForeignKey(e => e.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Supplier)
            .WithMany(e => e.SupplyOrderUkraineItems)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.SupplyOrderUkraineItems)
            .HasForeignKey(e => e.ConsignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PackingListPackageOrderItem)
            .WithMany(e => e.SupplyOrderUkraineItems)
            .HasForeignKey(e => e.PackingListPackageOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.ProductSpecification)
            .WithMany(x => x.SupplyOrderUkraineItems)
            .HasForeignKey(x => x.ProductSpecificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}