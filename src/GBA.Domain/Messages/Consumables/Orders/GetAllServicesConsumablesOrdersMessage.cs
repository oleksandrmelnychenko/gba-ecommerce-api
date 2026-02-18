using System;

namespace GBA.Domain.Messages.Consumables.Orders;

public sealed class GetAllServicesConsumablesOrdersMessage {
    public GetAllServicesConsumablesOrdersMessage(DateTime from, DateTime to, string value, Guid? organizationNetId) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        Value = string.IsNullOrEmpty(value) ? string.Empty : value;

        OrganizationNetId = organizationNetId;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    public string Value { get; }

    public Guid? OrganizationNetId { get; }
}