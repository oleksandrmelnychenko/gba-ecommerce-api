using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSalesFilteredMessage {
    public GetSalesFilteredMessage(
        SaleLifeCycleType? saleLifeCycleType,
        QueryType type,
        long? clientId,
        long[] organisationIds,
        Guid? userNetId,
        DateTime? from,
        DateTime? to,
        string value,
        bool fromShipments,
        int limit,
        int offset,
        bool forEcommerce,
        bool fastEcommerce) {
        SaleLifeCycleType = saleLifeCycleType;
        Type = type;
        UserNetId = userNetId;
        From = from;
        To = to;
        Value = value;
        FromShipments = fromShipments;
        Limit = limit;
        Offset = offset;
        ForEcommerce = forEcommerce;
        FastEcommerce = fastEcommerce;
        ClientId = clientId;
        OrganisationIds = organisationIds;
    }

    public SaleLifeCycleType? SaleLifeCycleType { get; set; }

    public QueryType Type { get; set; }

    public Guid? UserNetId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public string Value { get; set; }

    public bool FromShipments { get; }
    public int Limit { get; }
    public int Offset { get; }
    public bool ForEcommerce { get; set; }

    public bool FastEcommerce { get; }
    public long? ClientId { get; }
    public long[] OrganisationIds { get; }
}