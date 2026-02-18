using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientTypeRoleMap : EntityBaseMap<ClientTypeRole> {
    public override void Map(EntityTypeBuilder<ClientTypeRole> entity) {
        base.Map(entity);

        entity.ToTable("ClientTypeRole");

        entity.Property(e => e.Name).HasMaxLength(75);

        entity.Property(e => e.ClientTypeId).HasColumnName("ClientTypeID");

        entity.HasOne(e => e.ClientType)
            .WithMany(e => e.ClientTypeRoles)
            .HasForeignKey(e => e.ClientTypeId);
    }
}