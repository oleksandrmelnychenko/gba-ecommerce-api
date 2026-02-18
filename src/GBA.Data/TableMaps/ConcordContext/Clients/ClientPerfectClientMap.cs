using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientPerfectClientMap : EntityBaseMap<ClientPerfectClient> {
    public override void Map(EntityTypeBuilder<ClientPerfectClient> entity) {
        base.Map(entity);

        entity.ToTable("ClientPerfectClient");

        entity.Property(e => e.PerfectClientValueId).HasColumnName("PerfectClientValueID");

        entity.Property(e => e.PerfectClientId).HasColumnName("PerfectClientID");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.HasOne(e => e.Client)
            .WithMany(e => e.PerfectClientValues)
            .HasForeignKey(e => e.ClientId);

        entity.HasOne(e => e.PerfectClient)
            .WithMany(e => e.ClientPerfectClients)
            .HasForeignKey(e => e.PerfectClientId);

        entity.HasOne(e => e.PerfectClientValue)
            .WithMany(e => e.ClientPerfectClients)
            .HasForeignKey(e => e.PerfectClientValueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}