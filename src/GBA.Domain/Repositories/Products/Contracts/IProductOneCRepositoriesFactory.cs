using System.Data;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductOneCRepositoriesFactory {
    IProductOneCRepository NewProductOneCRepository(IDbConnection connection);
}