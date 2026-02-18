using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class SaleInvoiceNumberMap : EntityBaseMap<SaleInvoiceNumber> {
    public override void Map(EntityTypeBuilder<SaleInvoiceNumber> entity) {
        base.Map(entity);

        entity.ToTable("SaleInvoiceNumber");

        entity.Property(e => e.Number).HasMaxLength(50);
    }
}