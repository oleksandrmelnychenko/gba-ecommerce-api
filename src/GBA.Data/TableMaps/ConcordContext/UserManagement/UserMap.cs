using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class UserMap : EntityBaseMap<User> {
    public override void Map(EntityTypeBuilder<User> entity) {
        base.Map(entity);

        entity.ToTable("User");

        entity.Property(e => e.UserRoleId).HasColumnName("UserRoleID");

        entity.Property(e => e.IsActive).HasDefaultValueSql("1");

        entity.HasOne(e => e.UserRole)
            .WithMany(e => e.Users)
            .HasForeignKey(e => e.UserRoleId);

        entity.HasOne(e => e.UserDetails)
            .WithMany(e => e.Users)
            .HasForeignKey(e => e.UserDetailsId);
    }
}