using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public class OrderItemMap : EntityBaseMap<OrderItem> {
    public override void Map(EntityTypeBuilder<OrderItem> entity) {
        base.Map(entity);

        entity.ToTable("OrderItem");

        entity.Property(e => e.IsValidForCurrentSale).HasDefaultValueSql("1");

        entity.Property(e => e.IsFromOffer).HasDefaultValueSql("0");

        entity.Property(e => e.IsFromReSale).HasDefaultValueSql("0");

        entity.Property(e => e.IsFromShiftedItem).HasDefaultValueSql("0");

        entity.Property(e => e.PricePerItem).HasColumnType("decimal(30,14)");

        entity.Property(e => e.Vat).HasColumnType("decimal(30,14)");

        entity.Property(e => e.PricePerItemWithoutVat).HasColumnType("decimal(30,14)");

        entity.Property(e => e.ExchangeRateAmount).HasColumnType("money");

        entity.Property(e => e.OneTimeDiscount).HasColumnType("money");

        entity.Property(e => e.DiscountAmount).HasColumnType("money");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.OrderId).HasColumnName("OrderID");

        entity.Property(e => e.ClientShoppingCartId).HasColumnName("ClientShoppingCartID");

        entity.Property(e => e.OfferProcessingStatusChangedById).HasColumnName("OfferProcessingStatusChangedByID");

        entity.Property(e => e.DiscountUpdatedById).HasColumnName("DiscountUpdatedByID");

        entity.Property(e => e.AssignedSpecificationId).HasColumnName("AssignedSpecificationID");

        entity.Property(e => e.OneTimeDiscountComment).HasMaxLength(450);

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Property(e => e.OverLordQty);

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountEurToUah);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.OverLordTotalAmountLocal);

        entity.Ignore(e => e.OverLordTotalAmount);

        entity.Ignore(e => e.Discount);

        entity.Ignore(e => e.ChangedQty);

        entity.Ignore(e => e.TotalWeight);

        entity.Ignore(e => e.ProductSpecification);

        entity.Ignore(e => e.UkProductSpecification);

        entity.Ignore(e => e.TotalVat);

        entity.Ignore(e => e.IsMisplacedItem);

        entity.Ignore(e => e.OrderItemsGroupByProduct);

        entity.HasOne(e => e.Order)
            .WithMany(e => e.OrderItems)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.OrderItems)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.DiscountUpdatedBy)
            .WithMany(e => e.UpdatedOrderItemOneTimeDiscounts)
            .HasForeignKey(e => e.DiscountUpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OfferProcessingStatusChangedBy)
            .WithMany(e => e.OfferItemsProcessingStatusChanged)
            .HasForeignKey(e => e.OfferProcessingStatusChangedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientShoppingCart)
            .WithMany(e => e.OrderItems)
            .HasForeignKey(e => e.ClientShoppingCartId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.AssignedSpecification)
            .WithMany(e => e.OrderItems)
            .HasForeignKey(e => e.AssignedSpecificationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.MisplacedSale)
            .WithMany(e => e.OrderItems)
            .HasForeignKey(e => e.MisplacedSaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}