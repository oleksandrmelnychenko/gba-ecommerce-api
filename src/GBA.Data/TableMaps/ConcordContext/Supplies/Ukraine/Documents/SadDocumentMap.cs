using GBA.Domain.Entities.Supplies.Ukraine.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine.Documents;

public sealed class SadDocumentMap : EntityBaseMap<SadDocument> {
    public override void Map(EntityTypeBuilder<SadDocument> entity) {
        base.Map(entity);

        entity.ToTable("SadDocument");

        entity.Property(e => e.SadId).HasColumnName("SadID");

        entity.Property(e => e.DocumentUrl).HasMaxLength(250);

        entity.Property(e => e.FileName).HasMaxLength(250);

        entity.Property(e => e.ContentType).HasMaxLength(250);

        entity.Property(e => e.GeneratedName).HasMaxLength(250);

        entity.HasOne(e => e.Sad)
            .WithMany(e => e.SadDocuments)
            .HasForeignKey(e => e.SadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}