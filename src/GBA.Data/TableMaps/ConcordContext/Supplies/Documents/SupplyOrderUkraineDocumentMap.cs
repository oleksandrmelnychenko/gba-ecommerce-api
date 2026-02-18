using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class SupplyOrderUkraineDocumentMap : EntityBaseMap<SupplyOrderUkraineDocument> {
    public override void Map(EntityTypeBuilder<SupplyOrderUkraineDocument> entity) {
        base.Map(entity);

        entity.ToTable("SupplyOrderUkraineDocument");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.Property(e => e.DocumentUrl).HasMaxLength(500);

        entity.Property(e => e.FileName).HasMaxLength(500);

        entity.Property(e => e.ContentType).HasMaxLength(500);

        entity.Property(e => e.GeneratedName).HasMaxLength(500);

        entity.HasOne(x => x.SupplyOrderUkraine)
            .WithMany(x => x.SupplyOrderUkraineDocuments)
            .HasForeignKey(x => x.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}