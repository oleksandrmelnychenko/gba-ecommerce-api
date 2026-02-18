using GBA.Common.IdentityConfiguration.Entities;
using GBA.Data.MapConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class UserTokenMap : EntityTypeConfiguration<UserToken> {
    public override void Map(EntityTypeBuilder<UserToken> entity) {
        entity.ToTable("UserToken");

        entity.Property(e => e.Id).HasColumnName("ID");

        entity.Property(e => e.UserId)
            .HasColumnName("UserID")
            .HasMaxLength(450);
    }
}