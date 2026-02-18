using GBA.Domain.TranslationEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders.PaymentMovements;

public sealed class PaymentCostMovementTranslationMap : EntityBaseMap<PaymentCostMovementTranslation> {
    public override void Map(EntityTypeBuilder<PaymentCostMovementTranslation> entity) {
        base.Map(entity);

        entity.ToTable("PaymentCostMovementTranslation");

        entity.Property(e => e.CultureCode).HasMaxLength(4);

        entity.Property(e => e.OperationName).HasMaxLength(150);

        entity.Property(e => e.PaymentCostMovementId).HasColumnName("PaymentCostMovementID");

        entity.HasOne(e => e.PaymentCostMovement)
            .WithMany(e => e.PaymentCostMovementTranslations)
            .HasForeignKey(e => e.PaymentCostMovementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}