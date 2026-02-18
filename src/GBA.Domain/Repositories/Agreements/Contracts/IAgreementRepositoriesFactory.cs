using System.Data;

namespace GBA.Domain.Repositories.Agreements.Contracts;

public interface IAgreementRepositoriesFactory {
    IAgreementRepository NewAgreementRepository(IDbConnection connection);

    IAgreementTypeRepository NewAgreementTypeRepository(IDbConnection connection);

    IAgreementTypeTranslationRepository NewAgreementTypeTranslationRepository(IDbConnection connection);
}