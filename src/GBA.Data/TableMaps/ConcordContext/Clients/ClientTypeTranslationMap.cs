using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientTypeTranslationMap : EntityBaseMap<ClientTypeTranslation> {
    public override void Map(EntityTypeBuilder<ClientTypeTranslation> entity) {
        base.Map(entity);

        entity.ToTable("ClientTypeTranslation");

        entity.Property(e => e.ClientTypeId).HasColumnName("ClientTypeID");

        entity.Property(e => e.Name).HasMaxLength(75);

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.HasOne(e => e.ClientType)
            .WithMany(e => e.ClientTypeTranslations)
            .HasForeignKey(e => e.ClientTypeId);
    }
}