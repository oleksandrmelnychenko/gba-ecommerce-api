using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SupplyOrderUkraineMap : EntityBaseMap<SupplyOrderUkraine> {
    public override void Map(EntityTypeBuilder<SupplyOrderUkraine> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderUkraine");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.InvNumber).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.AdditionalPaymentCurrencyId).HasColumnName("AdditionalPaymentCurrencyID");

        entity.Property(e => e.ShipmentAmount).HasColumnType("decimal(30,14)");

        entity.Property(e => e.ShipmentAmountLocal).HasColumnType("decimal(30,14)");

        entity.Property(e => e.AdditionalAmount).HasColumnType("money");

        entity.Property(e => e.VatPercent).HasColumnType("money");

        entity.Ignore(e => e.TotalNetPrice);

        entity.Ignore(e => e.TotalGrossPrice);

        entity.Ignore(e => e.TotalNetPriceLocal);

        entity.Ignore(e => e.TotalGrossPriceLocal);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalQty);

        entity.Ignore(e => e.ExchangeRateAmount);

        entity.Ignore(e => e.TotalNetPriceLocalWithVat);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.Ignore(e => e.TotalVatAmount);

        entity.Ignore(e => e.TotalAccountingGrossPrice);

        entity.Ignore(e => e.TotalAccountingGrossPriceLocal);

        entity.Ignore(e => e.TotalProtocolsValue);

        entity.Ignore(e => e.TotalProtocolsDiscount);

        entity.Ignore(e => e.TotalRowsQty);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleSupplyOrderUkraines)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.SupplyOrderUkraines)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Supplier)
            .WithMany(e => e.SupplyOrderUkraines)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.SupplyOrderUkraines)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AdditionalPaymentCurrency)
            .WithMany(e => e.SupplyOrderUkraines)
            .HasForeignKey(e => e.AdditionalPaymentCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}