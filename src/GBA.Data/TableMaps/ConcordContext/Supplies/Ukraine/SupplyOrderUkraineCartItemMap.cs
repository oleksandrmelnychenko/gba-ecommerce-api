using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemMap : EntityBaseMap<SupplyOrderUkraineCartItem> {
    public override void Map(EntityTypeBuilder<SupplyOrderUkraineCartItem> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderUkraineCartItem");

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.UnitPrice).HasColumnType("money");

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.UpdatedById).HasColumnName("UpdatedByID");

        entity.Property(e => e.TaxFreePackListId).HasColumnName("TaxFreePackListID");

        entity.Property(e => e.SupplierId).HasColumnName("SupplierID");

        entity.Property(e => e.PackingListPackageOrderItemId).HasColumnName("PackingListPackageOrderItemID");

        entity.Property(e => e.IsRecommended).HasDefaultValueSql("0");

        entity.Ignore(e => e.AvailableQty);

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.PackageSize);

        entity.Ignore(e => e.Coef);

        entity.Ignore(e => e.UnitPriceLocal);

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.CreatedSupplyOrderUkraineCartItems)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.SupplyOrderUkraineCartItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleSupplyOrderUkraineCartItems)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.UpdatedBy)
            .WithMany(e => e.UpdatedSupplyOrderUkraineCartItems)
            .HasForeignKey(e => e.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFreePackList)
            .WithMany(e => e.SupplyOrderUkraineCartItems)
            .HasForeignKey(e => e.TaxFreePackListId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Supplier)
            .WithMany(e => e.SupplyOrderUkraineCartItems)
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.PackingListPackageOrderItem)
            .WithMany(e => e.SupplyOrderUkraineCartItems)
            .HasForeignKey(e => e.PackingListPackageOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}