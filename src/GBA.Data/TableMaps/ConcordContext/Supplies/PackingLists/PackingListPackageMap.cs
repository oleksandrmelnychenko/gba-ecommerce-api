using GBA.Domain.Entities.Supplies.PackingLists;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.PackingLists;

public sealed class PackingListPackageMap : EntityBaseMap<PackingListPackage> {
    public override void Map(EntityTypeBuilder<PackingListPackage> entity) {
        base.Map(entity);

        entity.ToTable("PackingListPackage");

        entity.Property(e => e.PackingListId).HasColumnName("PackingListID");

        entity.HasOne(e => e.PackingList)
            .WithMany(e => e.PackingListPackages)
            .HasForeignKey(e => e.PackingListId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}