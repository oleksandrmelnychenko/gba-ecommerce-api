using GBA.Domain.Entities.Supplies.HelperServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.HelperServices;

public sealed class ServiceDetailItemMap : EntityBaseMap<ServiceDetailItem> {
    public override void Map(EntityTypeBuilder<ServiceDetailItem> entity) {
        base.Map(entity);

        entity.ToTable("ServiceDetailItem");

        entity.Property(e => e.ServiceDetailItemKeyId).HasColumnName("ServiceDetailItemKeyID");

        entity.Property(e => e.PortWorkServiceId).HasColumnName("PortWorkServiceID");

        entity.Property(e => e.TransportationServiceId).HasColumnName("TransportationServiceID");

        entity.Property(e => e.CustomServiceId).HasColumnName("CustomServiceID");

        entity.Property(e => e.VehicleDeliveryServiceId).HasColumnName("VehicleDeliveryServiceID");

        entity.Property(e => e.CustomAgencyServiceId).HasColumnName("CustomAgencyServiceID");

        entity.Property(e => e.PlaneDeliveryServiceId).HasColumnName("PlaneDeliveryServiceID");

        entity.Property(e => e.PortCustomAgencyServiceId).HasColumnName("PortCustomAgencyServiceID");

        entity.Property(e => e.MergedServiceId).HasColumnName("MergedServiceID");

        entity.HasOne(e => e.ServiceDetailItemKey)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.ServiceDetailItemKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PortWorkService)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.PortWorkServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TransportationService)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.TransportationServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomService)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.CustomServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomService)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.CustomServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomAgencyService)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.CustomAgencyServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PlaneDeliveryService)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.PlaneDeliveryServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PortCustomAgencyService)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.PortCustomAgencyServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.MergedService)
            .WithMany(e => e.ServiceDetailItems)
            .HasForeignKey(e => e.MergedServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}