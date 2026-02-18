using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Messages.Supplies;

public sealed class SearchForPaymentTasksByOrganizationAndServiceTypesMessage {
    public SearchForPaymentTasksByOrganizationAndServiceTypesMessage(
        string organizationName,
        IEnumerable<ServiceOrganizationType> serviceTypes,
        DateTime from,
        DateTime to) {
        OrganizationName = organizationName;

        ServiceTypes = serviceTypes;

        From = from;

        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string OrganizationName { get; set; }

    public IEnumerable<ServiceOrganizationType> ServiceTypes { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }
}