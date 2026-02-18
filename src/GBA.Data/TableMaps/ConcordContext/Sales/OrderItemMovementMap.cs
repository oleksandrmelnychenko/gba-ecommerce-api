using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class OrderItemMovementMap : EntityBaseMap<OrderItemMovement> {
    public override void Map(EntityTypeBuilder<OrderItemMovement> entity) {
        base.Map(entity);

        entity.ToTable("OrderItemMovement");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.OrderItemMovements)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.User)
            .WithMany(e => e.OrderItemMovements)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}