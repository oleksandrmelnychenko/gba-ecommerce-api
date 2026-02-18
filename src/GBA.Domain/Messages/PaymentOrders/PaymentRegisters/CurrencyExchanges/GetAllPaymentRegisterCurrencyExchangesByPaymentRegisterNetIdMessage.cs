using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetAllPaymentRegisterCurrencyExchangesByPaymentRegisterNetIdMessage {
    public GetAllPaymentRegisterCurrencyExchangesByPaymentRegisterNetIdMessage(
        DateTime from,
        DateTime to,
        Guid? paymentRegisterNetId,
        Guid? fromCurrencyNetId,
        Guid? toCurrencyNetId
    ) {
        PaymentRegisterNetId = paymentRegisterNetId;

        From = from;

        To = to;

        FromCurrencyNetId = fromCurrencyNetId;

        ToCurrencyNetId = toCurrencyNetId;
    }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public Guid? PaymentRegisterNetId { get; set; }

    public Guid? FromCurrencyNetId { get; set; }

    public Guid? ToCurrencyNetId { get; set; }
}