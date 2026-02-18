using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Repositories.Agreements.Contracts;

public interface IAgreementRepository {
    long Add(Agreement agreement);

    void Update(Agreement agreement);

    Agreement GetById(long id);

    Agreement GetByNetId(Guid netId);

    Agreement GetDefaultByCulture();

    Agreement GetLastRecord();

    Agreement GetAgreementByClientAgreementId(long id);

    Agreement GetLastRecordByOrganizationId(long organizationId);

    Agreement GetLastRecordByOrganizationCode(string organizationCode);

    List<Agreement> GetAll();

    List<Agreement> GetAllByIds(List<long> ids);

    void Remove(Guid netId);

    List<TaxAccountingScheme> GetAllTaxAccountingScheme();

    List<AgreementTypeCivilCode> GetAllAgreementTypeCivilCodeMessage();
}