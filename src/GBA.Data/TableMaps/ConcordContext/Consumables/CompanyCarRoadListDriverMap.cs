using GBA.Domain.Entities.Consumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Consumables;

public sealed class CompanyCarRoadListDriverMap : EntityBaseMap<CompanyCarRoadListDriver> {
    public override void Map(EntityTypeBuilder<CompanyCarRoadListDriver> entity) {
        base.Map(entity);

        entity.ToTable("CompanyCarRoadListDriver");

        entity.Property(e => e.CompanyCarRoadListId).HasColumnName("CompanyCarRoadListID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(e => e.CompanyCarRoadList)
            .WithMany(e => e.CompanyCarRoadListDrivers)
            .HasForeignKey(e => e.CompanyCarRoadListId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.CompanyCarRoadListDrivers)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}