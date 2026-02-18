using System.Data;
using GBA.Domain.Repositories.Categories.Contracts;

namespace GBA.Domain.Repositories.Categories;

public sealed class CategoryRepositoryFactory : ICategoryRepositoryFactory {
    public ICategoryRepository NewCategoryRepository(IDbConnection connection) {
        return new CategoryRepository(connection);
    }
}