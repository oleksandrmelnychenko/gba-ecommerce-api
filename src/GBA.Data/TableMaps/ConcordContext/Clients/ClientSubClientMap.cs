using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientSubClientMap : EntityBaseMap<ClientSubClient> {
    public override void Map(EntityTypeBuilder<ClientSubClient> entity) {
        base.Map(entity);

        entity.ToTable("ClientSubClient");

        entity.Property(e => e.RootClientId).HasColumnName("RootClientID");

        entity.Property(e => e.SubClientId).HasColumnName("SubClientID");

        entity.HasOne(e => e.RootClient)
            .WithMany(e => e.RootClients)
            .HasForeignKey(e => e.RootClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SubClient)
            .WithMany(e => e.SubClients)
            .HasForeignKey(e => e.SubClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}