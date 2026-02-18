using System.Collections.Generic;

namespace GBA.Domain.EntityHelpers;

public sealed class FromSearchServiceOrganization {
    public FromSearchServiceOrganization(string name, ServiceOrganizationType type) {
        Name = name;

        ServiceOrganizationTypes = new List<ServiceOrganizationType> { type };
    }

    public string Name { get; set; }

    public List<ServiceOrganizationType> ServiceOrganizationTypes { get; set; }
}