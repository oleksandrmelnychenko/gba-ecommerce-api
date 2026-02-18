using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientInRoleMap : EntityBaseMap<ClientInRole> {
    public override void Map(EntityTypeBuilder<ClientInRole> entity) {
        base.Map(entity);

        entity.ToTable("ClientInRole");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ClientTypeId).HasColumnName("ClientTypeID");

        entity.Property(e => e.ClientTypeRoleId).HasColumnName("ClientTypeRoleID");

        entity.HasOne(e => e.ClientType)
            .WithMany(e => e.ClientInRoles)
            .HasForeignKey(e => e.ClientTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Client)
            .WithOne(e => e.ClientInRole)
            .HasForeignKey<ClientInRole>(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientTypeRole)
            .WithMany(e => e.ClientInRoles)
            .HasForeignKey(e => e.ClientTypeRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}