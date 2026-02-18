using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientRegistrationTaskMap : EntityBaseMap<ClientRegistrationTask> {
    public override void Map(EntityTypeBuilder<ClientRegistrationTask> entity) {
        base.Map(entity);

        entity.ToTable("ClientRegistrationTask");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.HasOne(e => e.Client)
            .WithMany(e => e.ClientRegistrationTasks)
            .HasForeignKey(e => e.ClientId);
    }
}