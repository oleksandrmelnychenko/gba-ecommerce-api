using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class RolePermissionMap : EntityBaseMap<RolePermission> {
    public override void Map(EntityTypeBuilder<RolePermission> entity) {
        base.Map(entity);

        entity.Property(e => e.PermissionId).HasColumnName("PermissionID");

        entity.Property(e => e.UserRoleId).HasColumnName("UserRoleID");
    }
}