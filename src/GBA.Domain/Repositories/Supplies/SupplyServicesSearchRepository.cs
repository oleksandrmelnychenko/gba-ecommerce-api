using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyServicesSearchRepository : ISupplyServicesSearchRepository {
    private readonly IDbConnection _connection;

    public SupplyServicesSearchRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<FromSearchServiceOrganization> SearchForServiceOrganizations(string value) {
        List<FromSearchServiceOrganization> toReturn = new();

        _connection.Query<string, ServiceOrganizationType, string>(
            ";WITH [OrganizationsSearch_CTE] " +
            "AS " +
            "( " +
            "SELECT [ContainerOrganization].Name, 0 AS [Type] " +
            "FROM [SupplyOrganization] AS [ContainerOrganization] " +
            "WHERE [ContainerOrganization].Name like '%' + @Value + '%' " +
            "AND [ContainerOrganization].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [CustomAgencyOrganization].Name, 1 AS [Type] " +
            "FROM [SupplyOrganization] AS [CustomAgencyOrganization] " +
            "WHERE [CustomAgencyOrganization].Name like '%' + @Value + '%' " +
            "AND [CustomAgencyOrganization].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [CustomOrganization].Name, 2 AS [Type] " +
            "FROM [SupplyOrganization] AS [CustomOrganization] " +
            "WHERE [CustomOrganization].Name like '%' + @Value + '%' " +
            "AND [CustomOrganization].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [ExciseDutyOrganization].Name, 3 AS [Type] " +
            "FROM [SupplyOrganization] AS [ExciseDutyOrganization] " +
            "WHERE [ExciseDutyOrganization].Name like '%' + @Value + '%' " +
            "AND [ExciseDutyOrganization].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PlaneDeliveryOrganization].Name, 4 AS [Type] " +
            "FROM [SupplyOrganization] AS [PlaneDeliveryOrganization] " +
            "WHERE [PlaneDeliveryOrganization].Name like '%' + @Value + '%' " +
            "AND [PlaneDeliveryOrganization].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortCustomAgencyOrganization].Name, 5 AS [Type] " +
            "FROM [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
            "WHERE [PortCustomAgencyOrganization].Name like '%' + @Value + '%' " +
            "AND [PortCustomAgencyOrganization].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [PortWorkOrganization].Name, 6 AS [Type] " +
            "FROM [SupplyOrganization] AS [PortWorkOrganization] " +
            "WHERE [PortWorkOrganization].Name like '%' + @Value + '%' " +
            "AND [PortWorkOrganization].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [TransportationOrganization].Name, 7 AS [Type] " +
            "FROM [SupplyOrganization] AS [TransportationOrganization] " +
            "WHERE [TransportationOrganization].Name like '%' + @Value + '%' " +
            "AND [TransportationOrganization].Deleted = 0 " +
            "UNION ALL " +
            "SELECT [VehicleDeliveryOrganization].Name, 8 AS [Type] " +
            "FROM [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
            "WHERE [VehicleDeliveryOrganization].Name like '%' + @Value + '%' " +
            "AND [VehicleDeliveryOrganization].Deleted = 0 " +
            ") " +
            "SELECT [OrganizationsSearch_CTE].Name AS [Name] " +
            ",[OrganizationsSearch_CTE].Type AS [Type] " +
            "FROM [OrganizationsSearch_CTE] " +
            "ORDER BY [OrganizationsSearch_CTE].Name " +
            ",[OrganizationsSearch_CTE].Type",
            (name, type) => {
                if (toReturn.Any(o => o.Name.ToLower().Equals(name.ToLower()))) {
                    FromSearchServiceOrganization fromList = toReturn.First(o => o.Name.ToLower().Equals(name.ToLower()));

                    if (!fromList.ServiceOrganizationTypes.Any(t => t.Equals(type))) fromList.ServiceOrganizationTypes.Add(type);
                } else {
                    toReturn.Add(new FromSearchServiceOrganization(name, type));
                }

                return name;
            },
            new { Value = value },
            splitOn: "Type"
        );

        return toReturn;
    }

    public FromSearchPaymentTasks GetPaymentTasksFromSearchByOrganizationsAndServices(
        string organizationName,
        IEnumerable<ServiceOrganizationType> serviceTypes,
        DateTime from,
        DateTime to) {
        FromSearchPaymentTasks toReturn = new();

        IEnumerable<long> forTotalIds = _connection.Query<long>(
            ";WITH " +
            "[ServicesSearch_CTE_Stage1] " +
            "AS " +
            "( " +
            "SELECT [ContainerServicePaymentTask].ID " +
            "FROM [SupplyOrderContainerService] " +
            "LEFT JOIN [ContainerService] " +
            "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
            "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
            "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] AS [ContainerServicePaymentTask] " +
            "ON [ContainerServicePaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
            "WHERE [SupplyOrderContainerService].Deleted = 0 " +
            "AND [ContainerOrganization].Name = @OrganizationName " +
            "AND 0 IN @ServiceTypes " +
            "UNION " +
            "SELECT [CustomAgencyServicePaymentTask].ID " +
            "FROM [CustomAgencyService] " +
            "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
            "ON [CustomAgencyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomAgencyServicePaymentTask] " +
            "ON [CustomAgencyServicePaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
            "WHERE [CustomAgencyService].Deleted = 0 " +
            "AND [CustomAgencyOrganization].Name = @OrganizationName " +
            "AND 1 IN @ServiceTypes " +
            "UNION " +
            "SELECT [CustomServicePaymentTask].ID " +
            "FROM [CustomService] " +
            "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
            "ON [CustomOrganization].ID = [CustomService].CustomOrganizationID " +
            "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
            "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomServicePaymentTask] " +
            "ON [CustomServicePaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
            "WHERE [CustomService].Deleted = 0 " +
            "AND ( " +
            "([CustomOrganization].Name = @OrganizationName AND 2 IN @ServiceTypes) " +
            "OR " +
            "([ExciseDutyOrganization].Name = @OrganizationName AND 3 IN @ServiceTypes) " +
            ") " +
            "UNION " +
            "SELECT [PlaneDeliveryServicePaymentTask].ID " +
            "FROM [PlaneDeliveryService] " +
            "LEFT JOIN [SupplyOrganization] AS [PlaneDeliveryOrganization] " +
            "ON [PlaneDeliveryOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PlaneDeliveryServicePaymentTask] " +
            "ON [PlaneDeliveryServicePaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
            "WHERE [PlaneDeliveryService].Deleted = 0 " +
            "AND [PlaneDeliveryOrganization].Name = @OrganizationName " +
            "AND 4 IN @ServiceTypes " +
            "UNION " +
            "SELECT [PortCustomAgencyServicePaymentTask].ID " +
            "FROM [PortCustomAgencyService] " +
            "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
            "ON [PortCustomAgencyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PortCustomAgencyServicePaymentTask] " +
            "ON [PortCustomAgencyServicePaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
            "WHERE [PortCustomAgencyService].Deleted = 0 " +
            "AND [PortCustomAgencyOrganization].Name = @OrganizationName " +
            "AND 5 IN @ServiceTypes " +
            "UNION " +
            "SELECT [PortWorkServicePaymentTask].ID " +
            "FROM [PortWorkService] " +
            "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
            "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PortWorkServicePaymentTask] " +
            "ON [PortWorkServicePaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
            "WHERE [PortWorkService].Deleted = 0 " +
            "AND [PortWorkOrganization].Name = @OrganizationName " +
            "AND 6 IN @ServiceTypes " +
            "UNION " +
            "SELECT [TransportationServicePaymentTask].ID " +
            "FROM [TransportationService] " +
            "LEFT JOIN [SupplyOrganization] AS [TransportationOrganization] " +
            "ON [TransportationOrganization].ID = [TransportationService].TransportationOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] AS [TransportationServicePaymentTask] " +
            "ON [TransportationServicePaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
            "WHERE [TransportationService].Deleted = 0 " +
            "AND [TransportationOrganization].Name = @OrganizationName " +
            "AND 7 IN @ServiceTypes " +
            "UNION " +
            "SELECT [VehicleDeliveryServicePaymentTask].ID " +
            "FROM [VehicleDeliveryService] " +
            "LEFT JOIN [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
            "ON [VehicleDeliveryOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] AS [VehicleDeliveryServicePaymentTask] " +
            "ON [VehicleDeliveryServicePaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
            "WHERE [VehicleDeliveryService].Deleted = 0 " +
            "AND [VehicleDeliveryOrganization].Name = @OrganizationName " +
            "AND 8 IN @ServiceTypes " +
            ") " +
            "SELECT [ServicesSearch_CTE_Stage1].ID FROM [ServicesSearch_CTE_Stage1]",
            new { OrganizationName = organizationName, ServiceTypes = serviceTypes }
        );

        toReturn.Total = _connection.Query<decimal>(
                ";WITH [ServiceSearch_CTE_Stage2] " +
                "AS " +
                "( " +
                "SELECT " +
                "( " +
                "CASE " +
                "WHEN [ContainerService].NetPrice IS NOT NULL " +
                "THEN CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [ContainerService].NetPrice), 2) ELSE ROUND([ContainerService].NetPrice, 2) END " +
                "ELSE 0 " +
                "END " +
                ") AS [NetPrice] " +
                "FROM [ContainerService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids " +
                "UNION ALL " +
                "SELECT " +
                "( " +
                "CASE " +
                "WHEN [CustomAgencyService].NetPrice IS NOT NULL " +
                "THEN CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [CustomAgencyService].NetPrice), 2) ELSE ROUND([CustomAgencyService].NetPrice, 2) END " +
                "ELSE 0 " +
                "END " +
                ") AS [NetPrice] " +
                "FROM [CustomAgencyService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids " +
                "UNION ALL " +
                "SELECT " +
                "( " +
                "CASE " +
                "WHEN [CustomService].NetPrice IS NOT NULL " +
                "THEN CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [CustomService].NetPrice), 2) ELSE ROUND([CustomService].NetPrice, 2) END " +
                "ELSE 0 " +
                "END " +
                ") AS [NetPrice] " +
                "FROM [CustomService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids " +
                "UNION ALL " +
                "SELECT " +
                "( " +
                "CASE " +
                "WHEN [PlaneDeliveryService].NetPrice IS NOT NULL " +
                "THEN CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [PlaneDeliveryService].NetPrice), 2) ELSE ROUND([PlaneDeliveryService].NetPrice, 2) END " +
                "ELSE 0 " +
                "END " +
                ") AS [NetPrice] " +
                "FROM [PlaneDeliveryService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids " +
                "UNION ALL " +
                "SELECT " +
                "( " +
                "CASE " +
                "WHEN [PortCustomAgencyService].NetPrice IS NOT NULL " +
                "THEN CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [PortCustomAgencyService].NetPrice), 2) ELSE ROUND([PortCustomAgencyService].NetPrice, 2) END " +
                "ELSE 0 " +
                "END " +
                ") AS [NetPrice] " +
                "FROM [PortCustomAgencyService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids " +
                "UNION ALL " +
                "SELECT " +
                "( " +
                "CASE " +
                "WHEN [PortWorkService].NetPrice IS NOT NULL " +
                "THEN CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [PortWorkService].NetPrice), 2) ELSE ROUND([PortWorkService].NetPrice, 2) END " +
                "ELSE 0 " +
                "END " +
                ") AS [NetPrice] " +
                "FROM [PortWorkService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids " +
                "UNION ALL " +
                "SELECT " +
                "( " +
                "CASE " +
                "WHEN [TransportationService].NetPrice IS NOT NULL " +
                "THEN CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [TransportationService].NetPrice), 2) ELSE ROUND([TransportationService].NetPrice, 2) END " +
                "ELSE 0 " +
                "END " +
                ") AS [NetPrice] " +
                "FROM [TransportationService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids " +
                "UNION ALL " +
                "SELECT " +
                "( " +
                "CASE " +
                "WHEN [VehicleDeliveryService].NetPrice IS NOT NULL " +
                "THEN CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [VehicleDeliveryService].NetPrice), 2) ELSE ROUND([VehicleDeliveryService].NetPrice, 2) END " +
                "ELSE 0 " +
                "END " +
                ") AS [NetPrice] " +
                "FROM [VehicleDeliveryService] " +
                "LEFT JOIN [SupplyPaymentTask] " +
                "ON [SupplyPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids " +
                ") " +
                "SELECT " +
                "(CASE " +
                "WHEN COUNT([ServiceSearch_CTE_Stage2].NetPrice) > 0 " +
                "THEN ROUND(SUM([ServiceSearch_CTE_Stage2].NetPrice), 2) " +
                "ELSE 0 " +
                "END) AS [Total] " +
                "FROM [ServiceSearch_CTE_Stage2]",
                new { Ids = forTotalIds }
            )
            .Single();

        List<long> ids = _connection.Query<long>(
                ";WITH [ServicesSearch_CTE] " +
                "AS " +
                "( " +
                "SELECT [ContainerServicePaymentTask].ID " +
                "FROM [SupplyOrderContainerService] " +
                "LEFT JOIN [ContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyPaymentTask] AS [ContainerServicePaymentTask] " +
                "ON [ContainerServicePaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
                "WHERE [SupplyOrderContainerService].Deleted = 0 " +
                "AND [ContainerService].Created > @From " +
                "AND [ContainerService].Created < @To " +
                "AND [ContainerOrganization].Name = @OrganizationName " +
                "AND 0 IN @ServiceTypes " +
                "UNION " +
                "SELECT [CustomAgencyServicePaymentTask].ID " +
                "FROM [CustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
                "ON [CustomAgencyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyPaymentTask] AS [CustomAgencyServicePaymentTask] " +
                "ON [CustomAgencyServicePaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
                "WHERE [CustomAgencyService].Deleted = 0 " +
                "AND [CustomAgencyService].Created > @From " +
                "AND [CustomAgencyService].Created < @To " +
                "AND [CustomAgencyOrganization].Name = @OrganizationName " +
                "AND 1 IN @ServiceTypes " +
                "UNION " +
                "SELECT [CustomServicePaymentTask].ID " +
                "FROM [CustomService] " +
                "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
                "ON [CustomOrganization].ID = [CustomService].CustomOrganizationID " +
                "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
                "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
                "LEFT JOIN [SupplyPaymentTask] AS [CustomServicePaymentTask] " +
                "ON [CustomServicePaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
                "WHERE [CustomService].Deleted = 0 " +
                "AND [CustomService].Created > @From " +
                "AND [CustomService].Created < @To " +
                "AND ( " +
                "([CustomOrganization].Name = @OrganizationName AND 2 IN @ServiceTypes) " +
                "OR " +
                "([ExciseDutyOrganization].Name = @OrganizationName AND 3 IN @ServiceTypes) " +
                ") " +
                "UNION " +
                "SELECT [PlaneDeliveryServicePaymentTask].ID " +
                "FROM [PlaneDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] AS [PlaneDeliveryOrganization] " +
                "ON [PlaneDeliveryOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
                "LEFT JOIN [SupplyPaymentTask] AS [PlaneDeliveryServicePaymentTask] " +
                "ON [PlaneDeliveryServicePaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
                "WHERE [PlaneDeliveryService].Deleted = 0 " +
                "AND [PlaneDeliveryService].Created > @From " +
                "AND [PlaneDeliveryService].Created < @To " +
                "AND [PlaneDeliveryOrganization].Name = @OrganizationName " +
                "AND 4 IN @ServiceTypes " +
                "UNION " +
                "SELECT [PortCustomAgencyServicePaymentTask].ID " +
                "FROM [PortCustomAgencyService] " +
                "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
                "ON [PortCustomAgencyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyPaymentTask] AS [PortCustomAgencyServicePaymentTask] " +
                "ON [PortCustomAgencyServicePaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
                "WHERE [PortCustomAgencyService].Deleted = 0 " +
                "AND [PortCustomAgencyService].Created > @From " +
                "AND [PortCustomAgencyService].Created < @To " +
                "AND [PortCustomAgencyOrganization].Name = @OrganizationName " +
                "AND 5 IN @ServiceTypes " +
                "UNION " +
                "SELECT [PortWorkServicePaymentTask].ID " +
                "FROM [PortWorkService] " +
                "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
                "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyPaymentTask] AS [PortWorkServicePaymentTask] " +
                "ON [PortWorkServicePaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
                "WHERE [PortWorkService].Deleted = 0 " +
                "AND [PortWorkService].Created > @From " +
                "AND [PortWorkService].Created < @To " +
                "AND [PortWorkOrganization].Name = @OrganizationName " +
                "AND 6 IN @ServiceTypes " +
                "UNION " +
                "SELECT [TransportationServicePaymentTask].ID " +
                "FROM [TransportationService] " +
                "LEFT JOIN [SupplyOrganization] AS [TransportationOrganization] " +
                "ON [TransportationOrganization].ID = [TransportationService].TransportationOrganizationID " +
                "LEFT JOIN [SupplyPaymentTask] AS [TransportationServicePaymentTask] " +
                "ON [TransportationServicePaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
                "WHERE [TransportationService].Deleted = 0 " +
                "AND [TransportationService].Created > @From " +
                "AND [TransportationService].Created < @To " +
                "AND [TransportationOrganization].Name = @OrganizationName " +
                "AND 7 IN @ServiceTypes " +
                "UNION " +
                "SELECT [VehicleDeliveryServicePaymentTask].ID " +
                "FROM [VehicleDeliveryService] " +
                "LEFT JOIN [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
                "ON [VehicleDeliveryOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
                "LEFT JOIN [SupplyPaymentTask] AS [VehicleDeliveryServicePaymentTask] " +
                "ON [VehicleDeliveryServicePaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
                "WHERE [VehicleDeliveryService].Deleted = 0 " +
                "AND [VehicleDeliveryService].Created > @From " +
                "AND [VehicleDeliveryService].Created < @To " +
                "AND [VehicleDeliveryOrganization].Name = @OrganizationName " +
                "AND 8 IN @ServiceTypes " +
                ") " +
                "SELECT [ServicesSearch_CTE].ID FROM [ServicesSearch_CTE] " +
                "ORDER BY [ServicesSearch_CTE].ID DESC",
                new { OrganizationName = organizationName, From = from, To = to, ServiceTypes = serviceTypes }
            )
            .ToList();

        if (!ids.Any()) return toReturn;

        if (serviceTypes.Any(t => t.Equals(ServiceOrganizationType.ContainerOrganization))) {
            _connection.Query<SupplyPaymentTask, User, ContainerService, SupplyOrganization, SupplyOrderContainerService, SupplyOrder, Client, SupplyPaymentTask>(
                "SELECT [SupplyPaymentTask].* " +
                ", [User].* " +
                ", [ContainerService].ID " +
                ", [ContainerService].BillOfLadingDocumentID " +
                ", [ContainerService].ContainerNumber " +
                ", [ContainerService].ContainerOrganizationID " +
                ", [ContainerService].Created " +
                ", [ContainerService].Deleted " +
                ", [ContainerService].FromDate " +
                ", [ContainerService].GroosWeight " +
                ", [ContainerService].GrossPrice " +
                ", [ContainerService].IsActive " +
                ", [ContainerService].IsCalculatedExtraCharge " +
                ", [ContainerService].LoadDate " +
                ", [ContainerService].Name " +
                ", ( " +
                "CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [ContainerService].NetPrice), 2) ELSE ROUND([ContainerService].NetPrice, 2) END " +
                ") AS [NetPrice] " +
                ", [ContainerService].NetUID " +
                ", [ContainerService].Number " +
                ", [ContainerService].SupplyExtraChargeType " +
                ", [ContainerService].SupplyPaymentTaskID " +
                ", [ContainerService].TermDeliveryInDays " +
                ", [ContainerService].Updated " +
                ", [ContainerService].UserID " +
                ", [ContainerService].Vat " +
                ", [ContainerService].VatPercent " +
                ", [ContainerOrganization].* " +
                ", [SupplyOrderContainerService].* " +
                ", [ContainerServiceSupplyOrder].* " +
                ", [ContainerServiceSupplyOrderClient].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [ContainerService] " +
                "ON [ContainerService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "LEFT JOIN [SupplyOrderContainerService] " +
                "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
                "AND [SupplyOrderContainerService].Deleted = 0 " +
                "LEFT JOIN [SupplyOrder] AS [ContainerServiceSupplyOrder] " +
                "ON [ContainerServiceSupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "LEFT JOIN [Client] AS [ContainerServiceSupplyOrderClient] " +
                "ON [ContainerServiceSupplyOrderClient].ID = [ContainerServiceSupplyOrder].ClientID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (paymentTask, user, containerService, containerOrganization, supplyOrderContainerService, supplyOrder, client) => {
                    if (containerService != null) {
                        if (!toReturn.SupplyPaymentTasks.Any(t => t.Id.Equals(paymentTask.Id))) {
                            if (supplyOrderContainerService != null) {
                                supplyOrder.Client = client;

                                supplyOrderContainerService.SupplyOrder = supplyOrder;

                                containerService.SupplyOrderContainerServices.Add(supplyOrderContainerService);
                            }

                            containerService.ContainerOrganization = containerOrganization;

                            paymentTask.ContainerServices.Add(containerService);
                            paymentTask.User = user;

                            paymentTask.CurrentTotal = Math.Round(containerService.NetPrice + toReturn.SupplyPaymentTasks.Sum(t => t.CurrentTotal), 2);
                            toReturn.TotalByRange += Math.Round(containerService.NetPrice, 2);

                            toReturn.SupplyPaymentTasks.Add(paymentTask);
                            ids.Remove(paymentTask.Id);
                        } else if (supplyOrderContainerService != null) {
                            SupplyPaymentTask fromList = toReturn.SupplyPaymentTasks.First(t => t.Id.Equals(paymentTask.Id));

                            ContainerService containerServiceFromList = fromList.ContainerServices.First();

                            if (!containerServiceFromList.SupplyOrderContainerServices.Any(j => j.Id.Equals(supplyOrderContainerService.Id))) {
                                supplyOrder.Client = client;

                                supplyOrderContainerService.SupplyOrder = supplyOrder;

                                containerServiceFromList.SupplyOrderContainerServices.Add(supplyOrderContainerService);
                            }
                        }
                    }

                    return paymentTask;
                },
                new { Ids = ids }
            );

            if (!ids.Any()) return toReturn;
        }

        if (serviceTypes.Any(t => t.Equals(ServiceOrganizationType.CustomAgencyOrganization))) {
            _connection.Query<SupplyPaymentTask, User, CustomAgencyService, SupplyOrganization, SupplyOrder, Client, SupplyPaymentTask>(
                "SELECT [SupplyPaymentTask].* " +
                ", [User].* " +
                ", [CustomAgencyService].ID " +
                ", [CustomAgencyService].Created " +
                ", [CustomAgencyService].CustomAgencyOrganizationID " +
                ", [CustomAgencyService].Deleted " +
                ", [CustomAgencyService].FromDate " +
                ", [CustomAgencyService].GrossPrice " +
                ", [CustomAgencyService].IsActive " +
                ", [CustomAgencyService].Name " +
                ", ( " +
                "CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [CustomAgencyService].NetPrice), 2) ELSE ROUND([CustomAgencyService].NetPrice, 2) END " +
                ") AS [NetPrice] " +
                ", [CustomAgencyService].NetUID " +
                ", [CustomAgencyService].Number " +
                ", [CustomAgencyService].SupplyPaymentTaskID " +
                ", [CustomAgencyService].Updated " +
                ", [CustomAgencyService].UserID " +
                ", [CustomAgencyService].Vat " +
                ", [CustomAgencyService].VatPercent " +
                ", [CustomAgencyOrganization].* " +
                ", [CustomAgencySupplyOrder].* " +
                ", [CustomAgencySupplyOrderClient].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [CustomAgencyService] " +
                "ON [CustomAgencyService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
                "ON [CustomAgencyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrder] AS [CustomAgencySupplyOrder] " +
                "ON [CustomAgencySupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
                "LEFT JOIN [Client] AS [CustomAgencySupplyOrderClient] " +
                "ON [CustomAgencySupplyOrderClient].ID = [CustomAgencySupplyOrder].ClientID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (paymentTask, user, customAgencyService, customAgencyOrganization, supplyOrder, client) => {
                    if (customAgencyService != null) {
                        if (supplyOrder != null) {
                            supplyOrder.Client = client;
                            customAgencyService.SupplyOrders.Add(supplyOrder);
                        }

                        customAgencyService.CustomAgencyOrganization = customAgencyOrganization;

                        paymentTask.User = user;
                        paymentTask.CustomAgencyServices.Add(customAgencyService);

                        paymentTask.CurrentTotal = Math.Round(customAgencyService.NetPrice + toReturn.SupplyPaymentTasks.Sum(t => t.CurrentTotal), 2);
                        toReturn.TotalByRange += Math.Round(customAgencyService.NetPrice, 2);

                        toReturn.SupplyPaymentTasks.Add(paymentTask);
                        ids.Remove(paymentTask.Id);
                    }

                    return paymentTask;
                },
                new { Ids = ids }
            );

            if (!ids.Any()) return toReturn;
        }

        if (serviceTypes.Any(t => t.Equals(ServiceOrganizationType.CustomOrganization)) || serviceTypes.Any(t => t.Equals(ServiceOrganizationType.ExciseDutyOrganization))) {
            _connection.Query<SupplyPaymentTask, User, CustomService, SupplyOrganization, SupplyOrganization, SupplyOrder, Client, SupplyPaymentTask>(
                "SELECT [SupplyPaymentTask].* " +
                ", [User].* " +
                ", [CustomService].ID " +
                ", [CustomService].Created " +
                ", [CustomService].CustomOrganizationID " +
                ", [CustomService].Deleted " +
                ", [CustomService].ExciseDutyOrganizationID " +
                ", [CustomService].FromDate " +
                ", [CustomService].GrossPrice " +
                ", [CustomService].IsActive " +
                ", [CustomService].Name " +
                ",( " +
                "CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [CustomService].NetPrice), 2) ELSE ROUND([CustomService].NetPrice, 2) END " +
                ") AS [NetPrice] " +
                ", [CustomService].NetUID " +
                ", [CustomService].Number " +
                ", [CustomService].SupplyCustomType " +
                ", [CustomService].SupplyOrderID " +
                ", [CustomService].SupplyPaymentTaskID " +
                ", [CustomService].Updated " +
                ", [CustomService].UserID " +
                ", [CustomService].Vat " +
                ", [CustomService].VatPercent " +
                ", [CustomOrganization].* " +
                ", [ExciseDutyOrganization].* " +
                ", [CustomServiceSupplyOrder].* " +
                ", [CustomServiceSupplyOrderClient].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [CustomService] " +
                "ON [CustomService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
                "ON [CustomOrganization].ID = [CustomService].CustomOrganizationID " +
                "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
                "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
                "LEFT JOIN [SupplyOrder] AS [CustomServiceSupplyOrder] " +
                "ON [CustomServiceSupplyOrder].ID = [CustomService].SupplyOrderID " +
                "LEFT JOIN [Client] AS [CustomServiceSupplyOrderClient] " +
                "ON [CustomServiceSupplyOrderClient].ID = [CustomServiceSupplyOrder].ClientID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (paymentTask, user, customService, customOrganization, exciseDutyOrganization, supplyOrder, client) => {
                    if (customService != null) {
                        if (supplyOrder != null) {
                            supplyOrder.Client = client;
                            customService.SupplyOrder = supplyOrder;
                        }

                        customService.CustomOrganization = customOrganization;
                        customService.ExciseDutyOrganization = exciseDutyOrganization;

                        paymentTask.User = user;
                        paymentTask.BrokerServices.Add(customService);

                        paymentTask.CurrentTotal = Math.Round(customService.NetPrice + toReturn.SupplyPaymentTasks.Sum(t => t.CurrentTotal), 2);
                        toReturn.TotalByRange += Math.Round(customService.NetPrice, 2);

                        toReturn.SupplyPaymentTasks.Add(paymentTask);
                        ids.Remove(paymentTask.Id);
                    }

                    return paymentTask;
                },
                new { Ids = ids }
            );

            if (!ids.Any()) return toReturn;
        }

        if (serviceTypes.Any(t => t.Equals(ServiceOrganizationType.PlaneDeliveryOrganization))) {
            _connection.Query<SupplyPaymentTask, User, PlaneDeliveryService, SupplyOrganization, SupplyOrder, Client, SupplyPaymentTask>(
                "SELECT [SupplyPaymentTask].* " +
                ", [User].* " +
                ", [PlaneDeliveryService].ID " +
                ", [PlaneDeliveryService].Created " +
                ", [PlaneDeliveryService].Deleted " +
                ", [PlaneDeliveryService].FromDate " +
                ", [PlaneDeliveryService].GrossPrice " +
                ", [PlaneDeliveryService].IsActive " +
                ", [PlaneDeliveryService].Name " +
                ",( " +
                "CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [PlaneDeliveryService].NetPrice), 2) ELSE ROUND([PlaneDeliveryService].NetPrice, 2) END " +
                ") AS [NetPrice] " +
                ", [PlaneDeliveryService].NetUID " +
                ", [PlaneDeliveryService].Number " +
                ", [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
                ", [PlaneDeliveryService].SupplyPaymentTaskID " +
                ", [PlaneDeliveryService].Updated " +
                ", [PlaneDeliveryService].UserID " +
                ", [PlaneDeliveryService].Vat " +
                ", [PlaneDeliveryService].VatPercent " +
                ", [PlaneDeliveryOrganization].* " +
                ", [PlaneDeliveryServiceSupplyOrder].* " +
                ", [PlaneDeliveryServiceSupplyOrderClient].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [PlaneDeliveryService] " +
                "ON [PlaneDeliveryService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [PlaneDeliveryOrganization] " +
                "ON [PlaneDeliveryOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrder] AS [PlaneDeliveryServiceSupplyOrder] " +
                "ON [PlaneDeliveryServiceSupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
                "LEFT JOIN [Client] AS [PlaneDeliveryServiceSupplyOrderClient] " +
                "ON [PlaneDeliveryServiceSupplyOrderClient].ID = [PlaneDeliveryServiceSupplyOrder].ClientID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (paymentTask, user, planeDeliveryService, planeDeliveryOrganization, supplyOrder, client) => {
                    if (planeDeliveryService != null) {
                        if (supplyOrder != null) {
                            supplyOrder.Client = client;

                            planeDeliveryService.SupplyOrders.Add(supplyOrder);
                        }

                        planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;

                        paymentTask.User = user;
                        paymentTask.PlaneDeliveryServices.Add(planeDeliveryService);

                        paymentTask.CurrentTotal = Math.Round(planeDeliveryService.NetPrice + toReturn.SupplyPaymentTasks.Sum(t => t.CurrentTotal), 2);
                        toReturn.TotalByRange += Math.Round(planeDeliveryService.NetPrice, 2);

                        toReturn.SupplyPaymentTasks.Add(paymentTask);
                        ids.Remove(paymentTask.Id);
                    }

                    return paymentTask;
                },
                new { Ids = ids }
            );

            if (!ids.Any()) return toReturn;
        }

        if (serviceTypes.Any(t => t.Equals(ServiceOrganizationType.PortCustomAgencyOrganization))) {
            _connection.Query<SupplyPaymentTask, User, PortCustomAgencyService, SupplyOrganization, SupplyOrder, Client, SupplyPaymentTask>(
                "SELECT [SupplyPaymentTask].* " +
                ", [User].* " +
                ", [PortCustomAgencyService].ID " +
                ", [PortCustomAgencyService].Created " +
                ", [PortCustomAgencyService].Deleted " +
                ", [PortCustomAgencyService].FromDate " +
                ", [PortCustomAgencyService].GrossPrice " +
                ", [PortCustomAgencyService].IsActive " +
                ", [PortCustomAgencyService].Name " +
                ",( " +
                "CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [PortCustomAgencyService].NetPrice), 2) ELSE ROUND([PortCustomAgencyService].NetPrice, 2) END " +
                ") AS [NetPrice] " +
                ", [PortCustomAgencyService].NetUID " +
                ", [PortCustomAgencyService].Number " +
                ", [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
                ", [PortCustomAgencyService].SupplyPaymentTaskID " +
                ", [PortCustomAgencyService].Updated " +
                ", [PortCustomAgencyService].UserID " +
                ", [PortCustomAgencyService].Vat " +
                ", [PortCustomAgencyService].VatPercent " +
                ", [PortCustomAgencyOrganization].* " +
                ", [PortCustomAgencyServiceSupplyOrder].* " +
                ", [PortCustomAgencyServiceSupplyOrderClient].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [PortCustomAgencyService] " +
                "ON [PortCustomAgencyService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
                "ON [PortCustomAgencyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
                "LEFT JOIN [SupplyOrder] AS [PortCustomAgencyServiceSupplyOrder] " +
                "ON [PortCustomAgencyServiceSupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
                "LEFT JOIN [Client] AS [PortCustomAgencyServiceSupplyOrderClient] " +
                "ON [PortCustomAgencyServiceSupplyOrderClient].ID = [PortCustomAgencyServiceSupplyOrder].ClientID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (paymentTask, user, portCustomAgencyService, portCustomAgencyOrganization, supplyOrder, client) => {
                    if (portCustomAgencyService != null) {
                        if (supplyOrder != null) {
                            supplyOrder.Client = client;

                            portCustomAgencyService.SupplyOrders.Add(supplyOrder);
                        }

                        portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;

                        paymentTask.User = user;
                        paymentTask.PortCustomAgencyServices.Add(portCustomAgencyService);

                        paymentTask.CurrentTotal = Math.Round(portCustomAgencyService.NetPrice + toReturn.SupplyPaymentTasks.Sum(t => t.CurrentTotal), 2);
                        toReturn.TotalByRange += Math.Round(portCustomAgencyService.NetPrice, 2);

                        toReturn.SupplyPaymentTasks.Add(paymentTask);
                        ids.Remove(paymentTask.Id);
                    }

                    return paymentTask;
                },
                new { Ids = ids }
            );

            if (!ids.Any()) return toReturn;
        }

        if (serviceTypes.Any(t => t.Equals(ServiceOrganizationType.PortWorkOrganization))) {
            _connection.Query<SupplyPaymentTask, User, PortWorkService, SupplyOrganization, SupplyOrder, Client, SupplyPaymentTask>(
                "SELECT [SupplyPaymentTask].* " +
                ", [User].* " +
                ", [PortWorkService].ID " +
                ", [PortWorkService].Created " +
                ", [PortWorkService].Deleted " +
                ", [PortWorkService].FromDate " +
                ", [PortWorkService].GrossPrice " +
                ", [PortWorkService].IsActive " +
                ", [PortWorkService].Name " +
                ",( " +
                "CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [PortWorkService].NetPrice), 2) ELSE ROUND([PortWorkService].NetPrice, 2) END " +
                ") AS [NetPrice] " +
                ", [PortWorkService].NetUID " +
                ", [PortWorkService].Number " +
                ", [PortWorkService].PortWorkOrganizationID " +
                ", [PortWorkService].SupplyPaymentTaskID " +
                ", [PortWorkService].Updated " +
                ", [PortWorkService].UserID " +
                ", [PortWorkService].Vat " +
                ", [PortWorkService].VatPercent " +
                ", [PortWorkOrganization].* " +
                ", [PortWorkServiceSupplyOrder].* " +
                ", [PortWorkServiceSupplyOrderClient].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [PortWorkService] " +
                "ON [PortWorkService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
                "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
                "LEFT JOIN [SupplyOrder] AS [PortWorkServiceSupplyOrder] " +
                "ON [PortWorkServiceSupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
                "LEFT JOIN [Client] AS [PortWorkServiceSupplyOrderClient] " +
                "ON [PortWorkServiceSupplyOrderClient].ID = [PortWorkServiceSupplyOrder].ClientID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (paymentTask, user, portWorkService, portWorkOrganization, supplyOrder, client) => {
                    if (portWorkService != null) {
                        if (supplyOrder != null) {
                            supplyOrder.Client = client;

                            portWorkService.SupplyOrders.Add(supplyOrder);
                        }

                        portWorkService.PortWorkOrganization = portWorkOrganization;

                        paymentTask.User = user;
                        paymentTask.PortWorkServices.Add(portWorkService);

                        paymentTask.CurrentTotal = Math.Round(portWorkService.NetPrice + toReturn.SupplyPaymentTasks.Sum(t => t.CurrentTotal), 2);
                        toReturn.TotalByRange += Math.Round(portWorkService.NetPrice, 2);

                        toReturn.SupplyPaymentTasks.Add(paymentTask);
                        ids.Remove(paymentTask.Id);
                    }

                    return paymentTask;
                },
                new { Ids = ids }
            );

            if (!ids.Any()) return toReturn;
        }

        if (serviceTypes.Any(t => t.Equals(ServiceOrganizationType.TransportationOrganization))) {
            _connection.Query<SupplyPaymentTask, User, TransportationService, SupplyOrganization, SupplyOrder, Client, SupplyPaymentTask>(
                "SELECT [SupplyPaymentTask].* " +
                ", [User].* " +
                ", [TransportationService].ID " +
                ", [TransportationService].Created " +
                ", [TransportationService].Deleted " +
                ", [TransportationService].FromDate " +
                ", [TransportationService].GrossPrice " +
                ", [TransportationService].IsActive " +
                ", [TransportationService].IsSealAndSignatureVerified " +
                ", [TransportationService].Name " +
                ",( " +
                "CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [TransportationService].NetPrice), 2) ELSE ROUND([TransportationService].NetPrice, 2) END " +
                ") AS [NetPrice] " +
                ", [TransportationService].NetUID " +
                ", [TransportationService].Number " +
                ", [TransportationService].SupplyPaymentTaskID " +
                ", [TransportationService].TransportationOrganizationID " +
                ", [TransportationService].Updated " +
                ", [TransportationService].UserID " +
                ", [TransportationService].Vat " +
                ", [TransportationService].VatPercent " +
                ", [TransportationOrganization].* " +
                ", [TransportationServiceSupplyOrder].* " +
                ", [TransportationServiceSupplyOrderClient].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [TransportationService] " +
                "ON [TransportationService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [TransportationOrganization] " +
                "ON [TransportationOrganization].ID = [TransportationService].TransportationOrganizationID " +
                "LEFT JOIN [SupplyOrder] AS [TransportationServiceSupplyOrder] " +
                "ON [TransportationServiceSupplyOrder].TransportationServiceID = [TransportationService].ID " +
                "LEFT JOIN [Client] AS [TransportationServiceSupplyOrderClient] " +
                "ON [TransportationServiceSupplyOrderClient].ID = [TransportationServiceSupplyOrder].ClientID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (paymentTask, user, transportationService, transportationOrganization, supplyOrder, client) => {
                    if (transportationService != null) {
                        if (supplyOrder != null) {
                            supplyOrder.Client = client;

                            transportationService.SupplyOrders.Add(supplyOrder);
                        }

                        transportationService.TransportationOrganization = transportationOrganization;

                        paymentTask.User = user;
                        paymentTask.TransportationServices.Add(transportationService);

                        paymentTask.CurrentTotal = Math.Round(transportationService.NetPrice + toReturn.SupplyPaymentTasks.Sum(t => t.CurrentTotal), 2);
                        toReturn.TotalByRange += Math.Round(transportationService.NetPrice, 2);

                        toReturn.SupplyPaymentTasks.Add(paymentTask);
                        ids.Remove(paymentTask.Id);
                    }

                    return paymentTask;
                },
                new { Ids = ids }
            );

            if (!ids.Any()) return toReturn;
        }

        if (serviceTypes.Any(t => t.Equals(ServiceOrganizationType.VehicleDeliveryOrganization))) {
            _connection.Query<SupplyPaymentTask, User, VehicleDeliveryService, SupplyOrganization, SupplyOrder, Client, SupplyPaymentTask>(
                "SELECT [SupplyPaymentTask].* " +
                ", [User].* " +
                ", [VehicleDeliveryService].ID " +
                ", [VehicleDeliveryService].Created " +
                ", [VehicleDeliveryService].Deleted " +
                ", [VehicleDeliveryService].FromDate " +
                ", [VehicleDeliveryService].GrossPrice " +
                ", [VehicleDeliveryService].IsActive " +
                ", [VehicleDeliveryService].IsSealAndSignatureVerified " +
                ", [VehicleDeliveryService].Name " +
                ",( " +
                "CASE WHEN [SupplyPaymentTask].TaskStatus = 0 THEN ROUND((0 - [VehicleDeliveryService].NetPrice), 2) ELSE ROUND([VehicleDeliveryService].NetPrice, 2) END " +
                ") AS [NetPrice] " +
                ", [VehicleDeliveryService].NetUID " +
                ", [VehicleDeliveryService].Number " +
                ", [VehicleDeliveryService].SupplyPaymentTaskID " +
                ", [VehicleDeliveryService].Updated " +
                ", [VehicleDeliveryService].UserID " +
                ", [VehicleDeliveryService].Vat " +
                ", [VehicleDeliveryService].VatPercent " +
                ", [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
                ", [VehicleDeliveryOrganization].* " +
                ", [VehicleDeliveryServiceSupplyOrder].* " +
                ", [VehicleDeliveryServiceSupplyOrderClient].* " +
                "FROM [SupplyPaymentTask] " +
                "LEFT JOIN [User] " +
                "ON [User].ID = [SupplyPaymentTask].UserID " +
                "LEFT JOIN [VehicleDeliveryService] " +
                "ON [VehicleDeliveryService].SupplyPaymentTaskID = [SupplyPaymentTask].ID " +
                "LEFT JOIN [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
                "ON [VehicleDeliveryOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
                "LEFT JOIN [SupplyOrder] AS [VehicleDeliveryServiceSupplyOrder] " +
                "ON [VehicleDeliveryServiceSupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
                "LEFT JOIN [Client] AS [VehicleDeliveryServiceSupplyOrderClient] " +
                "ON [VehicleDeliveryServiceSupplyOrderClient].ID = [VehicleDeliveryServiceSupplyOrder].ClientID " +
                "WHERE [SupplyPaymentTask].ID IN @Ids",
                (paymentTask, user, vehicleDeliveryService, vehicleDeliveryOrganization, supplyOrder, client) => {
                    if (vehicleDeliveryService != null) {
                        if (supplyOrder != null) {
                            supplyOrder.Client = client;

                            vehicleDeliveryService.SupplyOrders.Add(supplyOrder);
                        }

                        vehicleDeliveryService.VehicleDeliveryOrganization = vehicleDeliveryOrganization;

                        paymentTask.User = user;
                        paymentTask.VehicleDeliveryServices.Add(vehicleDeliveryService);

                        paymentTask.CurrentTotal = Math.Round(vehicleDeliveryService.NetPrice + toReturn.SupplyPaymentTasks.Sum(t => t.CurrentTotal), 2);
                        toReturn.TotalByRange += Math.Round(vehicleDeliveryService.NetPrice, 2);

                        toReturn.SupplyPaymentTasks.Add(paymentTask);
                        ids.Remove(paymentTask.Id);
                    }

                    return paymentTask;
                },
                new { Ids = ids }
            );

            if (!ids.Any()) return toReturn;
        }

        return toReturn;
    }
}