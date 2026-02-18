using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class TaxFreeItemMap : EntityBaseMap<TaxFreeItem> {
    public override void Map(EntityTypeBuilder<TaxFreeItem> entity) {
        base.Map(entity);

        entity.ToTable("TaxFreeItem");

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.SupplyOrderUkraineCartItemId).HasColumnName("SupplyOrderUkraineCartItemID");

        entity.Property(e => e.TaxFreePackListOrderItemId).HasColumnName("TaxFreePackListOrderItemID");

        entity.Property(e => e.TaxFreeId).HasColumnName("TaxFreeID");

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.UnitPriceWithVat);

        entity.Ignore(e => e.TotalWithVat);

        entity.Ignore(e => e.VatAmountPl);

        entity.Ignore(e => e.TotalWithVatPl);

        entity.Ignore(e => e.UnitPricePL);

        entity.Ignore(e => e.TotalWithoutVatPl);

        entity.HasOne(e => e.SupplyOrderUkraineCartItem)
            .WithMany(e => e.TaxFreeItems)
            .HasForeignKey(e => e.SupplyOrderUkraineCartItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFree)
            .WithMany(e => e.TaxFreeItems)
            .HasForeignKey(e => e.TaxFreeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFreePackListOrderItem)
            .WithMany(e => e.TaxFreeItems)
            .HasForeignKey(e => e.TaxFreePackListOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}