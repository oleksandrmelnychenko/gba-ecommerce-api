using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class BillOfLadingServiceMap : EntityBaseMap<BillOfLadingService> {
    public override void Map(EntityTypeBuilder<BillOfLadingService> entity) {
        base.Map(entity);

        entity.ToTable("BillOfLadingService");

        entity.Property(e => e.ServiceNumber).HasMaxLength(50);

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.GrossPrice).HasColumnType("money");

        entity.Property(e => e.Vat).HasColumnType("money");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.Property(e => e.SupplyOrganizationId).HasColumnName("SupplyOrganizationID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.AccountingNetPrice).HasColumnType("money");

        entity.Property(e => e.AccountingGrossPrice).HasColumnType("money");

        entity.Property(e => e.AccountingVat).HasColumnType("money");

        entity.Property(e => e.AccountingPaymentTaskId).HasColumnName("AccountingPaymentTaskID");

        entity.Property(e => e.TypeBillOfLadingService).HasDefaultValueSql("0");

        entity.Property(e => e.DeliveryProductProtocolId).HasColumnName("DeliveryProductProtocolID");

        entity.Property(e => e.SupplyExtraChargeType).HasDefaultValueSql("0");

        entity.Property(e => e.AccountingSupplyCostsWithinCountry).HasColumnType("money");

        entity.Property(e => e.SupplyInformationTaskId).HasColumnName("SupplyInformationTaskID");

        entity.Property(x => x.ExchangeRate).HasColumnType("money");

        entity.Property(x => x.AccountingExchangeRate).HasColumnType("money");

        entity.Property(e => e.ActProvidingServiceDocumentId).HasColumnName("ActProvidingServiceDocumentID");

        entity.Property(e => e.SupplyServiceAccountDocumentId).HasColumnName("SupplyServiceAccountDocumentID");

        entity.Property(e => e.ActProvidingServiceId).HasColumnName("ActProvidingServiceID");

        entity.Property(e => e.AccountingActProvidingServiceId).HasColumnName("AccountingActProvidingServiceID");

        entity.HasOne(e => e.SupplyServiceAccountDocument)
            .WithOne(e => e.BillOfLadingService)
            .HasForeignKey<BillOfLadingService>(e => e.SupplyServiceAccountDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ActProvidingServiceDocument)
            .WithOne(e => e.BillOfLadingService)
            .HasForeignKey<BillOfLadingService>(e => e.ActProvidingServiceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.BillOfLadingServices)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.BillOfLadingServices)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AccountingPaymentTask)
            .WithMany(e => e.AccountingBillOfLadingServices)
            .HasForeignKey(e => e.AccountingPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganization)
            .WithMany(e => e.BillOfLadingServices)
            .HasForeignKey(e => e.SupplyOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.BillOfLadingServices)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.DeliveryProductProtocol)
            .WithOne(x => x.BillOfLadingService)
            .HasForeignKey<BillOfLadingService>(x => x.DeliveryProductProtocolId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.SupplyInformationTask)
            .WithOne(x => x.BillOfLadingService)
            .HasForeignKey<BillOfLadingService>(x => x.SupplyInformationTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ActProvidingService)
            .WithOne(e => e.BillOfLadingService)
            .HasForeignKey<BillOfLadingService>(e => e.ActProvidingServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AccountingActProvidingService)
            .WithOne(e => e.AccountingBillOfLadingService)
            .HasForeignKey<BillOfLadingService>(e => e.AccountingActProvidingServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}