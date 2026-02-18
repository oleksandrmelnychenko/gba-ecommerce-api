using GBA.Domain.Entities.ReSales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.ReSales;

public sealed class ReSaleItemMap : EntityBaseMap<ReSaleItem> {
    public override void Map(EntityTypeBuilder<ReSaleItem> entity) {
        base.Map(entity);

        entity.ToTable("ReSaleItem");

        entity.Property(e => e.ReSaleId).HasColumnName("ReSaleID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ReSaleAvailabilityId).HasColumnName("ReSaleAvailabilityID");

        entity.Property(e => e.PricePerItem).HasColumnType("decimal(30,14)");

        entity.Property(e => e.ExchangeRate).HasColumnType("money");

        entity.Ignore(e => e.TotalPrice);

        entity.Ignore(e => e.RemainingQty);

        entity.Ignore(e => e.Weight);

        entity.HasOne(e => e.ReSale)
            .WithMany(e => e.ReSaleItems)
            .HasForeignKey(e => e.ReSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ReSaleAvailability)
            .WithMany(e => e.ReSaleItems)
            .HasForeignKey(e => e.ReSaleAvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ReSaleItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}