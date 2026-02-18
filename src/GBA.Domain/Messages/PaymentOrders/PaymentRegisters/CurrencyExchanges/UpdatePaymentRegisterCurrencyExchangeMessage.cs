using System;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class UpdatePaymentRegisterCurrencyExchangeMessage {
    public UpdatePaymentRegisterCurrencyExchangeMessage(PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange, Guid userNetId) {
        PaymentRegisterCurrencyExchange = paymentRegisterCurrencyExchange;

        UserNetId = userNetId;
    }

    public PaymentRegisterCurrencyExchange PaymentRegisterCurrencyExchange { get; set; }

    public Guid UserNetId { get; set; }
}