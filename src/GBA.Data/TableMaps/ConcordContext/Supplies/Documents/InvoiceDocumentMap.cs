using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class InvoiceDocumentMap : EntityBaseMap<InvoiceDocument> {
    public override void Map(EntityTypeBuilder<InvoiceDocument> entity) {
        base.Map(entity);

        entity.ToTable("InvoiceDocument");

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");

        entity.Property(e => e.PortWorkServiceId).HasColumnName("PortWorkServiceID");

        entity.Property(e => e.TransportationServiceId).HasColumnName("TransportationServiceID");

        entity.Property(e => e.ContainerServiceId).HasColumnName("ContainerServiceID");

        entity.Property(e => e.CustomServiceId).HasColumnName("CustomServiceID");

        entity.Property(e => e.VehicleDeliveryServiceId).HasColumnName("VehicleDeliveryServiceID");

        entity.Property(e => e.CustomAgencyServiceId).HasColumnName("CustomAgencyServiceID");

        entity.Property(e => e.PlaneDeliveryServiceId).HasColumnName("PlaneDeliveryServiceID");

        entity.Property(e => e.PortCustomAgencyServiceId).HasColumnName("PortCustomAgencyServiceID");

        entity.Property(e => e.SupplyOrderPolandPaymentDeliveryProtocolId).HasColumnName("SupplyOrderPolandPaymentDeliveryProtocolID");

        entity.Property(e => e.PackingListId).HasColumnName("PackingListID");

        entity.Property(e => e.MergedServiceId).HasColumnName("MergedServiceID");

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PortWorkService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.PortWorkServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TransportationService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.TransportationServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ContainerService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.ContainerServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.CustomServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.CustomServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CustomAgencyService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.CustomAgencyServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PlaneDeliveryService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.PlaneDeliveryServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PortCustomAgencyService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.PortCustomAgencyServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderPolandPaymentDeliveryProtocol)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.SupplyOrderPolandPaymentDeliveryProtocolId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PackingList)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.PackingListId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.MergedService)
            .WithMany(e => e.InvoiceDocuments)
            .HasForeignKey(e => e.MergedServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}