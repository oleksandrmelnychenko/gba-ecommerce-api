using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyInformationTaskRepository : ISupplyInformationTaskRepository {
    private readonly IDbConnection _connection;

    public SupplyInformationTaskRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long? Add(SupplyInformationTask supplyInformationTask) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyInformationTask] ([Updated], [Comment], [FromDate], [UserID], [UpdatedByID], [DeletedByID], [GrossPrice]) " +
            "VALUES (getutcdate(), @Comment, @FromDate, @UserID, @UpdatedByID, @DeletedByID, @GrossPrice); " +
            "SELECT SCOPE_IDENTITY(); ",
            supplyInformationTask).SingleOrDefault();
    }

    public void Update(SupplyInformationTask supplyInformationTask) {
        _connection.Execute(
            "UPDATE [SupplyInformationTask] " +
            "SET [Updated] = getutcdate() " +
            ", [Comment] = @Comment " +
            ", [FromDate] = @FromDate " +
            ", [UpdatedByID] = @UpdatedByID " +
            ", [UserID] = @UserID " +
            ", [GrossPrice] = @GrossPrice " +
            "WHERE [SupplyInformationTask].[ID] = @Id; ",
            supplyInformationTask);
    }

    public void Remove(SupplyInformationTask supplyInformationTask) {
        _connection.Execute(
            "UPDATE [SupplyInformationTask] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            ", [DeletedByID] = @DeletedByID " +
            "WHERE [SupplyInformationTask].[ID] = @Id; ",
            supplyInformationTask);
    }
}