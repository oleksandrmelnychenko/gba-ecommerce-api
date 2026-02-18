using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class OrderProductSpecificationRepository : IOrderProductSpecificationRepository {
    private readonly IDbConnection _connection;

    public OrderProductSpecificationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(OrderProductSpecification specification) {
        _connection.Execute(
            "INSERT INTO [OrderProductSpecification] (Qty, UnitPrice, SupplyInvoiceId, ProductSpecificationId, SadId, Updated) " +
            "VALUES (@Qty, @UnitPrice, @SupplyInvoiceId, @ProductSpecificationId, @SadId, GETUTCDATE())",
            specification
        );
    }
}