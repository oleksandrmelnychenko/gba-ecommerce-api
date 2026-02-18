using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class RetailClientMap : EntityBaseMap<RetailClient> {
    public override void Map(EntityTypeBuilder<RetailClient> entity) {
        base.Map(entity);

        entity.ToTable("RetailClient");

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.PhoneNumber).IsRequired();

        entity.HasOne(e => e.EcommerceRegion)
            .WithMany(e => e.RetailClients)
            .HasForeignKey(e => e.EcommerceRegionId);
    }
}