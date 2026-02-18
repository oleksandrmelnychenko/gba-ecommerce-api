using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Agreements;

public sealed class CalculationTypeTranslationMap : EntityBaseMap<CalculationTypeTranslation> {
    public override void Map(EntityTypeBuilder<CalculationTypeTranslation> entity) {
        base.Map(entity);

        entity.ToTable("CalculationTypeTranslation");

        entity.Property(e => e.CalculationTypeId).HasColumnName("CalculationTypeID");

        entity.Property(e => e.Name).HasMaxLength(75);

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.HasOne(e => e.CalculationType)
            .WithMany(e => e.CalculationTypeTranslations)
            .HasForeignKey(e => e.CalculationTypeId);
    }
}