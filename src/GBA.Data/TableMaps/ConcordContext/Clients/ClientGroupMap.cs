using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientGroupMap : EntityBaseMap<ClientGroup> {
    public override void Map(EntityTypeBuilder<ClientGroup> entity) {
        base.Map(entity);

        entity.ToTable("ClientGroup");

        entity.Property(e => e.Name).HasMaxLength(500);

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.HasOne(e => e.Client)
            .WithMany(e => e.ClientGroups)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}