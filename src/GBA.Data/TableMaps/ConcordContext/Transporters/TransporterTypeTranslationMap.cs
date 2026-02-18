using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Transporters;

public sealed class TransporterTypeTranslationMap : EntityBaseMap<TransporterTypeTranslation> {
    public override void Map(EntityTypeBuilder<TransporterTypeTranslation> entity) {
        base.Map(entity);

        entity.ToTable("TransporterTypeTranslation");

        entity.Property(e => e.TransporterTypeId).HasColumnName("TransporterTypeID");

        entity.Property(e => e.Name).HasMaxLength(75);

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.HasOne(e => e.TransporterType)
            .WithMany(e => e.TransporterTypeTranslations)
            .HasForeignKey(e => e.TransporterTypeId);
    }
}