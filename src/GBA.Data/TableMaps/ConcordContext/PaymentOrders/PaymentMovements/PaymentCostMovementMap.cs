using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders.PaymentMovements;

public sealed class PaymentCostMovementMap : EntityBaseMap<PaymentCostMovement> {
    public override void Map(EntityTypeBuilder<PaymentCostMovement> entity) {
        base.Map(entity);

        entity.ToTable("PaymentCostMovement");

        entity.Property(e => e.OperationName).HasMaxLength(150);
    }
}