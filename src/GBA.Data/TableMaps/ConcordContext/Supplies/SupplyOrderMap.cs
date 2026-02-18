using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyOrderMap : EntityBaseMap<SupplyOrder> {
    public override void Map(EntityTypeBuilder<SupplyOrder> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrder");

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.GrossPrice).HasColumnType("money");

        entity.Property(e => e.AdditionalAmount).HasColumnType("money");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.SupplyOrderNumberId).HasColumnName("SupplyOrderNumberID");

        entity.Property(e => e.SupplyProFormId).HasColumnName("SupplyProFormID");

        entity.Property(e => e.TransportationServiceId).HasColumnName("TransportationServiceID");

        entity.Property(e => e.PortWorkServiceId).HasColumnName("PortWorkServiceID");

        entity.Property(e => e.CustomAgencyServiceId).HasColumnName("CustomAgencyServiceID");

        entity.Property(e => e.PortCustomAgencyServiceId).HasColumnName("PortCustomAgencyServiceID");

        entity.Property(e => e.PlaneDeliveryServiceId).HasColumnName("PlaneDeliveryServiceID");

        entity.Property(e => e.VehicleDeliveryServiceId).HasColumnName("VehicleDeliveryServiceID");

        entity.Property(e => e.AdditionalPaymentCurrencyId).HasColumnName("AdditionalPaymentCurrencyID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleId");

        entity.Property(e => e.IsApproved).HasDefaultValueSql("0");

        entity.Ignore(e => e.SupplyOrderTotals);

        entity.Ignore(e => e.InvoiceNumbers);

        entity.Ignore(e => e.PackListNumbers);

        entity.Ignore(e => e.TotalNetPrice);

        entity.Ignore(e => e.TotalQuantity);

        entity.Ignore(e => e.TotalVat);

        entity.Ignore(e => e.TotalRowsQty);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyProForm)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.SupplyProFormId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TransportationService)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.TransportationServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PortWorkService)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.PortWorkServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomAgencyService)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.CustomAgencyServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PortCustomAgencyService)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.PortCustomAgencyServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PlaneDeliveryService)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.PlaneDeliveryServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.VehicleDeliveryService)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.VehicleDeliveryServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AdditionalPaymentCurrency)
            .WithMany(e => e.SupplyOrders)
            .HasForeignKey(e => e.AdditionalPaymentCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}