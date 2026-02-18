using GBA.Domain.Entities.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Products;

public sealed class ProductGroupMap : EntityBaseMap<ProductGroup> {
    public override void Map(EntityTypeBuilder<ProductGroup> entity) {
        base.Map(entity);

        entity.ToTable("ProductGroup");

        entity.Property(e => e.IsSubGroup).HasDefaultValueSql("0");

        entity.Property(e => e.SourceAmgId).HasColumnName("SourceAmgID").HasMaxLength(16);

        entity.Property(e => e.SourceFenixId).HasColumnName("SourceFenixID").HasMaxLength(16);

        entity.Ignore(x => x.TotalProductSubGroup);

        entity.Ignore(x => x.TotalProduct);

        entity.Ignore(x => x.RootProductGroup);
    }
}