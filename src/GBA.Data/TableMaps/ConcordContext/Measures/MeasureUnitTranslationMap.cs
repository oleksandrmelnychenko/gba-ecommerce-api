using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Measures;

public sealed class MeasureUnitTranslationMap : EntityBaseMap<MeasureUnitTranslation> {
    public override void Map(EntityTypeBuilder<MeasureUnitTranslation> entity) {
        base.Map(entity);

        entity.ToTable("MeasureUnitTranslation");

        entity.Property(e => e.MeasureUnitId).HasColumnName("MeasureUnitID");

        entity.HasOne(e => e.MeasureUnit)
            .WithMany(e => e.MeasureUnitTranslations)
            .HasForeignKey(e => e.MeasureUnitId);
    }
}