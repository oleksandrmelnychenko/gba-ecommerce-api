using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class CurrencyTraderExchangeRateMap : EntityBaseMap<CurrencyTraderExchangeRate> {
    public override void Map(EntityTypeBuilder<CurrencyTraderExchangeRate> entity) {
        base.Map(entity);

        entity.Property(e => e.CurrencyTraderId).HasColumnName("CurrencyTraderID");

        entity.Property(e => e.ExchangeRate).HasColumnType("money");

        entity.Property(e => e.CurrencyName).HasMaxLength(25);

        entity.ToTable("CurrencyTraderExchangeRate");

        entity.HasOne(e => e.CurrencyTrader)
            .WithMany(e => e.CurrencyTraderExchangeRates)
            .HasForeignKey(e => e.CurrencyTraderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}