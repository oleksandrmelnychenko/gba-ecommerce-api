using System.Data;
using GBA.Domain.Repositories.Agreements.Contracts;

namespace GBA.Domain.Repositories.Agreements;

public sealed class AgreementRepositoriesFactory : IAgreementRepositoriesFactory {
    public IAgreementRepository NewAgreementRepository(IDbConnection connection) {
        return new AgreementRepository(connection);
    }

    public IAgreementTypeRepository NewAgreementTypeRepository(IDbConnection connection) {
        return new AgreementTypeRepository(connection);
    }

    public IAgreementTypeTranslationRepository NewAgreementTypeTranslationRepository(IDbConnection connection) {
        return new AgreementTypeTranslationRepository(connection);
    }
}