using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class SupplyInvoiceBillOfLadingServiceMap : EntityBaseMap<SupplyInvoiceBillOfLadingService> {
    public override void Map(EntityTypeBuilder<SupplyInvoiceBillOfLadingService> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInvoiceBillOfLadingService");

        entity.Property(e => e.BillOfLadingServiceId).HasColumnName("BillOfLadingServiceID");

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");

        entity.Property(e => e.Value).HasColumnType("decimal(30,14)");

        entity.Property(e => e.AccountingValue).HasColumnType("decimal(30,14)");

        entity.HasOne(e => e.BillOfLadingService)
            .WithMany(e => e.SupplyInvoiceBillOfLadingServices)
            .HasForeignKey(e => e.BillOfLadingServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.SupplyInvoiceBillOfLadingServices)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}