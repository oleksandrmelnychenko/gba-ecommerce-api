using GBA.Domain.Entities.Supplies.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Documents;

public sealed class BillOfLadingDocumentMap : EntityBaseMap<BillOfLadingDocument> {
    public override void Map(EntityTypeBuilder<BillOfLadingDocument> entity) {
        base.Map(entity);

        entity.ToTable("BillOfLadingDocument");

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.BillOfLadingServiceId).HasColumnName("BillOfLadingServiceID");

        entity.HasOne(x => x.BillOfLadingService)
            .WithMany(x => x.BillOfLadingDocuments)
            .HasForeignKey(e => e.BillOfLadingServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        ;
    }
}