using GBA.Domain.Entities.Dashboards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Dashboards;

public sealed class DashboardNodeModuleMap : EntityBaseMap<DashboardNodeModule> {
    public override void Map(EntityTypeBuilder<DashboardNodeModule> entity) {
        base.Map(entity);

        entity.ToTable("DashboardNodeModule");

        entity.Property(e => e.Language).HasMaxLength(2);

        entity.Property(e => e.Module).HasMaxLength(75);

        entity.Property(e => e.CssClass).HasMaxLength(200);

        entity.Property(e => e.Description).HasMaxLength(500);
    }
}