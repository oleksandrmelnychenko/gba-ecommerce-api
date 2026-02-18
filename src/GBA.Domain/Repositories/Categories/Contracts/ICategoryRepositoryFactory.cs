using System.Data;

namespace GBA.Domain.Repositories.Categories.Contracts;

public interface ICategoryRepositoryFactory {
    ICategoryRepository NewCategoryRepository(IDbConnection connection);
}