using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IWorkplaceRepository {
    long AddWorkplace(Workplace workplace);

    void Update(Workplace workplace);

    void Update(IEnumerable<Workplace> workplaces);

    void RemoveById(long id);

    void RemoveClientGroupByNetId(Guid netId);

    void RemoveWorkplaceClientAgreementById(long id);

    void DisableById(long id);

    long AddWorkplaceClientAgreement(WorkplaceClientAgreement workplaceClientAgreement);

    void UpdateWorkplaceClientAgreement(WorkplaceClientAgreement workplaceClientAgreement);

    Workplace GetById(long id);

    Workplace GetByNetId(Guid netId);

    Workplace GetByNetIdWithClient(Guid netId);

    IEnumerable<Workplace> GetWorkplacesByMainClientId(long id);

    IEnumerable<Workplace> GetWorkplacesByMainClientNetId(Guid netId);

    IEnumerable<Workplace> GetWorkplacesByClientGroupId(long id);

    IEnumerable<Workplace> GetWorkplacesByClientGroupNetId(Guid netId);

    IEnumerable<WorkplaceClientAgreement> GetWorkplaceClientAgreementsByWorkplaceId(long id);
}