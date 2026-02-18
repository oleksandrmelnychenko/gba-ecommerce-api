using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Repositories.Ecommerce.Contracts;

namespace GBA.Domain.Repositories.Ecommerce;

public sealed class EcommerceRegionRepository : IEcommerceRegionRepository {
    private readonly IDbConnection _connection;

    public EcommerceRegionRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<EcommerceRegion> GetAll() {
        return _connection.Query<EcommerceRegion>(
            "SELECT * FROM [EcommerceRegion] " +
            "WHERE [Deleted] = 0 ");
    }

    public long Add(EcommerceRegion ecommerceRegion) {
        return _connection.Query<long>(
            "INSERT INTO [EcommerceRegion] ([NameUa], [NameRu], [IsLocalPayment], [Updated]) " +
            "VALUES (@NameUa, @NameRu, @IsLocalPayment, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY() ",
            ecommerceRegion).SingleOrDefault();
    }

    public void Update(EcommerceRegion ecommerceRegion) {
        _connection.Execute(
            "UPDATE [EcommerceRegion] " +
            "SET [NameUa] = @NameUa, [NameRu] = @NameRu, [IsLocalPayment] = @IsLocalPayment, [Deleted] = @Deleted, [Updated] = GETUTCDATE() " +
            "WHERE [ID] = @Id",
            ecommerceRegion);
    }
}