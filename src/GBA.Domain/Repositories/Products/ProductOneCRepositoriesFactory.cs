using System.Data;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductOneCRepositoriesFactory : IProductOneCRepositoriesFactory {
    public IProductOneCRepository NewProductOneCRepository(IDbConnection connection) {
        return new ProductOneCRepository(connection);
    }
}