using GBA.Domain.Entities.Clients.PerfectClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients.PerfectClients;

public sealed class PerfectClientValueMap : EntityBaseMap<PerfectClientValue> {
    public override void Map(EntityTypeBuilder<PerfectClientValue> entity) {
        base.Map(entity);

        entity.ToTable("PerfectClientValue");

        entity.Property(e => e.PerfectClientId).HasColumnName("PerfectClientID");

        entity.HasOne(e => e.PerfectClient)
            .WithMany(e => e.Values)
            .HasForeignKey(e => e.PerfectClientId);
    }
}