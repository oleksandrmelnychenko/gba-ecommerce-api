using GBA.Domain.Entities.Products.Transfers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products.Transfers;

public sealed class ProductTransferItemMap : EntityBaseMap<ProductTransferItem> {
    public override void Map(EntityTypeBuilder<ProductTransferItem> entity) {
        base.Map(entity);

        entity.ToTable("ProductTransferItem");

        entity.Property(e => e.Reason).HasMaxLength(150);

        entity.Property(e => e.ActReconciliationItemId).HasColumnName("ActReconciliationItemID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ProductTransferId).HasColumnName("ProductTransferID");

        entity.Ignore(e => e.ProductAvailability);

        entity.HasOne(e => e.ActReconciliationItem)
            .WithMany(e => e.ProductTransferItems)
            .HasForeignKey(e => e.ActReconciliationItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ProductTransferItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductTransfer)
            .WithMany(e => e.ProductTransferItems)
            .HasForeignKey(e => e.ProductTransferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}