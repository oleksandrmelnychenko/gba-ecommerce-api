using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyInvoiceMap : EntityBaseMap<SupplyInvoice> {
    public override void Map(EntityTypeBuilder<SupplyInvoice> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInvoice");

        entity.Property(e => e.ServiceNumber).HasMaxLength(50);

        entity.Property(e => e.Number).HasMaxLength(100);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.ExtraCharge).HasColumnType("money");

        entity.Property(e => e.IsShipped).HasDefaultValueSql("0");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.SupplyOrganizationId).HasColumnName("SupplyOrganizationID");

        entity.Property(e => e.RootSupplyInvoiceId).HasColumnName("RootSupplyInvoiceID");

        entity.Property(e => e.DeliveryProductProtocolId).HasColumnName("DeliveryProductProtocolID");

        entity.Property(e => e.NumberCustomDeclaration).HasMaxLength(100);

        entity.Property(e => e.DeliveryAmount).HasColumnType("money");

        entity.Property(e => e.DiscountAmount).HasColumnType("money");

        entity.Ignore(e => e.TotalNetPrice);

        entity.Ignore(e => e.TotalNetPriceWithVat);

        entity.Ignore(e => e.TotalGrossPrice);

        entity.Ignore(e => e.AccountingTotalGrossPrice);

        entity.Ignore(e => e.TotalSpending);

        entity.Ignore(e => e.AccountingTotalSpending);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalCBM);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.Ignore(e => e.TotalQuantity);

        entity.Ignore(e => e.ExchangeRate);

        entity.Ignore(e => e.TotalVatAmount);

        entity.Ignore(e => e.TotalValueWithVat);

        entity.Ignore(e => e.ExchangeRateEurToUah);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.SupplyInvoices)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DeliveryProductProtocol)
            .WithMany(e => e.SupplyInvoices)
            .HasForeignKey(e => e.DeliveryProductProtocolId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany()
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganization)
            .WithMany()
            .HasForeignKey(e => e.SupplyOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RootSupplyInvoice)
            .WithMany(e => e.MergedSupplyInvoices)
            .HasForeignKey(e => e.RootSupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}