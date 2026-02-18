using GBA.Domain.Entities.Sales.Shipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales.Shipments;

public sealed class ShipmentListMap : EntityBaseMap<ShipmentList> {
    public override void Map(EntityTypeBuilder<ShipmentList> entity) {
        base.Map(entity);

        entity.ToTable("ShipmentList");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.TransporterId).HasColumnName("TransporterID");

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleShipmentLists)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Transporter)
            .WithMany(e => e.ShipmentLists)
            .HasForeignKey(e => e.TransporterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}