using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Entities.Consumables;

public sealed class ConsumablesOrderItem : EntityBase {
    public ConsumablesOrderItem() {
        DepreciatedConsumableOrderItems = new HashSet<DepreciatedConsumableOrderItem>();
    }

    public decimal TotalPrice { get; set; }

    public decimal TotalPriceWithVAT { get; set; }

    public decimal PricePerItem { get; set; }

    public decimal VAT { get; set; }

    public double VatPercent { get; set; }

    public double Qty { get; set; }

    public bool IsService { get; set; }

    public long ConsumableProductCategoryId { get; set; }

    public long ConsumablesOrderId { get; set; }

    public long? ConsumableProductId { get; set; }

    public long? ConsumableProductOrganizationId { get; set; }

    public long? SupplyOrganizationAgreementId { get; set; }

    public ConsumableProductCategory ConsumableProductCategory { get; set; }

    public ConsumablesOrder ConsumablesOrder { get; set; }

    public ConsumableProduct ConsumableProduct { get; set; }

    public SupplyOrganization ConsumableProductOrganization { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public PaymentCostMovementOperation PaymentCostMovementOperation { get; set; }

    public ICollection<DepreciatedConsumableOrderItem> DepreciatedConsumableOrderItems { get; set; }
}