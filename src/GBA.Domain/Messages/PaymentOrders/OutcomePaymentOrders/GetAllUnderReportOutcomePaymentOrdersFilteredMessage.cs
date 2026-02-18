using System;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public sealed class GetAllUnderReportOutcomePaymentOrdersFilteredMessage {
    public GetAllUnderReportOutcomePaymentOrdersFilteredMessage(long limit, long offset, DateTime from, DateTime to, string value, Guid? currencyNetId, Guid? registerNetId,
        Guid? paymentMovementNetId) {
        Limit = limit;

        Offset = offset;

        From = from;

        To = to;

        Value = value;

        CurrencyNetId = currencyNetId;

        RegisterNetId = registerNetId;

        PaymentMovementNetId = paymentMovementNetId;
    }

    public long Limit { get; set; }

    public long Offset { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public string Value { get; set; }

    public Guid? CurrencyNetId { get; set; }

    public Guid? RegisterNetId { get; set; }

    public Guid? PaymentMovementNetId { get; set; }
}