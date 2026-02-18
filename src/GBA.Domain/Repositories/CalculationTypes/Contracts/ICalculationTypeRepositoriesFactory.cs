using System.Data;

namespace GBA.Domain.Repositories.CalculationTypes.Contracts;

public interface ICalculationTypeRepositoriesFactory {
    ICalculationTypeRepository NewCalculationTypeRepository(IDbConnection connection);

    ICalculationTypeTranslationRepository NewCalculationTypeTranslationRepository(IDbConnection connection);
}