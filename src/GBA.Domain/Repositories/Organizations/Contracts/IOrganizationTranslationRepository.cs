using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Organizations.Contracts;

public interface IOrganizationTranslationRepository {
    long Add(OrganizationTranslation organizationTranslation);

    void Add(IEnumerable<OrganizationTranslation> organizationTranslations);

    void Update(OrganizationTranslation organizationTranslation);

    void Update(IEnumerable<OrganizationTranslation> organizationTranslations);

    OrganizationTranslation GetById(long id);

    OrganizationTranslation GetByNetId(Guid netId);

    List<OrganizationTranslation> GetAll();

    void Remove(Guid netId);
}