using System;

namespace GBA.Domain.Messages.PaymentOrders.OutcomePaymentOrders;

public sealed class GetAllOutcomePaymentOrdersMessage {
    public GetAllOutcomePaymentOrdersMessage(long limit, long offset, DateTime from, DateTime to, string value, Guid? currencyNetId, Guid? registerNetId,
        Guid? paymentMovementNetId, long[] organizationIds) {
        Limit = limit;

        Offset = offset;

        From = from;

        To = to;

        Value = value;

        CurrencyNetId = currencyNetId;

        RegisterNetId = registerNetId;

        PaymentMovementNetId = paymentMovementNetId;
        OrganizationIds = organizationIds;
    }

    public long Limit { get; set; }

    public long Offset { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public string Value { get; set; }

    public Guid? CurrencyNetId { get; set; }

    public Guid? RegisterNetId { get; set; }

    public Guid? PaymentMovementNetId { get; set; }

    public long[] OrganizationIds { get; }
}