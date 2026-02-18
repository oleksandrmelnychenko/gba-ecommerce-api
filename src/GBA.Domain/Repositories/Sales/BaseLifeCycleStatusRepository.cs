using System;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class BaseLifeCycleStatusRepository : IBaseLifeCycleStatusRepository {
    private readonly IDbConnection _connection;

    public BaseLifeCycleStatusRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(BaseLifeCycleStatus baseLifeCycleStatus) {
        return _connection.Query<long>(
                "INSERT INTO BaseLifeCycleStatus (SaleLifeCycleType, Updated) " +
                "VALUES(@SaleLifeCycleType, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                baseLifeCycleStatus
            )
            .Single();
    }

    public BaseLifeCycleStatus GetByNetId(Guid netId) {
        return _connection.Query<BaseLifeCycleStatus>(
                "SELECT * FROM BaseLifeCycleStatus " +
                "WHERE NetUid = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Update(BaseLifeCycleStatus baseLifeCycleStatus) {
        _connection.Execute(
            "UPDATE BaseLifeCycleStatus " +
            "SET SaleLifeCycleType = @SaleLifeCycleType, Updated = getutcdate() " +
            "WHERE NetUid = @NetUid",
            baseLifeCycleStatus
        );
    }
}