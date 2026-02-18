using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Repositories.Consignments.Contracts;

namespace GBA.Domain.Repositories.Consignments;

public sealed class ConsignmentRepository : IConsignmentRepository {
    private readonly IDbConnection _connection;

    public ConsignmentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Consignment consignment) {
        return _connection.Query<long>(
            "INSERT INTO [Consignment] (IsVirtual, [FromDate], StorageId, OrganizationId, ProductIncomeId, ProductTransferId, Updated) " +
            "VALUES (@IsVirtual, @FromDate, @StorageId, @OrganizationId, @ProductIncomeId, @ProductTransferId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            consignment
        ).First();
    }

    public Consignment GetIfExistsByConsignmentParams(Consignment consignment) {
        return _connection.Query<Consignment>(
            "SELECT TOP(1) * " +
            "FROM [Consignment] " +
            "WHERE [Consignment].IsVirtual = @IsVirtual " +
            "AND [Consignment].StorageID = @StorageId " +
            "AND [Consignment].OrganizationID = @OrganizationId " +
            "AND [Consignment].ProductIncomeID = @ProductIncomeId " +
            "AND [Consignment].ProductTransferID = @ProductTransferId " +
            "ORDER BY [Consignment].ID DESC",
            consignment
        ).SingleOrDefault();
    }
}