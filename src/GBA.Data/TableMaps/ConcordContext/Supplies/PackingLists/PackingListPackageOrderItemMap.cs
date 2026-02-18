using GBA.Domain.Entities.Supplies.PackingLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.PackingLists;

public class PackingListPackageOrderItemMap : EntityBaseMap<PackingListPackageOrderItem> {
    public override void Map(EntityTypeBuilder<PackingListPackageOrderItem> entity) {
        base.Map(entity);

        entity.ToTable("PackingListPackageOrderItem");

        entity.Property(e => e.PackingListId).HasColumnName("PackingListID");

        entity.Property(e => e.PackingListPackageId).HasColumnName("PackingListPackageID");

        entity.Property(e => e.SupplyInvoiceOrderItemId).HasColumnName("SupplyInvoiceOrderItemID");

        entity.Property(e => e.UnitPrice).HasColumnType("money");

        entity.Property(e => e.UnitPriceEur).HasColumnType("money");

        entity.Property(e => e.UnitPriceEurWithVat).HasColumnType("decimal(30,14)");

        entity.Property(e => e.GrossUnitPriceEur).HasColumnType("decimal(30,14)");

        entity.Property(e => e.DeliveryPerItem).HasColumnType("decimal(30,14)");

        entity.Property(e => e.AccountingGrossUnitPriceEur).HasColumnType("decimal(30,14)");

        entity.Property(e => e.AccountingGeneralGrossUnitPriceEur).HasColumnType("decimal(30,14)");

        entity.Property(e => e.ContainerUnitPriceEur).HasColumnType("money");

        entity.Property(e => e.AccountingContainerUnitPriceEur).HasColumnType("money");

        entity.Property(e => e.ExchangeRateAmount).HasColumnType("money");

        entity.Property(e => e.ExchangeRateAmountUahToEur).HasColumnType("money");

        entity.Property(e => e.VatPercent).HasColumnType("money");

        entity.Property(e => e.VatAmount).HasColumnType("money");

        entity.Property(e => e.Placement).HasMaxLength(25);

        entity.Ignore(e => e.TotalNetPrice);

        entity.Ignore(e => e.AccountingTotalNetPrice);

        entity.Ignore(e => e.TotalGrossPrice);

        entity.Ignore(e => e.AccountingTotalGrossPrice);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.ToOperationQty);

        entity.Ignore(e => e.TotalNetWithVat);

        entity.Ignore(e => e.QtyDifferent);

        entity.Ignore(e => e.IsUpdated);

        entity.Ignore(e => e.Reason);

        entity.Ignore(e => e.Supplier);

        entity.Ignore(e => e.TotalPriceWithVatOne);

        entity.Ignore(e => e.TotalPriceWithVatTwo);

        entity.Ignore(e => e.TotalNetPriceEur);

        entity.Ignore(e => e.TotalGrossPriceEur);

        entity.Ignore(e => e.AccountingTotalGrossPriceEur);

        entity.Ignore(e => e.VatAmountEur);

        entity.Ignore(e => e.TotalNetPriceWithVat);

        entity.Ignore(e => e.TotalNetPriceWithVatEur);

        entity.Ignore(e => e.ProductIncomeItem);

        entity.Ignore(e => e.ProductSpecification);

        entity.Ignore(e => e.UnitPriceUah);

        entity.Ignore(e => e.AccountingGeneralTotalGrossPriceEur);

        entity.Ignore(e => e.AccountingGeneralTotalGrossPrice);

        entity.Ignore(e => e.DeliveryAmountUah);

        entity.Ignore(e => e.DeliveryAmountEur);

        entity.HasOne(e => e.PackingList)
            .WithMany(e => e.PackingListPackageOrderItems)
            .HasForeignKey(e => e.PackingListId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PackingListPackage)
            .WithMany(e => e.PackingListPackageOrderItems)
            .HasForeignKey(e => e.PackingListPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyInvoiceOrderItem)
            .WithMany(e => e.PackingListPackageOrderItems)
            .HasForeignKey(e => e.SupplyInvoiceOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}