using GBA.Domain.Entities.Sales.SaleMerges;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class SaleMergedMap : EntityBaseMap<SaleMerged> {
    public override void Map(EntityTypeBuilder<SaleMerged> entity) {
        base.Map(entity);

        entity.ToTable("SaleMerged");

        entity.Property(e => e.InputSaleId).HasColumnName("InputSaleID");

        entity.Property(e => e.OutputSaleId).HasColumnName("OutputSaleID");

        entity.HasOne(e => e.InputSale)
            .WithMany(e => e.InputSaleMerges)
            .HasForeignKey(e => e.InputSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OutputSale)
            .WithMany(e => e.OutputSaleMerges)
            .HasForeignKey(e => e.OutputSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => new { e.OutputSaleId, e.Deleted });
    }
}