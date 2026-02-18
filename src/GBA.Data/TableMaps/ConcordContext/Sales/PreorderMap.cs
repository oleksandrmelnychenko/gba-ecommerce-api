using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class PreOrderMap : EntityBaseMap<PreOrder> {
    public override void Map(EntityTypeBuilder<PreOrder> entity) {
        base.Map(entity);

        entity.ToTable("PreOrder");

        entity.Property(e => e.Culture).HasMaxLength(4);

        entity.Property(e => e.MobileNumber).HasMaxLength(25);

        entity.Property(e => e.Comment).HasMaxLength(250);

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ProductId).HasColumnName("ProductID");

        entity.HasOne(e => e.Product)
            .WithMany(e => e.PreOrders)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.PreOrders)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}