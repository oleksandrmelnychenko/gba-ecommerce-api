using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class SupplyOrderContainerServiceRepository : ISupplyOrderContainerServiceRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderContainerServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<SupplyOrderContainerService> supplyOrderContainerServices) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderContainerService] (SupplyOrderId, ContainerServiceId, Updated) " +
            "VALUES (@SupplyOrderId, @ContainerServiceId, getutcdate())",
            supplyOrderContainerServices
        );
    }

    public void Update(IEnumerable<SupplyOrderContainerService> supplyOrderContainerServices) {
        _connection.Execute(
            "UPDATE [SupplyOrderContainerService] " +
            "SET SupplyOrderId = @SupplyOrderId, ContainerServiceId = @ContainerServiceId, Updated = getutcdate() " +
            "WHERE [SupplyOrderContainerService].NetUID = @NetUid",
            supplyOrderContainerServices
        );
    }

    public void RemoveAllBySupplyOrderId(long id) {
        _connection.Execute(
            "UPDATE [SupplyOrderContainerService] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrderContainerService].SupplyOrderID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllBySupplyOrderIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrderContainerService] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrderContainerService].SupplyOrderID = @Id AND [SupplyOrderContainerService].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllBySupplyOrderAndContainerServiceId(long supplyOrderId, long containerServiceId) {
        _connection.Execute(
            "UPDATE [SupplyOrderContainerService] " +
            "SET [Deleted] = 1, [Updated] = getutcdate() " +
            "WHERE [SupplyOrderContainerService].[SupplyOrderID] = @SupplyOrderId " +
            "AND [SupplyOrderContainerService].[ContainerServiceID] = @ServiceId " +
            "AND [SupplyOrderContainerService].[Deleted] = 0 ",
            new {
                SupplyOrderId = supplyOrderId,
                ServiceId = containerServiceId
            });
    }

    public void RemoveAllByContainerServiceId(long containerServiceId) {
        _connection.Execute(
            "UPDATE [SupplyOrderContainerService] " +
            "SET [Deleted] = 1, [Updated] = getutcdate() " +
            "WHERE [SupplyOrderContainerService].[ContainerServiceID] = @Id",
            new { Id = containerServiceId });
    }
}