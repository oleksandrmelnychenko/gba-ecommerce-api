using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class RetailClientPaymentImageItemMap : EntityBaseMap<RetailClientPaymentImageItem> {
    public override void Map(EntityTypeBuilder<RetailClientPaymentImageItem> entity) {
        base.Map(entity);

        entity.ToTable("RetailClientPaymentImageItem");

        entity.Property(e => e.ImgUrl).HasMaxLength(1000);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.Amount).HasColumnType("money");

        entity.Property(e => e.UserId).HasColumnName("UserID");

        entity.Property(e => e.RetailClientPaymentImageId).HasColumnName("RetailClientPaymentImageID");

        entity.HasOne(e => e.RetailClientPaymentImage)
            .WithMany(e => e.RetailClientPaymentImageItems)
            .HasForeignKey(e => e.RetailClientPaymentImageId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.User)
            .WithMany(e => e.RetailClientPaymentImageItems)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}