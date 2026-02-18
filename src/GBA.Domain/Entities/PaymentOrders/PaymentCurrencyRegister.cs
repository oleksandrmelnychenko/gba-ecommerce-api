using System.Collections.Generic;

namespace GBA.Domain.Entities.PaymentOrders;

public sealed class PaymentCurrencyRegister : EntityBase {
    public PaymentCurrencyRegister() {
        PaymentRegisterTransfers = new HashSet<PaymentRegisterTransfer>();

        FromPaymentRegisterTransfers = new HashSet<PaymentRegisterTransfer>();

        ToPaymentRegisterTransfers = new HashSet<PaymentRegisterTransfer>();

        PaymentRegisterCurrencyExchanges = new HashSet<PaymentRegisterCurrencyExchange>();

        FromPaymentRegisterCurrencyExchanges = new HashSet<PaymentRegisterCurrencyExchange>();

        ToPaymentRegisterCurrencyExchanges = new HashSet<PaymentRegisterCurrencyExchange>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();
    }

    public decimal Amount { get; set; }

    public decimal InitialAmount { get; set; }

    public decimal BeforeRangeTotal { get; set; }

    public decimal RangeTotal { get; set; }

    public long PaymentRegisterId { get; set; }

    public long CurrencyId { get; set; }

    public PaymentRegister PaymentRegister { get; set; }

    public Currency Currency { get; set; }

    public ICollection<PaymentRegisterTransfer> PaymentRegisterTransfers { get; set; }

    public ICollection<PaymentRegisterTransfer> FromPaymentRegisterTransfers { get; set; }

    public ICollection<PaymentRegisterTransfer> ToPaymentRegisterTransfers { get; set; }

    public ICollection<PaymentRegisterCurrencyExchange> PaymentRegisterCurrencyExchanges { get; set; }

    public ICollection<PaymentRegisterCurrencyExchange> FromPaymentRegisterCurrencyExchanges { get; set; }

    public ICollection<PaymentRegisterCurrencyExchange> ToPaymentRegisterCurrencyExchanges { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }
}