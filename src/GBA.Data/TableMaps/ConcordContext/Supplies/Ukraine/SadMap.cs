using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class SadMap : EntityBaseMap<Sad> {
    public override void Map(EntityTypeBuilder<Sad> entity) {
        base.Map(entity);

        entity.ToTable("Sad");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.MarginAmount).HasColumnType("money");

        entity.Property(e => e.VatPercent).HasColumnType("money");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.StathamCarId).HasColumnName("StathamCarID");

        entity.Property(e => e.StathamPassportId).HasColumnName("StathamPassportID");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.StathamId).HasColumnName("StathamID");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.Property(e => e.OrganizationClientId).HasColumnName("OrganizationClientID");

        entity.Property(e => e.OrganizationClientAgreementId).HasColumnName("OrganizationClientAgreementID");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Ignore(e => e.TotalQty);

        entity.Ignore(e => e.TotalNetWeight);

        entity.Ignore(e => e.TotalGrossWeight);

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.TotalAmountWithMargin);

        entity.Ignore(e => e.SadCoefficient);

        entity.Ignore(e => e.TotalVatAmount);

        entity.Ignore(e => e.TotalVatAmountWithMargin);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleSads)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.StathamCar)
            .WithMany(e => e.Sads)
            .HasForeignKey(e => e.StathamCarId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.Sads)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Statham)
            .WithMany(e => e.Sads)
            .HasForeignKey(e => e.StathamId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraine)
            .WithOne(e => e.Sad)
            .HasForeignKey<Sad>(e => e.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrganizationClient)
            .WithMany(e => e.Sads)
            .HasForeignKey(e => e.OrganizationClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OrganizationClientAgreement)
            .WithMany(e => e.Sads)
            .HasForeignKey(e => e.OrganizationClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.Sads)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.Sads)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.StathamPassport)
            .WithMany(e => e.Sads)
            .HasForeignKey(e => e.StathamPassportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}