using GBA.Domain.Entities.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.CrossExchangeRates;

public sealed class GovCrossExchangeRateHistoryMap : EntityBaseMap<GovCrossExchangeRateHistory> {
    public override void Map(EntityTypeBuilder<GovCrossExchangeRateHistory> entity) {
        base.Map(entity);

        entity.ToTable("GovCrossExchangeRateHistory");

        entity.Property(e => e.GovCrossExchangeRateId).HasColumnName("GovCrossExchangeRateID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)");

        entity.HasOne(e => e.CrossExchangeRate)
            .WithMany(e => e.GovCrossExchangeRateHistories)
            .HasForeignKey(e => e.GovCrossExchangeRateId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.GovCrossExchangeRateHistories)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}