using GBA.Data.TableMaps.ConcordContext;
using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordDataAnalytic.HistoryOrderItems;

public sealed class ProductPlacementDataHistoryeMap : EntityBaseMap<ProductPlacementDataHistory> {
    public override void Map(EntityTypeBuilder<ProductPlacementDataHistory> entity) {
        base.Map(entity);

        entity.ToTable("ProductPlacementDataHistory");

        entity.Property(e => e.CellNumber).HasMaxLength(5);

        entity.Property(e => e.RowNumber).HasMaxLength(5);

        entity.Property(e => e.StorageNumber).HasMaxLength(5);

        entity.Property(e => e.VendorCode).HasColumnName("VendorCode");

        entity.Property(e => e.MainOriginalNumber).HasColumnName("MainOriginalNumber");

        entity.Property(e => e.NameUA).HasColumnName("NameUA");

        entity.Property(e => e.ConsignmentNumber).HasColumnName("ConsignmentNumber");

        entity.Property(e => e.Qty);

        entity.Property(e => e.ProductId).HasColumnName("ProductId");

        entity.Property(e => e.StorageId).HasColumnName("StorageId");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemId");

        entity.Ignore(e => e.Product);

        entity.Ignore(e => e.TotalRowQty);

        entity.Ignore(e => e.Storage);

        entity.Ignore(e => e.ConsignmentItem);
    }
}