using GBA.Domain.Entities.Transporters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Transporters;

public sealed class TransporterMap : EntityBaseMap<Transporter> {
    public override void Map(EntityTypeBuilder<Transporter> entity) {
        base.Map(entity);

        entity.ToTable("Transporter");

        entity.Property(e => e.TransporterTypeId).HasColumnName("TransporterTypeID");

        entity.Property(e => e.ImageUrl).HasColumnName("ImageUrl");

        entity.HasOne(e => e.TransporterType)
            .WithMany(e => e.Transporters)
            .HasForeignKey(e => e.TransporterTypeId);
    }
}