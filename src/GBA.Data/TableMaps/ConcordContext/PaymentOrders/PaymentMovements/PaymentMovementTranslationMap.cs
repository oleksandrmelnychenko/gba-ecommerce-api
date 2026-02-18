using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders.PaymentMovements;

public sealed class PaymentMovementTranslationMap : EntityBaseMap<PaymentMovementTranslation> {
    public override void Map(EntityTypeBuilder<PaymentMovementTranslation> entity) {
        base.Map(entity);

        entity.ToTable("PaymentMovementTranslation");

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.Property(e => e.Name).HasMaxLength(150);

        entity.Property(e => e.PaymentMovementId).HasColumnName("PaymentMovementID");

        entity.HasOne(e => e.PaymentMovement)
            .WithMany(e => e.PaymentMovementTranslations)
            .HasForeignKey(e => e.PaymentMovementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}