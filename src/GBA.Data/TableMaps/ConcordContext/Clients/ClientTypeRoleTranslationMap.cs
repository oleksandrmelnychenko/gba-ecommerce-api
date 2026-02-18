using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientTypeRoleTranslationMap : EntityBaseMap<ClientTypeRoleTranslation> {
    public override void Map(EntityTypeBuilder<ClientTypeRoleTranslation> entity) {
        base.Map(entity);

        entity.ToTable("ClientTypeRoleTranslation");

        entity.Property(e => e.ClientTypeRoleId).HasColumnName("ClientTypeRoleID");

        entity.Property(e => e.Name).HasMaxLength(75);

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.HasOne(e => e.ClientTypeRole)
            .WithMany(e => e.ClientTypeRoleTranslations)
            .HasForeignKey(e => e.ClientTypeRoleId);
    }
}