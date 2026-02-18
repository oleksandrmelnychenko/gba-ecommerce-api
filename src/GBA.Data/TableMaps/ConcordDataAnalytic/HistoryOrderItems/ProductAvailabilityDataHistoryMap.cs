using GBA.Data.TableMaps.ConcordContext;
using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordDataAnalytic.HistoryOrderItems;

public sealed class ProductAvailabilityDataHistoryMap : EntityBaseMap<ProductAvailabilityDataHistory> {
    public override void Map(EntityTypeBuilder<ProductAvailabilityDataHistory> entity) {
        base.Map(entity);

        entity.ToTable("ProductAvailabilityDataHistory");

        entity.Property(e => e.StorageId).HasColumnName("StorageId");

        entity.Property(e => e.Amount).HasColumnName("Amount");


        entity.HasMany(s => s.ProductPlacementDataHistory)
            .WithOne(h => h.ProductAvailabilityDataHistory)
            .HasForeignKey(h => h.ProductAvailabilityDataHistoryID)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Ignore(e => e.Storage);
    }
}