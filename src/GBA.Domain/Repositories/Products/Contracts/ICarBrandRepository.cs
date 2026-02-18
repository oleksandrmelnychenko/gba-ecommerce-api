using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface ICarBrandRepository {
    CarBrand GetByAliasIfExists(string alias);

    IEnumerable<CarBrand> GetAllCarBrands();
}