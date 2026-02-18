using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Organizations;

public sealed class OrganizationTranslationMap : EntityBaseMap<OrganizationTranslation> {
    public override void Map(EntityTypeBuilder<OrganizationTranslation> entity) {
        base.Map(entity);

        entity.ToTable("OrganizationTranslation");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.Name).HasMaxLength(100);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.OrganizationTranslations)
            .HasForeignKey(e => e.OrganizationId);
    }
}