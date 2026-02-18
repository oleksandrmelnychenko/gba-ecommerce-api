using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.PaymentOrders.PaymentMovements;

public sealed class PaymentMovementMap : EntityBaseMap<PaymentMovement> {
    public override void Map(EntityTypeBuilder<PaymentMovement> entity) {
        base.Map(entity);

        entity.ToTable("PaymentMovement");

        entity.Property(e => e.OperationName).HasMaxLength(150);
    }
}