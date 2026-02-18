using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyServicesSearchRepository {
    List<FromSearchServiceOrganization> SearchForServiceOrganizations(string value);

    FromSearchPaymentTasks GetPaymentTasksFromSearchByOrganizationsAndServices(
        string organizationName,
        IEnumerable<ServiceOrganizationType> serviceTypes,
        DateTime from,
        DateTime to);
}