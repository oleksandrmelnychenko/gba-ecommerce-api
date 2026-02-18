using System;

namespace GBA.Domain.Messages.PaymentOrders.PaymentRegisters;

public sealed class GetAllIncomePaymentOrdersByRegisterNetIdMessage {
    public GetAllIncomePaymentOrdersByRegisterNetIdMessage(Guid registerNetId, long limit, long offset, DateTime from, DateTime to, string value, Guid? currencyNetId) {
        RegisterNetId = registerNetId;

        Limit = limit;

        Offset = offset;

        From = from;

        To = to;

        Value = value;

        CurrencyNetId = currencyNetId;
    }

    public Guid RegisterNetId { get; set; }

    public long Limit { get; set; }

    public long Offset { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public string Value { get; set; }

    public Guid? CurrencyNetId { get; set; }
}