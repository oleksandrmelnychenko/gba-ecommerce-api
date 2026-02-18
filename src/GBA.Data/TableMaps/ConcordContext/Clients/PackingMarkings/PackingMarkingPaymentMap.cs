using GBA.Domain.Entities.Clients.PackingMarkings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Clients.PackingMarkings;

public class PackingMarkingPaymentMap : EntityBaseMap<PackingMarkingPayment> {
    public override void Map(EntityTypeBuilder<PackingMarkingPayment> entity) {
        base.Map(entity);

        entity.ToTable("PackingMarkingPayment");
    }
}