using GBA.Domain.Entities.Supplies.PackingLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.PackingLists;

public sealed class PackingListPackageOrderItemSupplyServiceMap : EntityBaseMap<PackingListPackageOrderItemSupplyService> {
    public override void Map(EntityTypeBuilder<PackingListPackageOrderItemSupplyService> entity) {
        base.Map(entity);

        entity.ToTable("PackingListPackageOrderItemSupplyService");

        entity.Property(e => e.PackingListPackageOrderItemId).HasColumnName("PackingListPackageOrderItemID");
        entity.Property(e => e.CurrencyId).HasColumnName("CurrencyID");
        entity.Property(e => e.BillOfLadingServiceId).HasColumnName("BillOfLadingServiceID");
        entity.Property(e => e.ContainerServiceId).HasColumnName("ContainerServiceID");
        entity.Property(e => e.CustomAgencyServiceId).HasColumnName("CustomAgencyServiceID");
        entity.Property(e => e.CustomServiceId).HasColumnName("CustomServiceID");
        entity.Property(e => e.MergedServiceId).HasColumnName("MergedServiceID");
        entity.Property(e => e.PlaneDeliveryServiceId).HasColumnName("PlaneDeliveryServiceID");
        entity.Property(e => e.PortCustomAgencyServiceId).HasColumnName("PortCustomAgencyServiceID");
        entity.Property(e => e.PortWorkServiceId).HasColumnName("PortWorkServiceID");
        entity.Property(e => e.TransportationServiceId).HasColumnName("TransportationServiceID");
        entity.Property(e => e.VehicleDeliveryServiceId).HasColumnName("VehicleDeliveryServiceID");
        entity.Property(e => e.VehicleServiceId).HasColumnName("VehicleServiceID");

        entity.Property(e => e.NetValue).HasColumnType("decimal(30,14)");
        entity.Property(e => e.GeneralValue).HasColumnType("decimal(30,14)");
        entity.Property(e => e.ManagementValue).HasColumnType("decimal(30,14)");

        entity.Property(e => e.Name).HasMaxLength(250);

        entity.Property(e => e.ExchangeRateDate).HasDefaultValueSql("getutcdate()");

        entity.HasOne(e => e.PackingListPackageOrderItem)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.PackingListPackageOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Currency)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.BillOfLadingService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.BillOfLadingServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ContainerService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.ContainerServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomAgencyService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.CustomAgencyServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.CustomServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.MergedService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.MergedServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PlaneDeliveryService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.PlaneDeliveryServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PortCustomAgencyService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.PortCustomAgencyServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PortWorkService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.PortWorkServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TransportationService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.TransportationServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.VehicleDeliveryService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.VehicleDeliveryServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.VehicleService)
            .WithMany(e => e.PackingListPackageOrderItemSupplyServices)
            .HasForeignKey(e => e.VehicleServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Ignore(x => x.NetValueEur);

        entity.Ignore(x => x.NetValueUah);

        entity.Ignore(x => x.GeneralValueEur);

        entity.Ignore(x => x.GeneralValueUah);

        entity.Ignore(x => x.ManagementValueEur);

        entity.Ignore(x => x.ManagementValueUah);

        entity.Ignore(x => x.TotalNetPriceForServiceEur);

        entity.Ignore(x => x.TotalGeneralPriceForServiceEur);

        entity.Ignore(x => x.TotalManagementPriceForServiceEur);

        entity.Ignore(x => x.TotalNetPriceForServiceUah);

        entity.Ignore(x => x.TotalGeneralPriceForServiceUah);

        entity.Ignore(x => x.TotalManagementPriceForServiceUah);
    }
}