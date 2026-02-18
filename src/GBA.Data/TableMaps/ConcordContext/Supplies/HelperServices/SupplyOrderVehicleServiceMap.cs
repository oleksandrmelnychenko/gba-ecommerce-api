using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class SupplyOrderVehicleServiceMap : EntityBaseMap<SupplyOrderVehicleService> {
    public override void Map(EntityTypeBuilder<SupplyOrderVehicleService> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderVehicleService");

        entity.Property(e => e.VehicleServiceId).HasColumnName("VehicleServiceID");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.HasOne(e => e.VehicleService)
            .WithMany(e => e.SupplyOrderVehicleServices)
            .HasForeignKey(e => e.VehicleServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.SupplyOrderVehicleServices)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}