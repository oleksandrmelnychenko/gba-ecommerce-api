using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class DeliveryExpenseMap : EntityBaseMap<DeliveryExpense> {
    public override void Map(EntityTypeBuilder<DeliveryExpense> entity) {
        base.Map(entity);

        entity.ToTable("DeliveryExpense");

        entity.Property(e => e.InvoiceNumber).HasMaxLength(50);

        entity.Property(e => e.GrossAmount).HasColumnType("money");

        entity.Property(e => e.VatPercent).HasColumnType("money");

        entity.Property(e => e.SupplyOrganizationId).HasColumnName("SupplyOrganizationID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.Property(e => e.AccountingGrossAmount).HasColumnType("money");

        entity.Property(e => e.AccountingVatPercent).HasColumnType("money");

        entity.Property(e => e.ActProvidingServiceDocumentId).HasColumnName("ActProvidingServiceDocumentID");

        entity.Property(e => e.ConsumableProductId).HasColumnName("ConsumableProductID");

        entity.Property(e => e.ActProvidingServiceId).HasColumnName("ActProvidingServiceID");

        entity.Property(e => e.AccountingActProvidingServiceId).HasColumnName("AccountingActProvidingServiceID");

        entity.Ignore(e => e.VatAmount);

        entity.HasOne(e => e.ActProvidingServiceDocument)
            .WithOne(e => e.DeliveryExpense)
            .HasForeignKey<DeliveryExpense>(e => e.ActProvidingServiceDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganization)
            .WithMany(e => e.DeliveryExpenses)
            .HasForeignKey(e => e.SupplyOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.DeliveryExpenses)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.DeliveryExpenses)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraine)
            .WithMany(e => e.DeliveryExpenses)
            .HasForeignKey(e => e.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.ConsumableProduct)
            .WithMany(x => x.DeliveryExpenses)
            .HasForeignKey(x => x.ConsumableProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ActProvidingService)
            .WithOne(e => e.DeliveryExpense)
            .HasForeignKey<DeliveryExpense>(e => e.ActProvidingServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AccountingActProvidingService)
            .WithOne(e => e.AccountingDeliveryExpense)
            .HasForeignKey<DeliveryExpense>(e => e.AccountingActProvidingServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}