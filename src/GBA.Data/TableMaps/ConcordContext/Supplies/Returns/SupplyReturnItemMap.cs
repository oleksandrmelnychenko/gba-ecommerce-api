using GBA.Domain.Entities.Supplies.Returns;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Returns;

public sealed class SupplyReturnItemMap : EntityBaseMap<SupplyReturnItem> {
    public override void Map(EntityTypeBuilder<SupplyReturnItem> entity) {
        base.Map(entity);

        entity.ToTable("SupplyReturnItem");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.Property(e => e.SupplyReturnId).HasColumnName("SupplyReturnID");

        entity.Property(e => e.ConsignmentItemId).HasColumnName("ConsignmentItemID");

        entity.Ignore(e => e.TotalNetPrice);

        entity.Ignore(e => e.TotalNetWeight);

        entity.HasOne(e => e.Product)
            .WithMany(e => e.SupplyReturnItems)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyReturn)
            .WithMany(e => e.SupplyReturnItems)
            .HasForeignKey(e => e.SupplyReturnId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ConsignmentItem)
            .WithMany(e => e.SupplyReturnItems)
            .HasForeignKey(e => e.ConsignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}