using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Organizations.Contracts;

public interface IOrganizationRepository {
    long Add(Organization organization);

    void Update(Organization organization);

    Organization GetById(long id);

    Organization GetByOrganizationCultureIfExists(string culture);

    Organization GetByNetId(Guid netId);

    Organization GetOrganizationByCurrentCultureIfExists();

    Organization GetCorrectOrganization();

    List<Organization> GetAll();

    bool IsAssignedToAnyAgreement(long organizationId);

    void Remove(Guid netId);

    long GetOrganizationIdByNetId(Guid organizationNetId);

    string GetCultureByNetId(Guid organizationNetId);
}