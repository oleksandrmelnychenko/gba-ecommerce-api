using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class SupplyInvoiceMergedServiceMap : EntityBaseMap<SupplyInvoiceMergedService> {
    public override void Map(EntityTypeBuilder<SupplyInvoiceMergedService> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInvoiceMergedService");

        entity.Property(e => e.MergedServiceId).HasColumnName("MergedServiceID");

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");

        entity.Property(e => e.Value).HasColumnType("decimal(30,14)");

        entity.Property(e => e.AccountingValue).HasColumnType("decimal(30,14)");

        entity.HasOne(e => e.MergedService)
            .WithMany(e => e.SupplyInvoiceMergedServices)
            .HasForeignKey(e => e.MergedServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.SupplyInvoiceMergedServices)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Ignore(x => x.ExchangeRateEurToUah);

        entity.Ignore(x => x.ExchangeRateEurToAgreementCurrency);
    }
}