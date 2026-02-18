using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class ChartMonthTranslationMap : EntityBaseMap<ChartMonthTranslation> {
    public override void Map(EntityTypeBuilder<ChartMonthTranslation> entity) {
        base.Map(entity);

        entity.ToTable("ChartMonthTranslation");

        entity.Property(e => e.ChartMonthId).HasColumnName("ChartMonthID");

        entity.HasOne(e => e.ChartMonth)
            .WithMany(e => e.ChartMonthTranslations)
            .HasForeignKey(e => e.ChartMonthId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}