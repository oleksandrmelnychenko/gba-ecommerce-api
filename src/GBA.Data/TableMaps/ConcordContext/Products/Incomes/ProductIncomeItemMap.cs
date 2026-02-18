using GBA.Domain.Entities.Products.Incomes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products.Incomes;

public sealed class ProductIncomeItemMap : EntityBaseMap<ProductIncomeItem> {
    public override void Map(EntityTypeBuilder<ProductIncomeItem> entity) {
        base.Map(entity);

        entity.ToTable("ProductIncomeItem");

        entity.Property(e => e.ProductIncomeId).HasColumnName("ProductIncomeID");

        entity.Property(e => e.SaleReturnItemId).HasColumnName("SaleReturnItemID");

        entity.Property(e => e.SupplyOrderUkraineItemId).HasColumnName("SupplyOrderUkraineItemID");

        entity.Property(e => e.PackingListPackageOrderItemId).HasColumnName("PackingListPackageOrderItemID");

        entity.Property(e => e.ActReconciliationItemId).HasColumnName("ActReconciliationItemID");

        entity.Property(e => e.ProductCapitalizationItemId).HasColumnName("ProductCapitalizationItemID");

        entity.Ignore(e => e.ProductAvailability);

        entity.Ignore(e => e.OrderProductSpecification);

        entity.HasOne(e => e.ProductIncome)
            .WithMany(e => e.ProductIncomeItems)
            .HasForeignKey(e => e.ProductIncomeId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.SupplyOrderUkraineItem)
            .WithMany(e => e.ProductIncomeItems)
            .HasForeignKey(e => e.SupplyOrderUkraineItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.SaleReturnItem)
            .WithOne(e => e.ProductIncomeItem)
            .HasForeignKey<ProductIncomeItem>(e => e.SaleReturnItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.PackingListPackageOrderItem)
            .WithMany(e => e.ProductIncomeItems)
            .HasForeignKey(e => e.PackingListPackageOrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.ActReconciliationItem)
            .WithMany(e => e.ProductIncomeItems)
            .HasForeignKey(e => e.ActReconciliationItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.ProductCapitalizationItem)
            .WithOne(e => e.ProductIncomeItem)
            .HasForeignKey<ProductIncomeItem>(e => e.ProductCapitalizationItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}