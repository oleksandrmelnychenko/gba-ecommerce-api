using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class CarBrandMap : EntityBaseMap<CarBrand> {
    public override void Map(EntityTypeBuilder<CarBrand> entity) {
        base.Map(entity);

        entity.ToTable("CarBrand");

        entity.Property(e => e.Name).HasMaxLength(100);

        entity.Property(e => e.Description).HasMaxLength(250);

        entity.Property(e => e.ImageUrl).HasMaxLength(100);
    }
}