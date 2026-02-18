using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class MisplacedSaleMap : EntityBaseMap<MisplacedSale> {
    public override void Map(EntityTypeBuilder<MisplacedSale> entity) {
        base.Map(entity);

        entity.ToTable("MisplacedSale");

        entity.Property(e => e.SaleId).HasColumnName("SaleID");

        entity.Property(e => e.RetailClientId).HasColumnName("RetailClientID");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.HasOne(e => e.RetailClient)
            .WithMany(e => e.MisplacedSales)
            .HasForeignKey(e => e.RetailClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.MisplacedSales)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Sale)
            .WithOne(e => e.MisplacedSale)
            .HasForeignKey<Sale>(e => e.MisplacedSaleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Ignore(x => x.WithSales);
    }
}