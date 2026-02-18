using GBA.Domain.Entities.AllegroServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.AllegroServices;

public sealed class AllegroCategoryMap : EntityBaseMap<AllegroCategory> {
    public override void Map(EntityTypeBuilder<AllegroCategory> entity) {
        base.Map(entity);

        entity.ToTable("AllegroCategory");

        entity.Property(e => e.CategoryId).HasColumnName("CategoryID");

        entity.Property(e => e.ParentCategoryId).HasColumnName("ParentCategoryID");

        entity.Ignore(e => e.SubCategories);

        entity.HasIndex(e => e.ParentCategoryId);

        entity.HasIndex(e => e.CategoryId);
    }
}