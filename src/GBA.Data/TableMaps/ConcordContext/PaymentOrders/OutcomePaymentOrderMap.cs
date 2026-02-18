using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class OutcomePaymentOrderMap : EntityBaseMap<OutcomePaymentOrder> {
    public override void Map(EntityTypeBuilder<OutcomePaymentOrder> entity) {
        base.Map(entity);

        entity.ToTable("OutcomePaymentOrder");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.CustomNumber).HasMaxLength(50);

        entity.Property(e => e.AdvanceNumber).HasMaxLength(50);

        entity.Property(e => e.ArrivalNumber).HasMaxLength(100);

        entity.Property(e => e.PaymentPurpose).HasMaxLength(500);

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Property(e => e.IsCanceled).HasDefaultValueSql("0");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.EuroAmount).HasColumnType("money");

        entity.Property(e => e.AfterExchangeAmount).HasColumnType("money");

        entity.Property(e => e.ExchangeRate).HasColumnType("money");

        entity.Property(e => e.VAT).HasColumnType("money");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.PaymentCurrencyRegisterId).HasColumnName("PaymentCurrencyRegisterID");

        entity.Property(e => e.ColleagueId).HasColumnName("ColleagueID");

        entity.Property(e => e.ConsumableProductOrganizationId).HasColumnName("ConsumableProductOrganizationID");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.SupplyOrderPolandPaymentDeliveryProtocolId).HasColumnName("SupplyOrderPolandPaymentDeliveryProtocolID");

        entity.Property(e => e.SupplyOrganizationAgreementId).HasColumnName("SupplyOrganizationAgreementID");

        entity.Property(e => e.OrganizationClientId).HasColumnName("OrganizationClientID");

        entity.Property(e => e.OrganizationClientAgreementId).HasColumnName("OrganizationClientAgreementID");

        entity.Property(e => e.TaxFreeId).HasColumnName("TaxFreeID");

        entity.Property(e => e.SadId).HasColumnName("SadID");

        entity.Ignore(e => e.IsUpdated);

        entity.Ignore(e => e.DifferenceAmount);

        entity.Ignore(e => e.AddedFuelAmount);

        entity.Ignore(e => e.SpentFuelAmount);

        entity.Ignore(e => e.OperationTypeName);

        entity.Ignore(e => e.TotalRowsQty);

        entity.HasOne(e => e.User)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PaymentCurrencyRegister)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.PaymentCurrencyRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Colleague)
            .WithMany(e => e.ColleagueOutcomePaymentOrders)
            .HasForeignKey(e => e.ColleagueId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsumableProductOrganization)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.ConsumableProductOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrganizationClient)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.OrganizationClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrganizationClientAgreement)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.OrganizationClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderPolandPaymentDeliveryProtocol)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.SupplyOrderPolandPaymentDeliveryProtocolId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrganizationAgreement)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.SupplyOrganizationAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFree)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.TaxFreeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sad)
            .WithMany(e => e.OutcomePaymentOrders)
            .HasForeignKey(e => e.SadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}