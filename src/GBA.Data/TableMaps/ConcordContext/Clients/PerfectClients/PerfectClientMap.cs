using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients.PerfectClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients.PerfectClients;

public sealed class PerfectClientMap : EntityBaseMap<PerfectClient> {
    public override void Map(EntityTypeBuilder<PerfectClient> entity) {
        base.Map(entity);

        entity.ToTable("PerfectClient");

        entity.Property(e => e.IsSelected).HasDefaultValueSql("0");

        entity.Property(e => e.Type).HasDefaultValue(PerfectClientType.Checkbox);

        entity.Property(e => e.Lable).HasMaxLength(100);

        entity.Property(e => e.Description).HasMaxLength(250);

        entity.Property(e => e.ClientTypeRoleId).HasColumnName("ClientTypeRoleID");

        entity.HasOne(e => e.ClientTypeRole)
            .WithMany(e => e.PerfectClients)
            .HasForeignKey(e => e.ClientTypeRoleId);
    }
}