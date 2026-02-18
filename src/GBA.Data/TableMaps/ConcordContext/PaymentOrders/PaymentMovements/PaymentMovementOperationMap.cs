using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders.PaymentMovements;

public sealed class PaymentMovementOperationMap : EntityBaseMap<PaymentMovementOperation> {
    public override void Map(EntityTypeBuilder<PaymentMovementOperation> entity) {
        base.Map(entity);

        entity.ToTable("PaymentMovementOperation");

        entity.Property(e => e.PaymentMovementId).HasColumnName("PaymentMovementID");

        entity.Property(e => e.IncomePaymentOrderId).HasColumnName("IncomePaymentOrderID");

        entity.Property(e => e.OutcomePaymentOrderId).HasColumnName("OutcomePaymentOrderID");

        entity.Property(e => e.PaymentRegisterTransferId).HasColumnName("PaymentRegisterTransferID");

        entity.Property(e => e.PaymentRegisterCurrencyExchangeId).HasColumnName("PaymentRegisterCurrencyExchangeID");

        entity.HasOne(e => e.PaymentMovement)
            .WithMany(e => e.PaymentMovementOperations)
            .HasForeignKey(e => e.PaymentMovementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.IncomePaymentOrder)
            .WithOne(e => e.PaymentMovementOperation)
            .HasForeignKey<PaymentMovementOperation>(e => e.IncomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OutcomePaymentOrder)
            .WithOne(e => e.PaymentMovementOperation)
            .HasForeignKey<PaymentMovementOperation>(e => e.OutcomePaymentOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PaymentRegisterTransfer)
            .WithOne(e => e.PaymentMovementOperation)
            .HasForeignKey<PaymentMovementOperation>(e => e.PaymentRegisterTransferId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PaymentRegisterCurrencyExchange)
            .WithOne(e => e.PaymentMovementOperation)
            .HasForeignKey<PaymentMovementOperation>(e => e.PaymentRegisterCurrencyExchangeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}