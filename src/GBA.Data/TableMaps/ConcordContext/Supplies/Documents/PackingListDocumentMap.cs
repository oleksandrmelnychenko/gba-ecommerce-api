using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class PackingListDocumentMap : EntityBaseMap<PackingListDocument> {
    public override void Map(EntityTypeBuilder<PackingListDocument> entity) {
        base.Map(entity);

        entity.ToTable("PackingListDocument");

        entity.Property(e => e.SupplyOrderId).HasColumnName("SupplyOrderID");

        entity.HasOne(e => e.SupplyOrder)
            .WithMany(e => e.PackingListDocuments)
            .HasForeignKey(e => e.SupplyOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}