using System.Data;

namespace GBA.Domain.Repositories.Transporters.Contracts;

public interface ITransporterRepositoriesFactory {
    ITransporterRepository NewTransporterRepository(IDbConnection connection);

    ITransporterTypeRepository NewTransporterTypeRepository(IDbConnection connection);

    ITransporterTypeTranslationRepository NewTransporterTypeTranslationRepository(IDbConnection connection);
}