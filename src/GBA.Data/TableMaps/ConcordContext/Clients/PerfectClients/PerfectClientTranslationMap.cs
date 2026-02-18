using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients.PerfectClients;

public sealed class PerfectClientTranslationMap : EntityBaseMap<PerfectClientTranslation> {
    public override void Map(EntityTypeBuilder<PerfectClientTranslation> entity) {
        base.Map(entity);

        entity.ToTable("PerfectClientTranslation");

        entity.Property(e => e.PerfectClientId).HasColumnName("PerfectClientID");

        entity.HasOne(e => e.PerfectClient)
            .WithMany(e => e.PerfectClientTranslations)
            .HasForeignKey(e => e.PerfectClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}