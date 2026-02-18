using System.Data;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientRepositoriesFactory : IClientRepositoriesFactory {
    public IClientRepository NewClientRepository(IDbConnection connection) {
        return new ClientRepository(connection);
    }

    public IClientCashFlowRepository NewClientCashFlowRepository(IDbConnection connection) {
        return new ClientCashFlowRepository(connection);
    }

    public IClientTypeRepository NewClientTypeRepository(IDbConnection connection) {
        return new ClientTypeRepository(connection);
    }

    public IClientTypeTranslationRepository NewClientTypeTranslationRepository(IDbConnection connection) {
        return new ClientTypeTranslationRepository(connection);
    }

    public IClientTypeRoleRepository NewClientTypeRoleRepository(IDbConnection connection) {
        return new ClientTypeRoleRepository(connection);
    }

    public IClientInRoleRepository NewClientInRoleRepository(IDbConnection connection) {
        return new ClientInRoleRepository(connection);
    }

    public IClientTypeRoleTranslationRepository NewClientTypeRoleTranslationRepository(IDbConnection connection) {
        return new ClientTypeRoleTranslationRepository(connection);
    }

    public IPerfectClientRepository NewPerfectClientRepository(IDbConnection connection) {
        return new PerfectClientRepository(connection);
    }

    public IClientPerfectClientRepository NewClientPerfectClientRepository(IDbConnection connection) {
        return new ClientPerfectClientRepository(connection);
    }

    public IClientAgreementRepository NewClientAgreementRepository(IDbConnection connection) {
        return new ClientAgreementRepository(connection);
    }

    public IClientUserProfileRepository NewClientUserProfileRepository(IDbConnection connection) {
        return new ClientUserProfileRepository(connection);
    }

    public IPerfectClientValueRepository NewPerfectClientValueRepository(IDbConnection connection) {
        return new PerfectClientValueRepository(connection);
    }

    public IPerfectClientTranslationRepository NewPerfectClientTranslationRepository(IDbConnection connection) {
        return new PerfectClientTranslationRepository(connection);
    }

    public IPerfectClientValueTranslationRepository NewPerfectClientValueTranslationRepository(IDbConnection connection) {
        return new PerfectClientValueTranslationRepository(connection);
    }

    public IClientSubClientRepository NewClientSubClientRepository(IDbConnection connection) {
        return new ClientSubClientRepository(connection);
    }

    public IClientWorkplaceRepository NewClientWorkplaceRepository(IDbConnection connection) {
        return new ClientWorkplaceRepository(connection);
    }

    public IClientInDebtRepository NewClientInDebtRepository(IDbConnection connection) {
        return new ClientInDebtRepository(connection);
    }

    public IClientBankDetailsRepository NewClientBankDetailsRepository(IDbConnection connection) {
        return new ClientBankDetailsRepository(connection);
    }

    public IClientBankDetailAccountNumberRepository NewClientBankDetailAccountNumberRepository(IDbConnection connection) {
        return new ClientBankDetailAccountNumberRepository(connection);
    }

    public IClientBankDetailIbanNoRepository NewClientBankDetailIbanNoRepository(IDbConnection connection) {
        return new ClientBankDetailIbanNoRepository(connection);
    }

    public IPackingMarkingRepository NewPackingMarkingRepository(IDbConnection connection) {
        return new PackingMarkingRepository(connection);
    }

    public IPackingMarkingPaymentRepository NewPackingMarkingPaymentRepository(IDbConnection connection) {
        return new PackingMarkingPaymentRepository(connection);
    }

    public IClientContractDocumentRepository NewClientContractDocumentRepository(IDbConnection connection) {
        return new ClientContractDocumentRepository(connection);
    }

    public IClientRegistrationTaskRepository NewClientRegistrationTaskRepository(IDbConnection connection) {
        return new ClientRegistrationTaskRepository(connection);
    }

    public IClientShoppingCartRepository NewClientShoppingCartRepository(IDbConnection connection) {
        return new ClientShoppingCartRepository(connection);
    }

    public IClientBalanceMovementRepository NewClientBalanceMovementRepository(IDbConnection connection) {
        return new ClientBalanceMovementRepository(connection);
    }

    public IDebtorRepository NewDebtorRepository(IDbConnection connection) {
        return new DebtorRepository(connection);
    }

    public IIncotermRepository NewIncotermRepository(IDbConnection connection) {
        return new IncotermRepository(connection);
    }

    public IClientGroupRepository NewClientGroupRepository(IDbConnection connection) {
        return new ClientGroupRepository(connection);
    }

    public IWorkplaceRepository NewWorkplaceRepository(IDbConnection connection) {
        return new WorkplaceRepository(connection);
    }
}