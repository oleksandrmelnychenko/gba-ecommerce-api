using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientWorkplaceMap : EntityBaseMap<ClientWorkplace> {
    public override void Map(EntityTypeBuilder<ClientWorkplace> entity) {
        base.Map(entity);

        entity.ToTable("ClientWorkplace");

        entity.Property(e => e.MainClientId).HasColumnName("MainClientID");

        entity.Property(e => e.WorkplaceId).HasColumnName("WorkplaceID");

        entity.HasOne(e => e.MainClient)
            .WithMany(e => e.MainClients)
            .HasForeignKey(e => e.MainClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.WorkplaceClient)
            .WithMany(e => e.ClientWorkplaces)
            .HasForeignKey(e => e.WorkplaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}