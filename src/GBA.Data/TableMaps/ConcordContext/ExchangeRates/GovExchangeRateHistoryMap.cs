using GBA.Domain.Entities.ExchangeRates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ExchangeRates;

public sealed class GovExchangeRateHistoryMap : EntityBaseMap<GovExchangeRateHistory> {
    public override void Map(EntityTypeBuilder<GovExchangeRateHistory> entity) {
        base.Map(entity);

        entity.ToTable("GovExchangeRateHistory");

        entity.Property(e => e.GovExchangeRateId).HasColumnName("GovExchangeRateID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.Amount).HasColumnType("decimal(30,14)");

        entity.HasOne(e => e.GovExchangeRate)
            .WithMany(e => e.GovExchangeRateHistories)
            .HasForeignKey(e => e.GovExchangeRateId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.GovExchangeRateHistories)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}