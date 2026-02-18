using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class TaxFreePackListOrderItemMap : EntityBaseMap<TaxFreePackListOrderItem> {
    public override void Map(EntityTypeBuilder<TaxFreePackListOrderItem> entity) {
        base.Map(entity);

        entity.ToTable("TaxFreePackListOrderItem");

        entity.Property(e => e.OrderItemId).HasColumnName("OrderItemID");

        entity.Property(e => e.TaxFreePackListId).HasColumnName("TaxFreePackListID");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.PackageSize);

        entity.Ignore(e => e.Coef);

        entity.Ignore(e => e.MaxQtyPerTF);

        entity.Ignore(e => e.UnitPriceLocal);

        entity.HasOne(e => e.OrderItem)
            .WithMany(e => e.TaxFreePackListOrderItems)
            .HasForeignKey(e => e.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFreePackList)
            .WithMany(e => e.TaxFreePackListOrderItems)
            .HasForeignKey(e => e.TaxFreePackListId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.TaxFreePackListOrderItems)
            .HasForeignKey(e => e.ConsignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}