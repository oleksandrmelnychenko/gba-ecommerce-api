using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class UserRoleMap : EntityBaseMap<UserRole> {
    public override void Map(EntityTypeBuilder<UserRole> entity) {
        base.Map(entity);

        entity.ToTable("UserRole");

        entity.Property(e => e.Name).HasMaxLength(40);

        entity.Property(e => e.Dashboard).HasMaxLength(100);

        entity.Ignore(e => e.DashboardNodeModules);

        entity.HasMany(e => e.Permissions)
            .WithMany(e => e.UserRoles)
            .UsingEntity<RolePermission>();

        entity.HasMany(e => e.DashboardNodes)
            .WithMany(e => e.UserRoles)
            .UsingEntity<UserRoleDashboardNode>();
    }
}