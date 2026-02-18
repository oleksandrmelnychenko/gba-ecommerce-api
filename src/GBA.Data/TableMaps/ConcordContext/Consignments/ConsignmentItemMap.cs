using GBA.Domain.Entities.Consignments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consignments;

public sealed class ConsignmentItemMap : EntityBaseMap<ConsignmentItem> {
    public override void Map(EntityTypeBuilder<ConsignmentItem> entity) {
        base.Map(entity);

        entity.ToTable("ConsignmentItem");

        entity.Property(e => e.Price).HasColumnType("decimal(30,14)");

        entity.Property(e => e.AccountingPrice).HasColumnType("decimal(30,14)");

        entity.Property(e => e.NetPrice).HasColumnType("decimal(30,14)");

        entity.Property(e => e.DutyPercent).HasColumnType("money");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.ConsignmentId).HasColumnName("ConsignmentID");

        entity.Property(e => e.RootConsignmentItemId).HasColumnName("RootConsignmentItemID");

        entity.Property(e => e.ProductIncomeItemId).HasColumnName("ProductIncomeItemID");

        entity.Property(e => e.ProductSpecificationId).HasColumnName("ProductSpecificationID");

        entity.Property(e => e.ExchangeRate).HasColumnType("money");

        entity.Ignore(e => e.IsReSaleAvailability);

        entity.Ignore(e => e.ProductAvailability);

        entity.Ignore(e => e.TotalPrice);

        entity.Ignore(e => e.TotalPriceWithExtraCharge);

        entity.Ignore(e => e.QtyToReSale);

        entity.Ignore(e => e.FromStorage);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.ConsignmentItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Consignment)
            .WithMany(e => e.ConsignmentItems)
            .HasForeignKey(e => e.ConsignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.RootConsignmentItem)
            .WithMany(e => e.ChildConsignmentItems)
            .HasForeignKey(e => e.RootConsignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductIncomeItem)
            .WithMany(e => e.ConsignmentItems)
            .HasForeignKey(e => e.ProductIncomeItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ProductSpecification)
            .WithMany(e => e.ConsignmentItems)
            .HasForeignKey(e => e.ProductSpecificationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}