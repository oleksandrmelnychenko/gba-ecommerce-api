using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Repositories.Agreements.Contracts;

public interface IAgreementTypeRepository {
    long Add(AgreementType agreementType);

    void Update(AgreementType agreementType);

    AgreementType GetById(long id);

    AgreementType GetByNetId(Guid netId);

    List<AgreementType> GetAll();

    void Remove(Guid netId);
}