using System.Data;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientRepositoriesFactory {
    IClientRepository NewClientRepository(IDbConnection connection);

    IClientCashFlowRepository NewClientCashFlowRepository(IDbConnection connection);

    IClientTypeRepository NewClientTypeRepository(IDbConnection connection);

    IClientTypeTranslationRepository NewClientTypeTranslationRepository(IDbConnection connection);

    IClientTypeRoleRepository NewClientTypeRoleRepository(IDbConnection connection);

    IClientInRoleRepository NewClientInRoleRepository(IDbConnection connection);

    IClientTypeRoleTranslationRepository NewClientTypeRoleTranslationRepository(IDbConnection connection);

    IPerfectClientRepository NewPerfectClientRepository(IDbConnection connection);

    IClientPerfectClientRepository NewClientPerfectClientRepository(IDbConnection connection);

    IClientAgreementRepository NewClientAgreementRepository(IDbConnection connection);

    IClientUserProfileRepository NewClientUserProfileRepository(IDbConnection connection);

    IPerfectClientValueRepository NewPerfectClientValueRepository(IDbConnection connection);

    IPerfectClientTranslationRepository NewPerfectClientTranslationRepository(IDbConnection connection);

    IPerfectClientValueTranslationRepository NewPerfectClientValueTranslationRepository(IDbConnection connection);

    IClientSubClientRepository NewClientSubClientRepository(IDbConnection connection);

    IClientWorkplaceRepository NewClientWorkplaceRepository(IDbConnection connection);

    IClientInDebtRepository NewClientInDebtRepository(IDbConnection connection);

    IClientBankDetailsRepository NewClientBankDetailsRepository(IDbConnection connection);

    IClientBankDetailAccountNumberRepository NewClientBankDetailAccountNumberRepository(IDbConnection connection);

    IClientBankDetailIbanNoRepository NewClientBankDetailIbanNoRepository(IDbConnection connection);

    IPackingMarkingRepository NewPackingMarkingRepository(IDbConnection connection);

    IPackingMarkingPaymentRepository NewPackingMarkingPaymentRepository(IDbConnection connection);

    IClientContractDocumentRepository NewClientContractDocumentRepository(IDbConnection connection);

    IClientRegistrationTaskRepository NewClientRegistrationTaskRepository(IDbConnection connection);

    IClientShoppingCartRepository NewClientShoppingCartRepository(IDbConnection connection);

    IClientBalanceMovementRepository NewClientBalanceMovementRepository(IDbConnection connection);

    IDebtorRepository NewDebtorRepository(IDbConnection connection);

    IIncotermRepository NewIncotermRepository(IDbConnection connection);

    IClientGroupRepository NewClientGroupRepository(IDbConnection connection);

    IWorkplaceRepository NewWorkplaceRepository(IDbConnection connection);
}