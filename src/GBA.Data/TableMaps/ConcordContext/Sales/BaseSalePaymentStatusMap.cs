using GBA.Domain.Entities.Sales.PaymentStatuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class BaseSalePaymentStatusMap : EntityBaseMap<BaseSalePaymentStatus> {
    public override void Map(EntityTypeBuilder<BaseSalePaymentStatus> entity) {
        base.Map(entity);

        entity.ToTable("BaseSalePaymentStatus");

        entity.Property(e => e.Amount).HasColumnType("money");
    }
}