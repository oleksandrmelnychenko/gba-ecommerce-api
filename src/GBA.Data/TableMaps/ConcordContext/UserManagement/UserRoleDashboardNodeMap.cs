using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class UserRoleDashboardNodeMap : EntityBaseMap<UserRoleDashboardNode> {
    public override void Map(EntityTypeBuilder<UserRoleDashboardNode> entity) {
        base.Map(entity);

        entity.ToTable("UserRoleDashboardNode");

        entity.Property(e => e.UserRoleId).HasColumnName("UserRoleID");

        entity.Property(e => e.DashboardNodeId).HasColumnName("DashboardNodeID");
    }
}