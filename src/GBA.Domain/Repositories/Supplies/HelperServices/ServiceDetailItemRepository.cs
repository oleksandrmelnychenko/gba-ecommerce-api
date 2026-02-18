using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class ServiceDetailItemRepository : IServiceDetailItemRepository {
    private readonly IDbConnection _connection;

    public ServiceDetailItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ServiceDetailItem> serviceDetailItems) {
        _connection.Execute(
            "INSERT INTO ServiceDetailItem(ServiceDetailItemKeyID, CustomAgencyServiceID, CustomServiceID, VehicleDeliveryServiceID, PlaneDeliveryServiceID, PortCustomAgencyServiceID, PortWorkServiceID, TransportationServiceID, GrossPrice, NetPrice, Qty, UnitPrice, Vat, VatPercent, MergedServiceId, Updated) " +
            "VALUES(@ServiceDetailItemKeyID, @CustomAgencyServiceID, @CustomServiceID, @VehicleDeliveryServiceID, @PlaneDeliveryServiceID, @PortCustomAgencyServiceID, @PortWorkServiceID, @TransportationServiceID, @GrossPrice, @NetPrice, @Qty, @UnitPrice, @Vat, @VatPercent, @MergedServiceId, getutcdate())",
            serviceDetailItems
        );
    }

    public List<ServiceDetailItem> GetAllByNetIdAndType(Guid netId, SupplyServiceType type) {
        string sqlExpression = "SELECT * FROM ServiceDetailItem " +
                               "LEFT JOIN ServiceDetailItemKey " +
                               "ON ServiceDetailItem.ServiceDetailItemKeyID = ServiceDetailItemKey.ID ";

        switch (type) {
            case SupplyServiceType.CustomAgency:
                sqlExpression += "WHERE ServiceDetailItem.CustomAgencyServiceID = (SELECT ID FROM CustomAgencyService WHERE NetUID = @NetId) ";
                break;
            case SupplyServiceType.ExciseDuty:
                sqlExpression +=
                    $"WHERE ServiceDetailItem.CustomServiceID = (SELECT ID FROM CustomService WHERE NetUID = @NetId AND SupplyCustomType = {(int)SupplyCustomType.ExciseDuty}) ";
                break;
            case SupplyServiceType.Custom:
                sqlExpression +=
                    $"WHERE ServiceDetailItem.CustomServiceID = (SELECT ID FROM CustomService WHERE NetUID = @NetId AND AND SupplyCustomType = {(int)SupplyCustomType.Custom}) ";
                break;
            case SupplyServiceType.PlaneDelivery:
                sqlExpression += "WHERE ServiceDetailItem.PlaneDeliveryServiceID = (SELECT ID FROM PlaneDeliveryService WHERE NetUID = @NetId) ";
                break;
            case SupplyServiceType.PortCustomAgency:
                sqlExpression += "WHERE ServiceDetailItem.PortCustomAgencyServiceID = (SELECT ID FROM PortCustomAgencyService WHERE NetUID = @NetId) ";
                break;
            case SupplyServiceType.PortWork:
                sqlExpression += "WHERE ServiceDetailItem.PortWorkServiceID = (SELECT ID FROM PortWorkService WHERE NetUID = @NetId) ";
                break;
            case SupplyServiceType.Transportation:
                sqlExpression += "WHERE ServiceDetailItem.TransportationServiceID = (SELECT ID FROM TransportationService WHERE NetUID = @NetId) ";
                break;
            case SupplyServiceType.VehicleDelivery:
                sqlExpression += "WHERE ServiceDetailItem.VehicleDeliveryServiceID = (SELECT ID FROM VehicleDeliveryService WHERE NetUID = @NetId) ";
                break;
            case SupplyServiceType.Merged:
                sqlExpression += "WHERE ServiceDetailItem.MergedServiceID = (SELECT ID FROM MergedService WHERE NetUID = @NetId) ";
                break;
        }

        sqlExpression += "AND ServiceDetailItem.Deleted = 0 ";

        Type[] types = {
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey)
        };

        ServiceDetailItem mapper(object[] objects) {
            ServiceDetailItem serviceDetailItem = (ServiceDetailItem)objects[0];
            ServiceDetailItemKey serviceDetailItemKey = (ServiceDetailItemKey)objects[1];

            if (serviceDetailItemKey != null) serviceDetailItem.ServiceDetailItemKey = serviceDetailItemKey;

            return serviceDetailItem;
        }

        ;

        List<ServiceDetailItem> detailItemsToReturn = _connection.Query(sqlExpression, types, mapper, new { NetId = netId }).ToList();

        return detailItemsToReturn;
    }

    public void RemoveAllByCustomAgencyServiceId(long id) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].CustomAgencyServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByCustomAgencyServiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].CustomAgencyServiceID = @Id AND [ServiceDetailItem].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByMergedServiceId(long id) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].MergedServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByMergedServiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].MergedServiceID = @Id " +
            "AND [ServiceDetailItem].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByCustomServiceId(long id) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].CustomServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByCustomServiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].CustomServiceID = @Id AND [ServiceDetailItem].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByPlaneDeliveryServiceId(long id) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].PlaneDeliveryServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByPlaneDeliveryServiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].PlaneDeliveryServiceID = @Id AND [ServiceDetailItem].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByPortCustomAgencyServiceId(long id) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].PortCustomAgencyServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByPortCustomAgencyServiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].PortCustomAgencyServiceID = @Id AND [ServiceDetailItem].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByPortWorkServiceId(long id) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].PortWorkServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByPortWorkServiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].PortWorkServiceID = @Id AND [ServiceDetailItem].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByTransportationServiceId(long id) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].TransportationServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByTransportationServiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].TransportationServiceID = @Id AND [ServiceDetailItem].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void RemoveAllByVehicleDeliveryServiceId(long id) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].VehicleDeliveryServiceID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByVehicleDeliveryServiceIdExceptProvided(long id, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ServiceDetailItem] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ServiceDetailItem].VehicleDeliveryServiceID = @Id AND [ServiceDetailItem].ID NOT IN @Ids",
            new { Id = id, Ids = ids }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ServiceDetailItem SET Deleted = 1 WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(IEnumerable<ServiceDetailItem> serviceDetailItems) {
        _connection.Execute(
            "UPDATE ServiceDetailItem SET ServiceDetailItemKeyID = @ServiceDetailItemKeyID, CustomAgencyServiceID = @CustomAgencyServiceID, CustomServiceID = @CustomServiceID, VehicleDeliveryServiceID = @VehicleDeliveryServiceID, " +
            "PlaneDeliveryServiceID = @PlaneDeliveryServiceID, PortCustomAgencyServiceID = @PortCustomAgencyServiceID, PortWorkServiceID = @PortWorkServiceID, TransportationServiceID = @TransportationServiceID, " +
            "GrossPrice = @GrossPrice, NetPrice = @NetPrice, Qty = @Qty, UnitPrice = @UnitPrice, Vat = @Vat, VatPercent = @VatPercent, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            serviceDetailItems
        );
    }
}