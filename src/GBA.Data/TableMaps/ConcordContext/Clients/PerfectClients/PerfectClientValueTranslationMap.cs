using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients.PerfectClients;

public sealed class PerfectClientValueTranslationMap : EntityBaseMap<PerfectClientValueTranslation> {
    public override void Map(EntityTypeBuilder<PerfectClientValueTranslation> entity) {
        base.Map(entity);

        entity.ToTable("PerfectClientValueTranslation");

        entity.Property(e => e.PerfectClientValueId).HasColumnName("PerfectClientValueID");

        entity.HasOne(e => e.PerfectClientValue)
            .WithMany(e => e.PerfectClientValueTranslations)
            .HasForeignKey(e => e.PerfectClientValueId);
    }
}