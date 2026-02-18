using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class PaymentRegisterTransferMap : EntityBaseMap<PaymentRegisterTransfer> {
    public override void Map(EntityTypeBuilder<PaymentRegisterTransfer> entity) {
        base.Map(entity);

        entity.ToTable("PaymentRegisterTransfer");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.FromPaymentCurrencyRegisterId).HasColumnName("FromPaymentCurrencyRegisterID");

        entity.Property(e => e.ToPaymentCurrencyRegisterId).HasColumnName("ToPaymentCurrencyRegisterID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.TypeOfOperation).HasColumnName("TypeOfOperation");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Ignore(e => e.Type);

        entity.Ignore(e => e.IsUpdated);

        entity.HasOne(e => e.FromPaymentCurrencyRegister)
            .WithMany(e => e.FromPaymentRegisterTransfers)
            .HasForeignKey(e => e.FromPaymentCurrencyRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ToPaymentCurrencyRegister)
            .WithMany(e => e.ToPaymentRegisterTransfers)
            .HasForeignKey(e => e.ToPaymentCurrencyRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.PaymentRegisterTransfers)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}