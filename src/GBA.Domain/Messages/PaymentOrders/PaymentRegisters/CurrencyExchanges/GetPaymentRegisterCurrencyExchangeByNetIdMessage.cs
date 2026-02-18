using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetPaymentRegisterCurrencyExchangeByNetIdMessage {
    public GetPaymentRegisterCurrencyExchangeByNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; set; }
}