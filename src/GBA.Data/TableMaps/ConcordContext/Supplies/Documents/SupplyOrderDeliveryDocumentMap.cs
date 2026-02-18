using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class SupplyOrderDeliveryDocumentMap : EntityBaseMap<SupplyOrderDeliveryDocument> {
    public override void Map(EntityTypeBuilder<SupplyOrderDeliveryDocument> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderDeliveryDocument");

        entity.Property(e => e.IsReceived).HasDefaultValueSql("0");

        entity.Property(e => e.IsProcessed).HasDefaultValueSql("0");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.Property(e => e.SupplyDeliveryDocumentId).HasColumnName("SupplyDeliveryDocumentID");

        entity.HasOne(e => e.User)
            .WithMany(e => e.SupplyOrderDeliveryDocuments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.SupplyOrderDeliveryDocuments)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyDeliveryDocument)
            .WithMany(e => e.SupplyOrderDeliveryDocuments)
            .HasForeignKey(e => e.SupplyDeliveryDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}