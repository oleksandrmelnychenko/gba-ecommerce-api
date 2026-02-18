using GBA.Domain.Entities.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.CrossExchangeRates;

public sealed class CrossExchangeRateHistoryMap : EntityBaseMap<CrossExchangeRateHistory> {
    public override void Map(EntityTypeBuilder<CrossExchangeRateHistory> entity) {
        base.Map(entity);

        entity.ToTable("CrossExchangeRateHistory");

        entity.Property(e => e.CrossExchangeRateId).HasColumnName("CrossExchangeRateID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)");

        entity.HasOne(e => e.CrossExchangeRate)
            .WithMany(e => e.CrossExchangeRateHistories)
            .HasForeignKey(e => e.CrossExchangeRateId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.CrossExchangeRateHistories)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}