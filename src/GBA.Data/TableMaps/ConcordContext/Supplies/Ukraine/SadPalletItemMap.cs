using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SadPalletItemMap : EntityBaseMap<SadPalletItem> {
    public override void Map(EntityTypeBuilder<SadPalletItem> entity) {
        base.Map(entity);

        entity.ToTable("SadPalletItem");

        entity.Property(e => e.SadItemId).HasColumnName("SadItemID");

        entity.Property(e => e.SadPalletId).HasColumnName("SadPalletID");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.HasOne(e => e.SadItem)
            .WithMany(e => e.SadPalletItems)
            .HasForeignKey(e => e.SadItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SadPallet)
            .WithMany(e => e.SadPalletItems)
            .HasForeignKey(e => e.SadPalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}