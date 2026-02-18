using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetAllPaymentRegisterTransfersByPaymentRegisterNetIdMessage {
    public GetAllPaymentRegisterTransfersByPaymentRegisterNetIdMessage(DateTime from, DateTime to, PaymentRegisterTransferType type, Guid? paymentRegisterNetId,
        Guid? currencyNetId) {
        From = from;

        To = to;

        Type = type;

        PaymentRegisterNetId = paymentRegisterNetId;

        CurrencyNetId = currencyNetId;
    }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public Guid? CurrencyNetId { get; set; }

    public Guid? PaymentRegisterNetId { get; set; }

    public PaymentRegisterTransferType Type { get; set; }
}