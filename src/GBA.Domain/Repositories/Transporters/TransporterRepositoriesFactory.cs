using System.Data;
using GBA.Domain.Repositories.Transporters.Contracts;

namespace GBA.Domain.Repositories.Transporters;

public sealed class TransporterRepositoriesFactory : ITransporterRepositoriesFactory {
    public ITransporterRepository NewTransporterRepository(IDbConnection connection) {
        return new TransporterRepository(connection);
    }

    public ITransporterTypeRepository NewTransporterTypeRepository(IDbConnection connection) {
        return new TransporterTypeRepository(connection);
    }

    public ITransporterTypeTranslationRepository NewTransporterTypeTranslationRepository(IDbConnection connection) {
        return new TransporterTypeTranslationRepository(connection);
    }
}