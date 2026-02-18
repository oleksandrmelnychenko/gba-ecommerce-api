using GBA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext;

public sealed class CategoryMap : EntityBaseMap<Category> {
    public override void Map(EntityTypeBuilder<Category> entity) {
        base.Map(entity);

        entity.ToTable("Category");

        entity.Property(e => e.RootCategoryId).HasColumnName("RootCategoryID");

        entity.HasOne(e => e.RootCategory)
            .WithMany(e => e.SubCategories)
            .HasForeignKey(e => e.RootCategoryId);
    }
}