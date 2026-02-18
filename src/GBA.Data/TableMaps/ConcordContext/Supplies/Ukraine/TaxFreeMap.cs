using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class TaxFreeMap : EntityBaseMap<TaxFree> {
    public override void Map(EntityTypeBuilder<TaxFree> entity) {
        base.Map(entity);

        entity.ToTable("TaxFree");

        entity.Property(e => e.StathamId).HasColumnName("StathamID");

        entity.Property(e => e.TaxFreePackListId).HasColumnName("TaxFreePackListID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.StathamCarId).HasColumnName("StathamCarID");

        entity.Property(e => e.StathamPassportId).HasColumnName("StathamPassportID");

        entity.Property(e => e.AmountPayedStatham).HasColumnType("money");

        entity.Property(e => e.AmountInPLN).HasColumnType("money");

        entity.Property(e => e.VatAmountInPLN).HasColumnType("money");

        entity.Property(e => e.AmountInEur).HasColumnType("money");

        entity.Property(e => e.MarginAmount).HasColumnType("money");

        entity.Property(e => e.VatPercent).HasColumnType("money");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.CustomCode).HasMaxLength(150);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.UnitPriceWithVat);

        entity.Ignore(e => e.TotalWithVat);

        entity.Ignore(e => e.VatAmountPl);

        entity.Ignore(e => e.TotalWithVatPl);

        entity.HasOne(e => e.Statham)
            .WithMany(e => e.TaxFrees)
            .HasForeignKey(e => e.StathamId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.StathamCar)
            .WithMany(e => e.TaxFrees)
            .HasForeignKey(e => e.StathamCarId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.TaxFreePackList)
            .WithMany(e => e.TaxFrees)
            .HasForeignKey(e => e.TaxFreePackListId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.StathamPassport)
            .WithMany(e => e.TaxFrees)
            .HasForeignKey(e => e.StathamPassportId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleTaxFrees)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}