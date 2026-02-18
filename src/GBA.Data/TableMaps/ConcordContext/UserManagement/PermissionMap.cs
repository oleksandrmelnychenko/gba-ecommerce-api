using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.UserManagement;

public sealed class PermissionMap : EntityBaseMap<Permission> {
    public override void Map(EntityTypeBuilder<Permission> entity) {
        base.Map(entity);

        entity.ToTable("Permission");

        entity.Property(e => e.DashboardNodeId).HasColumnName("DashboardNodeID");

        entity.Property(e => e.Name).HasMaxLength(500);

        entity.Property(e => e.Description).HasMaxLength(500);

        entity.HasOne(e => e.DashboardNode)
            .WithMany(e => e.Permissions)
            .HasForeignKey(e => e.DashboardNodeId);
    }
}