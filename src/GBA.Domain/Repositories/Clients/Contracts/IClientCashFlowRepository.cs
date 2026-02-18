using System;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientCashFlowRepository {
    AccountingCashFlow GetRangedBySupplier(Client client, DateTime from, DateTime to);

    AccountingCashFlow GetRangedBySupplierV2(Client client, DateTime from, DateTime to);

    AccountingCashFlow GetRangedBySupplierClientAgreement(ClientAgreement preDefinedClientAgreement, DateTime from, DateTime to);

    AccountingCashFlow GetRangedBySupplierClientAgreementV2(ClientAgreement preDefinedClientAgreement, DateTime from, DateTime to);

    AccountingCashFlow GetRangedByClient(Client client, DateTime from, DateTime to, bool isFromEcommerce = false);

    decimal GetAccountBalanceByClientAgreement(
        long preDefinedClientAgreementId,
        bool isEuroAgreement = false,
        bool isFromEcommerce = false);

    AccountingCashFlow GetRangedByClientAgreement(
        ClientAgreement preDefinedClientAgreement,
        DateTime from,
        DateTime to,
        bool isEuroAgreement = false,
        bool isFromEcommerce = false);
}