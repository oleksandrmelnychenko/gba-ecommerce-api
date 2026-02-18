using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class ClientShoppingCartMap : EntityBaseMap<ClientShoppingCart> {
    public override void Map(EntityTypeBuilder<ClientShoppingCart> entity) {
        base.Map(entity);

        entity.ToTable("ClientShoppingCart");

        entity.Property(e => e.ClientAgreementId).HasColumnName("ClientAgreementID");

        entity.Property(e => e.OfferProcessingStatusChangedById).HasColumnName("OfferProcessingStatusChangedByID");

        entity.Property(e => e.CreatedById).HasColumnName("CreatedByID");

        entity.Property(e => e.WorkplaceId).HasColumnName("WorkplaceID");

        entity.Property(e => e.Number).HasMaxLength(50);

        entity.Property(e => e.Comment).HasMaxLength(450);

        entity.Property(e => e.IsOfferProcessed).HasDefaultValueSql("0");

        entity.Property(e => e.IsOffer).HasDefaultValueSql("0");

        entity.Ignore(e => e.TotalAmount);

        entity.Ignore(e => e.TotalLocalAmount);

        entity.HasOne(e => e.ClientAgreement)
            .WithMany(e => e.ClientShoppingCarts)
            .HasForeignKey(e => e.ClientAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.OfferProcessingStatusChangedBy)
            .WithMany(e => e.OffersProcessingStatusChanged)
            .HasForeignKey(e => e.OfferProcessingStatusChangedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.CreatedBy)
            .WithMany(e => e.ClientShoppingCarts)
            .HasForeignKey(e => e.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Workplace)
            .WithMany(e => e.ClientShoppingCarts)
            .HasForeignKey(e => e.WorkplaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}