using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.PackingLists;

public sealed class PackingListPackageOrderItemSupplyServiceRepository : IPackingListPackageOrderItemSupplyServiceRepository {
    private readonly IDbConnection _connection;

    public PackingListPackageOrderItemSupplyServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void New(PackingListPackageOrderItemSupplyService itemService) {
        _connection.Execute(
            "INSERT INTO [PackingListPackageOrderItemSupplyService] ([Updated], [NetValue], [Name], [ExchangeRateDate], [PackingListPackageOrderItemID], [CurrencyID], " +
            "[BillOfLadingServiceID], [ContainerServiceID], [CustomAgencyServiceID], [CustomServiceID], [MergedServiceID], [PlaneDeliveryServiceID], " +
            "[PortCustomAgencyServiceID], [PortWorkServiceID], [TransportationServiceID], [VehicleDeliveryServiceID], [VehicleServiceID], " +
            "[GeneralValue], [ManagementValue]) " +
            "VALUES (@Updated, @NetValue, @Name, @ExchangeRateDate, @PackingListPackageOrderItemID, @CurrencyID, " +
            "@BillOfLadingServiceID, @ContainerServiceID, @CustomAgencyServiceID, @CustomServiceID, @MergedServiceID, @PlaneDeliveryServiceID, " +
            "@PortCustomAgencyServiceID, @PortWorkServiceID, @TransportationServiceID, @VehicleDeliveryServiceID, @VehicleServiceID, " +
            "@GeneralValue, @ManagementValue); ",
            itemService);
    }

    public void Update(PackingListPackageOrderItemSupplyService itemService) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItemSupplyService] " +
            "SET [Updated] = getutcdate() " +
            ", [NetValue] = @NetValue " +
            ", [GeneralValue] = @GeneralValue " +
            ", [ManagementValue] = @ManagementValue " +
            ", [Name] = @Name " +
            ", [ExchangeRateDate] = @ExchangeRateDate " +
            ", [CurrencyID] = @CurrencyID " +
            ", [Deleted] = @Deleted " +
            "WHERE [PackingListPackageOrderItemSupplyService].[ID] = @Id; ",
            itemService);
    }

    public PackingListPackageOrderItemSupplyService GetByPackingListItemAndServiceId(long id, long serviceId, TypeService typeService) {
        string serviceIdCond;

        switch (typeService) {
            case TypeService.BillOfLadingService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[BillOfLadingServiceID] = @ServiceId ";
                break;
            case TypeService.ContainerService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[ContainerServiceID] = @ServiceId ";
                break;
            case TypeService.CustomAgencyService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[CustomAgencyServiceID] = @ServiceId ";
                break;
            case TypeService.CustomService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[CustomServiceID] = @ServiceId ";
                break;
            case TypeService.MergedService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[MergedServiceID] = @ServiceId ";
                break;
            case TypeService.PlaneDeliveryService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[PlaneDeliveryServiceID] = @ServiceId ";
                break;
            case TypeService.PortCustomAgencyService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[PortCustomAgencyServiceID] = @ServiceId ";
                break;
            case TypeService.PortWorkService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[PortWorkServiceID] = @ServiceId ";
                break;
            case TypeService.TransportationService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[TransportationServiceID] = @ServiceId ";
                break;
            case TypeService.VehicleDeliveryService:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[VehicleDeliveryServiceID] = @ServiceId ";
                break;
            default:
                serviceIdCond = "AND [PackingListPackageOrderItemSupplyService].[VehicleServiceID] = @ServiceId ";
                break;
        }

        return _connection.Query<PackingListPackageOrderItemSupplyService>(
            "SELECT * FROM [PackingListPackageOrderItemSupplyService] " +
            "WHERE [PackingListPackageOrderItemSupplyService].[PackingListPackageOrderItemID] = @Id " +
            serviceIdCond,
            new { Id = id, ServiceId = serviceId }).FirstOrDefault();
    }


    public void RemoveByServiceId(long serviceId, TypeService typeService) {
        string serviceIdCond;

        switch (typeService) {
            case TypeService.BillOfLadingService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[BillOfLadingServiceID] = @Id; ";
                break;
            case TypeService.ContainerService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[ContainerServiceID] = @Id; ";
                break;
            case TypeService.CustomAgencyService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[CustomAgencyServiceID] = @Id; ";
                break;
            case TypeService.CustomService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[CustomServiceID] = @Id; ";
                break;
            case TypeService.MergedService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[MergedServiceID] = @Id; ";
                break;
            case TypeService.PlaneDeliveryService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[PlaneDeliveryServiceID] = @Id; ";
                break;
            case TypeService.PortCustomAgencyService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[PortCustomAgencyServiceID] = @Id; ";
                break;
            case TypeService.PortWorkService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[PortWorkServiceID] = @Id; ";
                break;
            case TypeService.TransportationService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[TransportationServiceID] = @Id; ";
                break;
            case TypeService.VehicleDeliveryService:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[VehicleDeliveryServiceID] = @Id; ";
                break;
            default:
                serviceIdCond = "[PackingListPackageOrderItemSupplyService].[VehicleServiceID] = @Id; ";
                break;
        }

        _connection.Execute(
            "UPDATE [PackingListPackageOrderItemSupplyService] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 1 " +
            "WHERE " + serviceIdCond,
            new { Id = serviceId });
    }
}