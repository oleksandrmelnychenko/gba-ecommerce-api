using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class VehicleServiceMap : EntityBaseMap<VehicleService> {
    public override void Map(EntityTypeBuilder<VehicleService> entity) {
        base.Map(entity);

        entity.ToTable("VehicleService");

        entity.Property(e => e.ServiceNumber).HasMaxLength(50);

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.GrossPrice).HasColumnType("money");

        entity.Property(e => e.Vat).HasColumnType("money");

        entity.Property(e => e.BillOfLadingDocumentId).HasColumnName("BillOfLadingDocumentID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.Property(e => e.VehicleOrganizationId).HasColumnName("VehicleOrganizationID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.AccountingNetPrice).HasColumnType("money");

        entity.Property(e => e.AccountingGrossPrice).HasColumnType("money");

        entity.Property(e => e.AccountingVat).HasColumnType("money");

        entity.Property(e => e.AccountingPaymentTaskId).HasColumnName("AccountingPaymentTaskID");

        entity.Property(e => e.AccountingSupplyCostsWithinCountry).HasColumnType("money");

        entity.Property(e => e.SupplyInformationTaskId).HasColumnName("SupplyInformationTaskID");

        entity.Property(x => x.ExchangeRate).HasColumnType("money");

        entity.Property(x => x.AccountingExchangeRate).HasColumnType("money");

        entity.Property(e => e.ActProvidingServiceDocumentId).HasColumnName("ActProvidingServiceDocumentID");

        entity.Property(e => e.SupplyServiceAccountDocumentId).HasColumnName("SupplyServiceAccountDocumentID");

        entity.HasOne(e => e.SupplyServiceAccountDocument)
            .WithOne(e => e.VehicleService)
            .HasForeignKey<VehicleService>(e => e.SupplyServiceAccountDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ActProvidingServiceDocument)
            .WithOne(e => e.VehicleService)
            .HasForeignKey<VehicleService>(e => e.ActProvidingServiceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.VehicleServices)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.BillOfLadingDocument)
            .WithMany(e => e.VehicleServices)
            .HasForeignKey(e => e.BillOfLadingDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.VehicleServices)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AccountingPaymentTask)
            .WithMany(e => e.AccountingVehicleServices)
            .HasForeignKey(e => e.AccountingPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.VehicleOrganization)
            .WithMany(e => e.VehicleServices)
            .HasForeignKey(e => e.VehicleOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.VehicleServices)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.SupplyInformationTask)
            .WithOne(x => x.VehicleService)
            .HasForeignKey<VehicleService>(x => x.SupplyInformationTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}