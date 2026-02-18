using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class SupplyOrderVehicleServiceRepository : ISupplyOrderVehicleServiceRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderVehicleServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void RemoveAllBySupplyOrderId(long id) {
        _connection.Execute(
            "UPDATE [SupplyOrderVehicleService] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrderVehicleService].[SupplyOrderID] = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllBySupplyOrderIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SupplyOrderVehicleService] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [SupplyOrderVehicleService].SupplyOrderID = @Id AND [SupplyOrderVehicleService].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void Add(IEnumerable<SupplyOrderVehicleService> supplyOrderVehicleServices) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderVehicleService] (SupplyOrderId, VehicleServiceId, Updated) " +
            "VALUES (@SupplyOrderId, @VehicleServiceId, getutcdate())",
            supplyOrderVehicleServices
        );
    }

    public void Update(IEnumerable<SupplyOrderVehicleService> supplyOrderVehicleServices) {
        _connection.Execute(
            "UPDATE [SupplyOrderVehicleService] " +
            "SET SupplyOrderId = @SupplyOrderId, VehicleServiceId = @VehicleServiceId, Updated = getutcdate() " +
            "WHERE [SupplyOrderVehicleService].NetUID = @NetUid",
            supplyOrderVehicleServices
        );
    }

    public void RemoveAllBySupplyOrderAndVehicleServiceId(long supplyOrderId, long vehicleServiceId) {
        _connection.Execute(
            "UPDATE [SupplyOrderVehicleService] " +
            "SET [Deleted] = 1, [Updated] = getutcdate() " +
            "WHERE [SupplyOrderVehicleService].[SupplyOrderID] = @SupplyOrderId " +
            "AND [SupplyOrderVehicleService].[VehicleServiceID] = @ServiceId " +
            "AND [SupplyOrderVehicleService].[Deleted] = 0 ",
            new {
                SupplyOrderId = supplyOrderId,
                ServiceId = vehicleServiceId
            });
    }

    public void RemoveAllByVehicleServiceId(long vehicleServiceId) {
        _connection.Execute(
            "UPDATE [SupplyOrderVehicleService] " +
            "SET [Deleted] = 1, [Updated] = getutcdate() " +
            "WHERE [SupplyOrderVehicleService].[VehicleServiceID] = @Id",
            new { Id = vehicleServiceId });
    }
}