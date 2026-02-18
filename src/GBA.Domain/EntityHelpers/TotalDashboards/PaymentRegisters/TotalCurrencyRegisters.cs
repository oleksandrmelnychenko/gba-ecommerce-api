using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;

public sealed class TotalCurrencyRegisters {
    public List<CurrencyRegisterStateByPeriod> CurrencyRegisters { get; set; }

    public TotalValueByPeriod TotalValueEur { get; set; }
}