using System.Data;
using GBA.Domain.Repositories.CalculationTypes.Contracts;

namespace GBA.Domain.Repositories.CalculationTypes;

public sealed class CalculationTypeRepositoriesFactory : ICalculationTypeRepositoriesFactory {
    public ICalculationTypeRepository NewCalculationTypeRepository(IDbConnection connection) {
        return new CalculationTypeRepository(connection);
    }

    public ICalculationTypeTranslationRepository NewCalculationTypeTranslationRepository(IDbConnection connection) {
        return new CalculationTypeTranslationRepository(connection);
    }
}