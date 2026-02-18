using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class CarBrandRepository : ICarBrandRepository {
    private readonly IDbConnection _connection;

    public CarBrandRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<CarBrand> GetAllCarBrands() {
        return _connection.Query<CarBrand>(
            "SELECT * " +
            "FROM [CarBrand] " +
            "WHERE Deleted = 0 " +
            "ORDER BY [Name]"
        );
    }

    public CarBrand GetByAliasIfExists(string alias) {
        return _connection.Query<CarBrand>(
            "SELECT * " +
            "FROM [CarBrand] " +
            "WHERE [CarBrand].Alias = @Alias " +
            "AND [CarBrand].Deleted = 0",
            new { Alias = alias }
        ).FirstOrDefault();
    }
}