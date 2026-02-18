namespace GBA.Domain.Entities.PaymentOrders;

public sealed class AssignedPaymentOrder : EntityBase {
    public long? RootOutcomePaymentOrderId { get; set; }

    public long? RootIncomePaymentOrderId { get; set; }

    public long? AssignedOutcomePaymentOrderId { get; set; }

    public long? AssignedIncomePaymentOrderId { get; set; }

    public OutcomePaymentOrder RootOutcomePaymentOrder { get; set; }

    public IncomePaymentOrder RootIncomePaymentOrder { get; set; }

    public OutcomePaymentOrder AssignedOutcomePaymentOrder { get; set; }

    public IncomePaymentOrder AssignedIncomePaymentOrder { get; set; }
}