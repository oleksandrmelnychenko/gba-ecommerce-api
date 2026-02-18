using GBA.Domain.Entities.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ExchangeRates;

public sealed class ExchangeRateHistoryMap : EntityBaseMap<ExchangeRateHistory> {
    public override void Map(EntityTypeBuilder<ExchangeRateHistory> entity) {
        base.Map(entity);

        entity.ToTable("ExchangeRateHistory");

        entity.Property(e => e.ExchangeRateId).HasColumnName("ExchangeRateID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)");

        entity.HasOne(e => e.ExchangeRate)
            .WithMany(e => e.ExchangeRateHistories)
            .HasForeignKey(e => e.ExchangeRateId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.ExchangeRateHistories)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}