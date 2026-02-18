using GBA.Data.TableMaps.ConcordContext;
using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordDataAnalytic.HistoryOrderItems;

public sealed class StockStateStorageMap : EntityBaseMap<StockStateStorage> {
    public override void Map(EntityTypeBuilder<StockStateStorage> entity) {
        base.Map(entity);

        entity.ToTable("StockStateStorage");

        entity.Property(e => e.QtyHistory).HasColumnName("QtyHistory");

        entity.Property(e => e.TotalReservedUK).HasColumnName("TotalReservedUK");

        entity.Property(e => e.TotalCartReservedUK).HasColumnName("TotalCartReservedUK");

        entity.Property(e => e.ChangeTypeOrderItem).HasColumnName("ChangeTypeOrderItem");

        entity.Property(e => e.ProductId).HasColumnName("ProductId");

        entity.Property(e => e.SaleId).HasColumnName("SaleId");

        entity.Property(e => e.SaleNumberId).HasColumnName("SaleNumberId");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasMany(s => s.ProductAvailabilityDataHistory)
            .WithOne(h => h.StockStateStorage)
            .HasForeignKey(h => h.StockStateStorageID)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Ignore(e => e.TotalRowQty);

        entity.Ignore(e => e.Product);

        entity.Ignore(e => e.Sale);

        entity.Ignore(e => e.User);

        entity.Ignore(e => e.SaleNumber);
    }
}