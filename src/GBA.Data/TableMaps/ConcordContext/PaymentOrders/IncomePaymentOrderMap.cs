using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class IncomePaymentOrderMap : EntityBaseMap<IncomePaymentOrder> {
    public override void Map(EntityTypeBuilder<IncomePaymentOrder> entity) {
        base.Map(entity);

        entity.ToTable("IncomePaymentOrder");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.BankAccount).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Property(e => e.ArrivalNumber).HasMaxLength(450);

        entity.Property(e => e.PaymentPurpose).HasMaxLength(450);

        entity.Property(e => e.VAT).HasColumnType("money");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.ExchangeRate).HasColumnType("money");

        entity.Property(e => e.OverpaidAmount).HasColumnType("money");

        entity.Property(e => e.AgreementExchangedAmount).HasColumnType("money");

        entity.Property(e => e.EuroAmount).HasColumnType("money");

        entity.Property(e => e.AgreementEuroExchangeRate).HasColumnType("money");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.Property(e => e.PaymentRegisterId).HasColumnName("PaymentRegisterID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.ColleagueId).HasColumnName("ColleagueID");

        entity.Property(e => e.OrganizationClientId).HasColumnName("OrganizationClientID");

        entity.Property(e => e.OrganizationClientAgreementId).HasColumnName("OrganizationClientAgreementID");

        entity.Property(e => e.TaxFreeId).HasColumnName("TaxFreeID");

        entity.Property(e => e.SadId).HasColumnName("SadID");

        entity.Property(e => e.SupplyOrganizationId).HasColumnName("SupplyOrganizationID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Ignore(e => e.IsUpdated);

        entity.Ignore(e => e.OperationTypeName);

        entity.Ignore(e => e.TotalQty);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrganizationClient)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.OrganizationClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrganizationClientAgreement)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.OrganizationClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PaymentRegister)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.PaymentRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Colleague)
            .WithMany(e => e.ColleagueIncomePaymentOrders)
            .HasForeignKey(e => e.ColleagueId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFree)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.TaxFreeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sad)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.SadId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganization)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.SupplyOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.IncomePaymentOrders)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}