using GBA.Domain.Entities.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.CrossExchangeRates;

public sealed class CrossExchangeRateMap : EntityBaseMap<CrossExchangeRate> {
    public override void Map(EntityTypeBuilder<CrossExchangeRate> entity) {
        base.Map(entity);

        entity.ToTable("CrossExchangeRate");

        entity.Property(e => e.CurrencyFromId).HasColumnName("CurrencyFromID");

        entity.Property(e => e.CurrencyToId).HasColumnName("CurrencyToID");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)");

        entity.Property(e => e.Culture).HasMaxLength(5);

        entity.Property(e => e.Code).HasMaxLength(30);

        entity.HasOne(e => e.CurrencyFrom)
            .WithMany(e => e.FromCrossExchangeRates)
            .HasForeignKey(e => e.CurrencyFromId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CurrencyTo)
            .WithMany(e => e.ToCrossExchangeRates)
            .HasForeignKey(e => e.CurrencyToId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}