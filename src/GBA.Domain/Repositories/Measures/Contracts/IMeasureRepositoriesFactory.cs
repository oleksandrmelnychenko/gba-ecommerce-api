using System.Data;

namespace GBA.Domain.Repositories.Measures.Contracts;

public interface IMeasureRepositoriesFactory {
    MeasureUnitRepository NewMeasureUnitRepository(IDbConnection connection);
    MeasureUnitTranslationRepository NewMeasureUnitTranslationRepository(IDbConnection connection);
}