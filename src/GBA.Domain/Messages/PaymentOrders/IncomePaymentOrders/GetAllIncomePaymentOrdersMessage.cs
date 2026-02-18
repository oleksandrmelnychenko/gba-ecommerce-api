using System;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class GetAllIncomePaymentOrdersMessage {
    public GetAllIncomePaymentOrdersMessage(long limit, long offset, DateTime from, DateTime to, string value, Guid? currencyNetId, Guid? registerNetId, long[] organizationIds) {
        Limit = limit;

        Offset = offset;

        From = from;

        To = to;

        Value = value;

        CurrencyNetId = currencyNetId;

        RegisterNetId = registerNetId;
        OrganizationIds = organizationIds;
    }

    public long Limit { get; set; }

    public long Offset { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public string Value { get; set; }

    public Guid? CurrencyNetId { get; set; }

    public Guid? RegisterNetId { get; set; }

    public long[] OrganizationIds { get; }
}