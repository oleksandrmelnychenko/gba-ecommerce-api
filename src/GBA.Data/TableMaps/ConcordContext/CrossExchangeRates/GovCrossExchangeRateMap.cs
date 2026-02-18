using GBA.Domain.Entities.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.CrossExchangeRates;

public sealed class GovCrossExchangeRateMap : EntityBaseMap<GovCrossExchangeRate> {
    public override void Map(EntityTypeBuilder<GovCrossExchangeRate> entity) {
        base.Map(entity);

        entity.ToTable("GovCrossExchangeRate");

        entity.Property(e => e.CurrencyFromId).HasColumnName("CurrencyFromID");

        entity.Property(e => e.CurrencyToId).HasColumnName("CurrencyToID");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)");

        entity.Property(e => e.Culture).HasMaxLength(5);

        entity.Property(e => e.Code).HasMaxLength(30);

        entity.Ignore(e => e.GovExchangeRate);

        entity.HasOne(e => e.CurrencyFrom)
            .WithMany(e => e.FromGovCrossExchangeRates)
            .HasForeignKey(e => e.CurrencyFromId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CurrencyTo)
            .WithMany(e => e.ToGovCrossExchangeRates)
            .HasForeignKey(e => e.CurrencyToId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}