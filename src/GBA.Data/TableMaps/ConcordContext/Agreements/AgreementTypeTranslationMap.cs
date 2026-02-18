using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Agreements;

public sealed class AgreementTypeTranslationMap : EntityBaseMap<AgreementTypeTranslation> {
    public override void Map(EntityTypeBuilder<AgreementTypeTranslation> entity) {
        base.Map(entity);

        entity.ToTable("AgreementTypeTranslation");

        entity.Property(e => e.AgreementTypeId).HasColumnName("AgreementTypeID");

        entity.Property(e => e.Name).HasMaxLength(75);

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.HasOne(e => e.AgreementType)
            .WithMany(e => e.AgreementTypeTranslations)
            .HasForeignKey(e => e.AgreementTypeId);
    }
}