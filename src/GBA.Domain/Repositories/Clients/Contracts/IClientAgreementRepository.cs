using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientAgreementRepository {
    long Add(ClientAgreement clientAgreement);

    void Add(IEnumerable<ClientAgreement> clientAgreements);

    void Update(IEnumerable<ClientAgreement> clientAgreements);

    void UpdateAmountByNetId(Guid netId, decimal amount);

    void RemoveAllByClientId(long clientId);

    void Remove(IEnumerable<ClientAgreement> clientAgreements);

    List<ClientAgreement> GetAllByClientIdWithoutIncludes(long id);

    List<ClientAgreement> GetAllByRetailClientNetId(Guid retailClientNetId);

    List<ClientAgreement> GetAllWithSubClientsByClientNetId(Guid clientNetId);

    List<ClientAgreement> GetAllByClientNetId(Guid netId);

    List<ClientAgreement> GetAllByClientNetIdGrouped(Guid netId);

    List<ClientAgreement> GetAllByAgreementIds(IEnumerable<long> ids);

    ClientAgreement GetActiveByRootClientNetId(Guid clientNetId, bool withVat);

    ClientAgreement GetActiveBySubClientNetId(Guid clientNetId);

    ClientAgreement GetByNetIdWithOrganizationInfo(Guid netId);

    ClientAgreement GetByClientAndAgreementIds(long clientId, long agreementId);

    ClientAgreement GetActiveByClientId(long id);

    ClientAgreement GetActiveByClientNetId(Guid netId);

    ClientAgreement GetById(long id);

    ClientAgreement GetWithOrganizationById(long id);

    ClientAgreement GetByIdWithAgreementAndOrganization(long id);

    ClientAgreement GetByIdWithoutIncludes(long id);

    ClientAgreement GetByNetId(Guid netId);

    ClientAgreement GetByNetIdWithIncludes(Guid netId);

    ClientAgreement GetByNetIdWithDiscountForSpecificProduct(Guid netId, long productGroupId);

    ClientAgreement GetByNetIdWithAgreementAndDiscountForSpecificProduct(Guid netId, long productGroupId);

    ClientAgreement GetByNetIdWithAgreement(Guid netId);

    ClientAgreement GetByNetIdWithoutIncludes(Guid netId);

    ClientAgreement GetWithOrganizationByNetId(Guid netId);

    ClientAgreement GetBySaleId(long id);

    ClientAgreement GetByNetIdWithClientRole(Guid netId);

    ClientAgreement GetByNetIdWithClientInfo(Guid netId);

    ClientAgreement GetSelectedByClientNetId(Guid clientNetId);
    ClientAgreement GetSelectedByClientNotSelectedNetId(Guid clientNetId);

    ClientAgreement GetSelectedByWorkplaceNetId(Guid workplaceNetId);

    bool IsSubClientsHasAgreements(Guid netId);

    ClientAgreement GetByNetIdWithAgreementAndOrganization(Guid netId);

    ClientAgreement GetClientAgreementWithAgreementByPackingListId(long id);

    ClientAgreement GetClientAgreementBySupplyOrderUkraineId(long id);

    ClientAgreement GetByClientNetIdWithOrWithoutVat(Guid netId, long organizationId, bool withVat);

    ClientAgreement GetWithClientInfoByAgreementNetId(Guid netId);
}