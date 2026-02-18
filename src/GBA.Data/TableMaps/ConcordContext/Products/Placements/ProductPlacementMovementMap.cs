using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductPlacementMovementMap : EntityBaseMap<ProductPlacementMovement> {
    public override void Map(EntityTypeBuilder<ProductPlacementMovement> entity) {
        base.Map(entity);

        entity.ToTable("ProductPlacementMovement");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.FromProductPlacementId).HasColumnName("FromProductPlacementID");

        entity.Property(e => e.ToProductPlacementId).HasColumnName("ToProductPlacementID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.HasOne(e => e.FromProductPlacement)
            .WithMany(e => e.FromProductPlacementMovements)
            .HasForeignKey(e => e.FromProductPlacementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ToProductPlacement)
            .WithMany(e => e.ToProductPlacementMovements)
            .HasForeignKey(e => e.ToProductPlacementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleProductPlacementMovements)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}