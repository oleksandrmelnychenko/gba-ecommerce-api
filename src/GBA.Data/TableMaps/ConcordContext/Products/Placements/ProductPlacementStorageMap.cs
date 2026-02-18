using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductPlacementStorageMap : EntityBaseMap<ProductPlacementStorage> {
    public override void Map(EntityTypeBuilder<ProductPlacementStorage> entity) {
        base.Map(entity);

        entity.ToTable("ProductPlacementStorage");

        entity.Property(e => e.Qty).HasColumnName("Qty");

        entity.Property(e => e.VendorCode).HasColumnName("VendorCode");

        entity.Property(e => e.Placement).HasMaxLength(500);

        entity.Property(e => e.ProductPlacementId).HasColumnName("ProductPlacementId");

        entity.Property(e => e.ProductId).HasColumnName("ProductId");

        entity.Property(e => e.StorageId).HasColumnName("StorageId");

        entity.Ignore(e => e.ErrorMessage);

        entity.Ignore(e => e.TotalRowsQty);
    }
}