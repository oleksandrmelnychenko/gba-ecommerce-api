using GBA.Domain.Entities.PaymentOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders;

public sealed class PaymentCurrencyRegisterMap : EntityBaseMap<PaymentCurrencyRegister> {
    public override void Map(EntityTypeBuilder<PaymentCurrencyRegister> entity) {
        base.Map(entity);

        entity.ToTable("PaymentCurrencyRegister");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.InitialAmount).HasColumnType("money");

        entity.Property(e => e.PaymentRegisterId).HasColumnName("PaymentRegisterID");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.Ignore(e => e.RangeTotal);

        entity.Ignore(e => e.BeforeRangeTotal);

        entity.Ignore(e => e.PaymentRegisterTransfers);

        entity.Ignore(e => e.PaymentRegisterCurrencyExchanges);

        entity.HasOne(e => e.PaymentRegister)
            .WithMany(e => e.PaymentCurrencyRegisters)
            .HasForeignKey(e => e.PaymentRegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.PaymentCurrencyRegisters)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}