using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SadPalletMap : EntityBaseMap<SadPallet> {
    public override void Map(EntityTypeBuilder<SadPallet> entity) {
        base.Map(entity);

        entity.ToTable("SadPallet");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(250);

        entity.Property(e => e.SadId).HasColumnName("SadID");

        entity.Property(e => e.SadPalletTypeId).HasColumnName("SadPalletTypeID");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.HasOne(e => e.Sad)
            .WithMany(e => e.SadPallets)
            .HasForeignKey(e => e.SadId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SadPalletType)
            .WithMany(e => e.SadPallets)
            .HasForeignKey(e => e.SadPalletTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}