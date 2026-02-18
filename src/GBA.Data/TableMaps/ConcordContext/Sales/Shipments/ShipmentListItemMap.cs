using GBA.Domain.Entities.Sales.Shipments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales.Shipments;

public sealed class ShipmentListItemMap : EntityBaseMap<ShipmentListItem> {
    public override void Map(EntityTypeBuilder<ShipmentListItem> entity) {
        base.Map(entity);

        entity.ToTable("ShipmentListItem");

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.IsChangeTransporter).HasColumnName("IsChangeTransporter");

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Property(e => e.ShipmentListId).HasColumnName("ShipmentListID");

        entity.HasOne(e => e.Sale)
            .WithMany(e => e.ShipmentListItems)
            .HasForeignKey(e => e.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ShipmentList)
            .WithMany(e => e.ShipmentListItems)
            .HasForeignKey(e => e.ShipmentListId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}