using GBA.Domain.Entities.Sales.OrderPackages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales.OrderPackages;

public sealed class OrderPackageMap : EntityBaseMap<OrderPackage> {
    public override void Map(EntityTypeBuilder<OrderPackage> entity) {
        base.Map(entity);

        entity.ToTable("OrderPackage");

        entity.Property(e => e.OrderId).HasColumnName("OrderID");

        entity.HasOne(e => e.Order)
            .WithMany(e => e.OrderPackages)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}