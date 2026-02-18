using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class CustomServiceMap : EntityBaseMap<CustomService> {
    public override void Map(EntityTypeBuilder<CustomService> entity) {
        base.Map(entity);

        entity.ToTable("CustomService");

        entity.Property(e => e.ServiceNumber).HasMaxLength(50);

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.Property(e => e.CustomOrganizationId).HasColumnName("CustomOrganizationID");

        entity.Property(e => e.ExciseDutyOrganizationId).HasColumnName("ExciseDutyOrganizationID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.GrossPrice).HasColumnType("money");

        entity.Property(e => e.Vat).HasColumnType("money");

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
            .WithOne(e => e.CustomService)
            .HasForeignKey<CustomService>(e => e.SupplyServiceAccountDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ActProvidingServiceDocument)
            .WithOne(e => e.CustomService)
            .HasForeignKey<CustomService>(e => e.ActProvidingServiceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.BrokerServices)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.BrokerServices)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.CustomServices)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AccountingPaymentTask)
            .WithMany(e => e.AccountingBrokerServices)
            .HasForeignKey(e => e.AccountingPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomOrganization)
            .WithMany(e => e.CustomServices)
            .HasForeignKey(e => e.CustomOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ExciseDutyOrganization)
            .WithMany(e => e.ExciseDutyCustomServices)
            .HasForeignKey(e => e.ExciseDutyOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.CustomServices)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.SupplyInformationTask)
            .WithOne(x => x.CustomService)
            .HasForeignKey<CustomService>(x => x.SupplyInformationTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}