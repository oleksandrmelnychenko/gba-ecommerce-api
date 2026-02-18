using GBA.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients;

public sealed class RetailPaymentStatusMap : EntityBaseMap<RetailPaymentStatus> {
    public override void Map(EntityTypeBuilder<RetailPaymentStatus> entity) {
        base.Map(entity);

        entity.ToTable("RetailPaymentStatus");

        entity.Ignore(e => e.AmountToPay);
    }
}