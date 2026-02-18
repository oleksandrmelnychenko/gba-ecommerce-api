using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientTypeMap : EntityBaseMap<ClientType> {
    public override void Map(EntityTypeBuilder<ClientType> entity) {
        base.Map(entity);

        entity.ToTable("ClientType");

        entity.Property(e => e.Name).HasMaxLength(75);
    }
}