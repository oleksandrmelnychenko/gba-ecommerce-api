using GBA.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GBA.Data.TableMaps.ConcordContext.Sales;

public sealed class SaleInvoiceDocumentMap : EntityBaseMap<SaleInvoiceDocument> {
    public override void Map(EntityTypeBuilder<SaleInvoiceDocument> entity) {
        base.Map(entity);

        entity.ToTable("SaleInvoiceDocument");

        entity.Property(e => e.Vat).HasColumnType("money");

        entity.Property(e => e.ShippingAmount).HasColumnType("money");

        entity.Property(e => e.ShippingAmountWithoutVat).HasColumnType("money");

        entity.Property(e => e.ShippingAmountEur).HasColumnType("money");

        entity.Property(e => e.ShippingAmountEurWithoutVat).HasColumnType("money");

        entity.Property(e => e.ExchangeRateAmount).HasColumnType("money");
    }
}