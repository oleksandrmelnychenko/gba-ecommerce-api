using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class UserRoleTranslationMap : EntityBaseMap<UserRoleTranslation> {
    public override void Map(EntityTypeBuilder<UserRoleTranslation> entity) {
        base.Map(entity);

        entity.ToTable("UserRoleTranslation");

        entity.Property(e => e.UserRoleId).HasColumnName("UserRoleID");

        entity.Property(e => e.Name).HasMaxLength(75);

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.HasOne(e => e.UserRole)
            .WithMany(e => e.UserRoleTranslations)
            .HasForeignKey(e => e.UserRoleId);
    }
}