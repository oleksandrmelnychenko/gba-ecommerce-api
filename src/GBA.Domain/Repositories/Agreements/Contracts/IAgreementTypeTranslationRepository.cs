using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Agreements.Contracts;

public interface IAgreementTypeTranslationRepository {
    long Add(AgreementTypeTranslation agreementTypeTranslation);

    void Update(AgreementTypeTranslation agreementTypeTranslation);

    AgreementTypeTranslation GetById(long id);

    AgreementTypeTranslation GetByNetId(Guid netId);

    List<AgreementTypeTranslation> GetAll();

    void Remove(Guid netId);
}