using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductSubGroupMap : EntityBaseMap<ProductSubGroup> {
    public override void Map(EntityTypeBuilder<ProductSubGroup> entity) {
        base.Map(entity);

        entity.ToTable("ProductSubGroup");

        entity.Property(e => e.RootProductGroupId).HasColumnName("RootProductGroupID");

        entity.Property(e => e.SubProductGroupId).HasColumnName("SubProductGroupID");

        entity.HasOne(e => e.RootProductGroup)
            .WithMany(e => e.RootProductGroups)
            .HasForeignKey(e => e.RootProductGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SubProductGroup)
            .WithMany(e => e.SubProductGroups)
            .HasForeignKey(e => e.SubProductGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}