using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;

public sealed class CurrencyRegisterStateByPeriod {
    public PaymentCurrencyRegister PaymentCurrencyRegister { get; set; }

    public TotalValueByPeriod TotalValue { get; set; }

    public TotalValueByPeriod TotalValueEur { get; set; }
}