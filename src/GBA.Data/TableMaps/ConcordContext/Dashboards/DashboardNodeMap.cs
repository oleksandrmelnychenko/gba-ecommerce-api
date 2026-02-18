using GBA.Domain.Entities.Dashboards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Dashboards;

public sealed class DashboardNodeMap : EntityBaseMap<DashboardNode> {
    public override void Map(EntityTypeBuilder<DashboardNode> entity) {
        base.Map(entity);

        entity.ToTable("DashboardNode");

        entity.Property(e => e.Language).HasMaxLength(2);

        entity.Property(e => e.Module).HasMaxLength(75);

        entity.Property(e => e.Route).HasMaxLength(4000);

        entity.Property(e => e.CssClass).HasMaxLength(200);

        entity.Property(e => e.ParentDashboardNodeId).HasColumnName("ParentDashboardNodeID");

        entity.Property(e => e.DashboardNodeModuleId).HasColumnName("DashboardNodeModuleID");

        entity.HasOne(e => e.ParentDashboardNode)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentDashboardNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DashboardNodeModule)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.DashboardNodeModuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}