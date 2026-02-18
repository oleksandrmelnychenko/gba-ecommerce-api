using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllSalesForSaleReturnsFromSearchMessage {
    public GetAllSalesForSaleReturnsFromSearchMessage(
        DateTime from,
        DateTime to,
        Guid clientNetId,
        string value,
        Guid? organizationNetId) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        ClientNetId = clientNetId;

        OrganizationNetId = organizationNetId;

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public Guid ClientNetId { get; }
    public Guid? OrganizationNetId { get; }

    public string Value { get; }
}