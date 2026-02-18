using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Products;

namespace GBA.Services.Services.Products.Contracts;

public interface ICarBrandService {
    Task<IEnumerable<CarBrand>> GetAllCarBrands();

    Task<List<Product>> GetAllProductsFilteredByCarBrand(Guid carBrandNetId, Guid currentClientNetId, long limit, long offset);

    Task<List<Product>> GetAllProductsFilteredByCarBrand(string carBrandAlias, Guid currentClientNetId, long limit, long offset);
}