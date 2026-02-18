using GBA.Domain.Entities.Sales.OrderPackages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales.OrderPackages;

public sealed class OrderPackageUserMap : EntityBaseMap<OrderPackageUser> {
    public override void Map(EntityTypeBuilder<OrderPackageUser> entity) {
        base.Map(entity);

        entity.ToTable("OrderPackageUser");

        entity.Property(e => e.OrderPackageId).HasColumnName("OrderPackageID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(e => e.OrderPackage)
            .WithMany(e => e.OrderPackageUsers)
            .HasForeignKey(e => e.OrderPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.OrderPackageUsers)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}