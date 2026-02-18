using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductPlacementHistoryMap : EntityBaseMap<ProductPlacementHistory> {
    public override void Map(EntityTypeBuilder<ProductPlacementHistory> entity) {
        base.Map(entity);

        entity.ToTable("ProductPlacementHistory");

        entity.Property(e => e.Qty).HasColumnName("Qty");

        entity.Property(e => e.Placement).HasMaxLength(500);

        entity.Property(e => e.ProductId).HasColumnName("ProductId");

        entity.Property(e => e.StorageId).HasColumnName("StorageId");

        entity.Property(e => e.UserId).HasColumnName("UserId");

        entity.Ignore(e => e.TotalRowsQty);
    }
}