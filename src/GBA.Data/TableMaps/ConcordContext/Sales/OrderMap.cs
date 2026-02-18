using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public class OrderMap : EntityBaseMap<Order> {
    public override void Map(EntityTypeBuilder<Order> entity) {
        base.Map(entity);

        entity.ToTable("Order");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.ClientShoppingCartId).HasColumnName("ClientShoppingCartID");

        entity.Ignore(e => e.TotalCount);

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.OverLordTotalAmount);

        entity.Ignore(e => e.OverLordTotalAmountLocal);

        entity.Ignore(e => e.Sale);

        entity.Ignore(e => e.TotalVat);

        entity.Ignore(e => e.TotalAmountEurToUah);

        entity.HasOne(e => e.User)
            .WithMany(e => e.Orders)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.Orders)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientShoppingCart)
            .WithMany(e => e.Orders)
            .HasForeignKey(e => e.ClientShoppingCartId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}