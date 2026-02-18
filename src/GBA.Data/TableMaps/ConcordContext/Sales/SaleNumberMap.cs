using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class SaleNumberMap : EntityBaseMap<SaleNumber> {
    public override void Map(EntityTypeBuilder<SaleNumber> entity) {
        base.Map(entity);

        entity.ToTable("SaleNumber");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.SaleNumbers)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}