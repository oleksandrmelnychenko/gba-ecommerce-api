using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class ConsumableProductMap : EntityBaseMap<ConsumableProduct> {
    public override void Map(EntityTypeBuilder<ConsumableProduct> entity) {
        base.Map(entity);

        entity.ToTable("ConsumableProduct");

        entity.Property(e => e.ConsumableProductCategoryId).HasColumnName("ConsumableProductCategoryID");

        entity.Property(e => e.MeasureUnitId).HasColumnName("MeasureUnitID");

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.VendorCode).HasMaxLength(3);

        entity.Ignore(e => e.TotalQty);

        entity.Ignore(e => e.PriceTotals);

        entity.HasOne(e => e.ConsumableProductCategory)
            .WithMany(e => e.ConsumableProducts)
            .HasForeignKey(e => e.ConsumableProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.MeasureUnit)
            .WithMany(e => e.ConsumableProducts)
            .HasForeignKey(e => e.MeasureUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}