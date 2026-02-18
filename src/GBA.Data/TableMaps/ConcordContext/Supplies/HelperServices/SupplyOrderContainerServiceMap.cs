using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class SupplyOrderContainerServiceMap : EntityBaseMap<SupplyOrderContainerService> {
    public override void Map(EntityTypeBuilder<SupplyOrderContainerService> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderContainerService");

        entity.Property(e => e.ContainerServiceId).HasColumnName("ContainerServiceID");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.HasOne(e => e.ContainerService)
            .WithMany(e => e.SupplyOrderContainerServices)
            .HasForeignKey(e => e.ContainerServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.SupplyOrderContainerServices)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}