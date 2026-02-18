using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Currencies;

public sealed class CurrencyTranslationMap : EntityBaseMap<CurrencyTranslation> {
    public override void Map(EntityTypeBuilder<CurrencyTranslation> entity) {
        base.Map(entity);

        entity.ToTable("CurrencyTranslation");

        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.CurrencyTranslations)
            .HasForeignKey(e => e.CurrencyId);
    }
}