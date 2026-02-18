using GBA.Domain.Entities.Supplies.Ukraine;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Supplies.Ukraine;

public sealed class TaxFreePackListMap : EntityBaseMap<TaxFreePackList> {
    public override void Map(EntityTypeBuilder<TaxFreePackList> entity) {
        base.Map(entity);

        entity.ToTable("TaxFreePackList");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(500);

        entity.Property(e => e.MarginAmount).HasColumnType("money");

        entity.Property(e => e.OrganizationId).HasColumnName("OrganizationID");

        entity.Property(e => e.ClientId).HasColumnName("ClientID");

        entity.Property(e => e.ResponsibleId).HasColumnName("ResponsibleID");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.SupplyOrderUkraineId).HasColumnName("SupplyOrderUkraineID");

        entity.Ignore(e => e.TaxFreesCount);

        entity.Ignore(e => e.TotalUnspecifiedWeight);

        entity.Ignore(e => e.TotalUnspecifiedAmount);

        entity.Ignore(e => e.TotalUnspecifiedAmountLocal);

        entity.Ignore(e => e.TotalWeight);

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalAmountLocal);

        entity.Ignore(e => e.TotalVatAmountLocal);

        entity.Ignore(e => e.Status);

        entity.HasOne(e => e.Organization)
            .WithMany(e => e.TaxFreePackLists)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Client)
            .WithMany(e => e.TaxFreePackLists)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Responsible)
            .WithMany(e => e.ResponsibleTaxFreePackLists)
            .HasForeignKey(e => e.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.TaxFreePackLists)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.SupplyOrderUkraine)
            .WithOne(e => e.TaxFreePackList)
            .HasForeignKey<TaxFreePackList>(e => e.SupplyOrderUkraineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}