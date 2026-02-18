using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class MergedServiceMap : EntityBaseMap<MergedService> {
    public override void Map(EntityTypeBuilder<MergedService> entity) {
        base.Map(entity);

        entity.ToTable("MergedService");

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.ServiceNumber).HasMaxLength(50);

        entity.Property(e => e.NetPrice).HasColumnType("money");

        entity.Property(e => e.GrossPrice).HasColumnType("money");

        entity.Property(e => e.Vat).HasColumnType("money");

        entity.Property(e => e.SupplyOrganizationId).HasColumnName("SupplyOrganizationID");

        entity.Property(e => e.SupplyPaymentTaskId).HasColumnName("SupplyPaymentTaskID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.Property(e => e.AccountingNetPrice).HasColumnType("money");

        entity.Property(e => e.AccountingGrossPrice).HasColumnType("money");

        entity.Property(e => e.AccountingVat).HasColumnType("money");

        entity.Property(e => e.AccountingPaymentTaskId).HasColumnName("AccountingPaymentTaskID");

        entity.Property(e => e.DeliveryProductProtocolId).HasColumnName("DeliveryProductProtocolID");

        entity.Property(e => e.SupplyExtraChargeType).HasDefaultValueSql("0");

        entity.Property(e => e.AccountingSupplyCostsWithinCountry).HasColumnType("money");

        entity.Property(e => e.SupplyInformationTaskId).HasColumnName("SupplyInformationTaskID");

        entity.Property(x => x.ExchangeRate).HasColumnType("money");

        entity.Property(x => x.AccountingExchangeRate).HasColumnType("money");

        entity.Property(e => e.ActProvidingServiceDocumentId).HasColumnName("ActProvidingServiceDocumentID");

        entity.Property(e => e.SupplyServiceAccountDocumentId).HasColumnName("SupplyServiceAccountDocumentID");

        entity.Property(e => e.ConsumableProductId).HasColumnName("ConsumableProductID");

        entity.Property(e => e.ActProvidingServiceId).HasColumnName("ActProvidingServiceID");

        entity.Property(e => e.AccountingActProvidingServiceId).HasColumnName("AccountingActProvidingServiceID");

        entity.HasOne(e => e.SupplyServiceAccountDocument)
            .WithOne(e => e.MergedService)
            .HasForeignKey<MergedService>(e => e.SupplyServiceAccountDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ActProvidingServiceDocument)
            .WithOne(e => e.MergedService)
            .HasForeignKey<MergedService>(e => e.ActProvidingServiceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganization)
            .WithMany(e => e.MergedServices)
            .HasForeignKey(e => e.SupplyOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyPaymentTask)
            .WithMany(e => e.MergedServices)
            .HasForeignKey(e => e.SupplyPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AccountingPaymentTask)
            .WithMany(e => e.AccountingMergedServices)
            .HasForeignKey(e => e.AccountingPaymentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.MergedServices)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.MergedServices)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.MergedServices)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraine)
            .WithMany(e => e.MergedServices)
            .HasForeignKey(e => e.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DeliveryProductProtocol)
            .WithMany(e => e.MergedServices)
            .HasForeignKey(e => e.DeliveryProductProtocolId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.SupplyInformationTask)
            .WithOne(x => x.MergedService)
            .HasForeignKey<MergedService>(x => x.SupplyInformationTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.ConsumableProduct)
            .WithMany(x => x.MergedServices)
            .HasForeignKey(x => x.ConsumableProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ActProvidingService)
            .WithOne(e => e.MergedService)
            .HasForeignKey<MergedService>(e => e.ActProvidingServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AccountingActProvidingService)
            .WithOne(e => e.AccountingMergedService)
            .HasForeignKey<MergedService>(e => e.AccountingActProvidingServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}