using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Entities.Consumables;

public sealed class CompanyCarFueling : EntityBase {
    public double FuelAmount { get; set; }

    public double VatPercent { get; set; }

    public decimal PricePerLiter { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal TotalPriceWithVat { get; set; }

    public decimal VatAmount { get; set; }

    public long CompanyCarId { get; set; }

    public long OutcomePaymentOrderId { get; set; }

    public long ConsumableProductOrganizationId { get; set; }

    public long? SupplyOrganizationAgreementId { get; set; }

    public long UserId { get; set; }

    public CompanyCar CompanyCar { get; set; }

    public OutcomePaymentOrder OutcomePaymentOrder { get; set; }

    public SupplyOrganization ConsumableProductOrganization { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public PaymentCostMovementOperation PaymentCostMovementOperation { get; set; }

    public User User { get; set; }
}