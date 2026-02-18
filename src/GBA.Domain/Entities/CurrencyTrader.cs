using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Entities;

public sealed class CurrencyTrader : EntityBase {
    public CurrencyTrader() {
        PaymentRegisterCurrencyExchanges = new HashSet<PaymentRegisterCurrencyExchange>();

        CurrencyTraderExchangeRates = new HashSet<CurrencyTraderExchangeRate>();
    }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string MiddleName { get; set; }

    public string PhoneNumber { get; set; }

    public ICollection<CurrencyTraderExchangeRate> CurrencyTraderExchangeRates { get; set; }

    public ICollection<PaymentRegisterCurrencyExchange> PaymentRegisterCurrencyExchanges { get; set; }
}