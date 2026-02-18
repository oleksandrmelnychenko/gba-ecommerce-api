using GBA.Domain.Entities.Supplies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies;

public sealed class SupplyInvoiceOrderItemMap : EntityBaseMap<SupplyInvoiceOrderItem> {
    public override void Map(EntityTypeBuilder<SupplyInvoiceOrderItem> entity) {
        base.Map(entity);

        entity.ToTable("SupplyInvoiceOrderItem");

        entity.Property(e => e.UnitPrice).HasColumnType("money");

        entity.Property(e => e.SupplyInvoiceId).HasColumnName("SupplyInvoiceID");

        entity.Property(e => e.SupplyOrderItemId).HasColumnName("SupplyOrderItemID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Ignore(e => e.Weight);

        entity.Ignore(e => e.GrossUnitPrice);

        entity.Ignore(e => e.ProductSpecification);

        entity.Ignore(e => e.PlProductSpecification);

        entity.HasOne(e => e.SupplyInvoice)
            .WithMany(e => e.SupplyInvoiceOrderItems)
            .HasForeignKey(e => e.SupplyInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderItem)
            .WithMany(e => e.SupplyInvoiceOrderItems)
            .HasForeignKey(e => e.SupplyOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.SupplyInvoiceOrderItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}