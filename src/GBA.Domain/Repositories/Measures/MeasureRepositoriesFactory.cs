using System.Data;
using GBA.Domain.Repositories.Measures.Contracts;

namespace GBA.Domain.Repositories.Measures;

public sealed class MeasureRepositoriesFactory : IMeasureRepositoriesFactory {
    public MeasureUnitRepository NewMeasureUnitRepository(IDbConnection connection) {
        return new MeasureUnitRepository(connection);
    }

    public MeasureUnitTranslationRepository NewMeasureUnitTranslationRepository(IDbConnection connection) {
        return new MeasureUnitTranslationRepository(connection);
    }
}