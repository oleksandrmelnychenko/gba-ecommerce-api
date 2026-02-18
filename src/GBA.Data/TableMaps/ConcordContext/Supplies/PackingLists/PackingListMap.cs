using GBA.Domain.Entities.Supplies.PackingLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.PackingLists;

public sealed class PackingListMap : EntityBaseMap<PackingList> {
    public override void Map(EntityTypeBuilder<PackingList> entity) {
        base.Map(entity);

        entity.ToTable("PackingList");

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");

        entity.Property(e => e.ContainerServiceId).HasColumnName("ContainerServiceID");

        entity.Property(e => e.RootPackingListId).HasColumnName("RootPackingListID");

        entity.Property(e => e.FromDate).HasDefaultValueSql("getutcdate()");

        entity.Property(e => e.ExtraCharge).HasColumnType("money");

        entity.Property(e => e.AccountingExtraCharge).HasColumnType("money");

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.MarkNumber).HasMaxLength(100);

        entity.Property(e => e.InvNo).HasMaxLength(100);

        entity.Property(e => e.PlNo).HasMaxLength(100);

        entity.Property(e => e.RefNo).HasMaxLength(100);

        entity.Property(e => e.No).HasMaxLength(100);

        entity.Ignore(e => e.TotalPallets);

        entity.Ignore(e => e.TotalBoxes);

        entity.Ignore(e => e.TotalQuantity);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalCBM);

        entity.Ignore(e => e.TotalPrice);

        entity.Ignore(e => e.TotalCustomValue);

        entity.Ignore(e => e.TotalDuty);

        entity.Ignore(e => e.PackingListBoxes);

        entity.Ignore(e => e.PackingListPallets);

        entity.Ignore(e => e.TotalGrossPrice);

        entity.Ignore(e => e.AccountingTotalGrossPrice);

        entity.Ignore(e => e.TotalNetPrice);

        entity.Ignore(e => e.TotalNetPriceWithVat);

        entity.Ignore(e => e.TotalNetPriceWithVatEur);

        entity.Ignore(e => e.TotalNetPriceEur);

        entity.Ignore(e => e.TotalGrossPriceEur);

        entity.Ignore(e => e.AccountingTotalGrossPriceEur);

        entity.Ignore(e => e.TotalVatAmount);

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.PackingLists)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ContainerService)
            .WithMany(e => e.PackingLists)
            .HasForeignKey(e => e.ContainerServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RootPackingList)
            .WithMany(e => e.MergedPackingLists)
            .HasForeignKey(e => e.RootPackingListId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}