using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.DebtorModels;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientInDebtRepository {
    void Add(ClientInDebt clientInDebt);

    void Update(ClientInDebt clientInDebt);

    ClientInDebt GetBySaleAndClientAgreementIds(long saleId, long clientAgreementId);

    ClientInDebt GetBySaleAndClientAgreementIdsWithDeleted(long saleId, long clientAgreementId);

    ClientInDebt GetActiveByClientAgreementId(long clientAgreementId);

    ClientInDebt GetExpiredDebtByClientAgreementId(long clientAgreementId);

    ClientInDebt GetBySaleAndAgreementIdWithDeleted(long saleId, long agreementId);

    List<ClientInDebt> GetAllActiveByClientAgreementId(long clientAgreementId);

    List<ClientInDebt> GetAllBySaleIds(IEnumerable<long> saleIds);

    List<ClientInDebt> GetAllBySaleIdsWithDeleted(IEnumerable<long> saleIds);

    List<ClientInDebt> GetAllByClientIdGrouped(Guid netId);

    List<ClientInDebt> GetAllByClientId(long id);

    List<Debt> GetDebtByClientAgreementNetId(Guid netId);

    dynamic GetDebtInfo(Guid clientNetId);

    void Remove(Guid netid);

    void Restore(Guid netid);

    void Restore(long id);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RestoreAllByIds(IEnumerable<long> ids);

    ClientDebtorsModel GetFilteredDebtorsByClientForPrintingDocument(
        Guid? userNetId,
        Guid? organizationNetId,
        TypeOfClientAgreement typeAgreement,
        TypeOfCurrencyOfAgreement typeCurrency);

    ClientDebtorsModel GetFilteredDebtorsByClientInfo(
        Guid? userNetId,
        Guid? organizationNetId,
        TypeOfClientAgreement typeAgreement,
        TypeOfCurrencyOfAgreement typeCurrency,
        long limit,
        long offset);


    ClientInDebt GetByReSaleAndClientAgreementIds(long reSaleId, long clientAgreementId);
}