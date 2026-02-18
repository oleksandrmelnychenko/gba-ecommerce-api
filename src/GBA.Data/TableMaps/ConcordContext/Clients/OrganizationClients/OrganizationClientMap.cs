using GBA.Domain.Entities.Clients.OrganizationClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients.OrganizationClients;

public sealed class OrganizationClientMap : EntityBaseMap<OrganizationClient> {
    public override void Map(EntityTypeBuilder<OrganizationClient> entity) {
        base.Map(entity);

        entity.ToTable("OrganizationClient");

        entity.Property(e => e.FullName).HasMaxLength(500);

        entity.Property(e => e.Address).HasMaxLength(500);

        entity.Property(e => e.Country).HasMaxLength(100);

        entity.Property(e => e.City).HasMaxLength(100);

        entity.Property(e => e.NIP).HasMaxLength(100);

        entity.Property(e => e.MarginAmount).HasColumnType("money");
    }
}