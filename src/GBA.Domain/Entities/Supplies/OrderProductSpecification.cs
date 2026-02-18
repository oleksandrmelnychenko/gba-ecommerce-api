using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies;

public sealed class OrderProductSpecification : EntityBase {
    public double Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public long? SupplyInvoiceId { get; set; }

    public long? SadId { get; set; }

    public long ProductSpecificationId { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public Sad Sad { get; set; }

    public ProductSpecification ProductSpecification { get; set; }
}