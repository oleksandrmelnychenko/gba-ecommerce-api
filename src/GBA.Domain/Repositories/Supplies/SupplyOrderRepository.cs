using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.Supplies.SupplyOrderModels;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyOrderRepository : ISupplyOrderRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public dynamic GetTotalsOnSupplyOrderItemsBySupplyOrderNetId(Guid netId) {
        return _connection.Query<dynamic>(
            "SELECT " +
            "COUNT([SupplyOrderItem].ID) AS TotalItemsCount " +
            ",ROUND(SUM([SupplyOrderItem].Qty), 2) AS TotalQuantity " +
            ",ROUND(SUM([SupplyOrderItem].UnitPrice * [SupplyOrderItem].Qty), 2) AS TotalPrice " +
            ",ROUND(SUM([SupplyOrderItem].TotalAmount), 2) AS TotalAmount " +
            ",ROUND(SUM([SupplyOrderItem].NetWeight * [SupplyOrderItem].Qty), 3) AS TotalNetWeight " +
            ",ROUND(SUM([SupplyOrderItem].GrossWeight * [SupplyOrderItem].Qty), 3) AS TotalGrossWeight " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrder].ID = [SupplyOrderItem].SupplyOrderID " +
            "WHERE [SupplyOrder].NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public dynamic GetTotalsByNetId(Guid netId) {
        return _connection.Query<dynamic>(
            "SELECT " +
            "(" +
            "ROUND(" +
            //"(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([ContainerService].NetPrice) ELSE 0 END FROM [ContainerService] WHERE [ContainerService].ID = [SupplyOrder].ContainerServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([PortWorkService].NetPrice) ELSE 0 END FROM [PortWorkService] WHERE [PortWorkService].ID = [SupplyOrder].PortWorkServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([TransportationService].NetPrice) ELSE 0 END FROM [TransportationService] WHERE [TransportationService].ID = [SupplyOrder].TransportationServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([CustomAgencyService].NetPrice) ELSE 0 END FROM [CustomAgencyService] WHERE [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([PortCustomAgencyService].NetPrice) ELSE 0 END FROM [PortCustomAgencyService] WHERE [PortCustomAgencyService].ID = [SupplyOrder].PortCustomAgencyServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([PlaneDeliveryService].NetPrice) ELSE 0 END FROM [PlaneDeliveryService] WHERE [PlaneDeliveryService].ID = [SupplyOrder].PlaneDeliveryServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([VehicleDeliveryService].NetPrice) ELSE 0 END FROM [VehicleDeliveryService] WHERE [VehicleDeliveryService].ID = [SupplyOrder].VehicleDeliveryServiceID) + " +
            "(SELECT CASE WHEN (COUNT(*)) > 0 THEN SUM([CustomService].NetPrice) ELSE 0 END FROM [CustomService] WHERE [CustomService].SupplyOrderID = [SupplyOrder].ID)" +
            ",2)" +
            ") AS TotalNetPrice, " +
            "(" +
            "ROUND(" +
            //"(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([ContainerService].GrossPrice) ELSE 0 END FROM [ContainerService] WHERE [ContainerService].ID = [SupplyOrder].ContainerServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([PortWorkService].GrossPrice) ELSE 0 END FROM [PortWorkService] WHERE [PortWorkService].ID = [SupplyOrder].PortWorkServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([TransportationService].GrossPrice) ELSE 0 END FROM [TransportationService] WHERE [TransportationService].ID = [SupplyOrder].TransportationServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([CustomAgencyService].GrossPrice) ELSE 0 END FROM [CustomAgencyService] WHERE [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([PortCustomAgencyService].GrossPrice) ELSE 0 END FROM [PortCustomAgencyService] WHERE [PortCustomAgencyService].ID = [SupplyOrder].PortCustomAgencyServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([PlaneDeliveryService].GrossPrice) ELSE 0 END FROM [PlaneDeliveryService] WHERE [PlaneDeliveryService].ID = [SupplyOrder].PlaneDeliveryServiceID) + " +
            "(SELECT CASE WHEN COUNT(*) > 0 THEN MAX([VehicleDeliveryService].GrossPrice) ELSE 0 END FROM [VehicleDeliveryService] WHERE [VehicleDeliveryService].ID = [SupplyOrder].VehicleDeliveryServiceID) + " +
            "(SELECT CASE WHEN (COUNT(*)) > 0 THEN SUM([CustomService].GrossPrice) ELSE 0 END FROM [CustomService] WHERE [CustomService].SupplyOrderID = [SupplyOrder].ID)" +
            ",2)" +
            ") AS TotalGrossPrice, " +
            "(" +
            "ROUND((SELECT SUM([SupplyOrderItem].NetWeight) FROM [SupplyOrderItem]), 2)" +
            ") AS TotalNetWeight, " +
            "(" +
            "ROUND((SELECT SUM([SupplyOrderItem].GrossWeight) FROM [SupplyOrderItem]), 2)" +
            ") AS TotalGrossWeight " +
            "FROM [SupplyOrder] " +
            "WHERE [SupplyOrder].NetUID = @NetId",
            new { NetId = netId }
        ).Single();
    }

    public dynamic GetNearestSupplyArrivalByProductNetId(Guid netId) {
        return _connection.Query<dynamic>(
            "SELECT TOP(1) [SupplyOrder].OrderArrivedDate AS [OrderArrivedDate] " +
            ",[SupplyOrderItem].Qty AS [Qty] " +
            ",[SupplyOrder].NetUID AS [NetUID] " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyOrderItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderItem].ProductID " +
            "WHERE [SupplyOrder].IsOrderShipped = 1 " +
            "AND [SupplyOrder].IsOrderArrived = 0 " +
            "AND [Product].NetUID = @NetId " +
            "AND [SupplyOrder].OrderArrivedDate IS NOT NULL " +
            "ORDER BY [SupplyOrder].OrderArrivedDate DESC",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public long GetIdByNetId(Guid supplyOrderNetId) {
        return _connection.Query<long>(
            "SELECT [SupplyOrder].[ID] FROM [SupplyOrder] " +
            "WHERE [SupplyOrder].[NetUID] = @NetId; ",
            new { NetId = supplyOrderNetId }).Single();
    }

    public long Add(SupplyOrder supplyOrder) {
        return _connection.Query<long>(
            "INSERT INTO SupplyOrder (ResponsibleId, IsOrderArrived, OrderArrivedDate, VechicalArrived, PlaneArrived, ShipArrived, CompleteDate, OrderShippedDate, " +
            "IsOrderShipped, IsCompleted, TransportationType, GrossPrice, NetPrice, ClientID, OrganizationID, Qty, SupplyOrderNumberID, SupplyProFormID, " +
            "PortWorkServiceID, TransportationServiceID, DateFrom, CustomAgencyServiceId, PortCustomAgencyServiceId, PlaneDeliveryServiceId, " +
            "VehicleDeliveryServiceId, IsDocumentSet, IsPlaced, Comment, ClientAgreementId, IsGrossPricesCalculated, IsPartiallyPlaced, IsFullyPlaced, " +
            "IsOrderInsidePoland, AdditionalAmount, AdditionalPercent, AdditionalPaymentCurrencyId, AdditionalPaymentFromDate, Updated) " +
            "VALUES(@ResponsibleId, @IsOrderArrived, @OrderArrivedDate, @VechicalArrived, @PlaneArrived, @ShipArrived, @CompleteDate, @OrderShippedDate, @IsOrderShipped, " +
            "@IsCompleted, @TransportationType, @GrossPrice, @NetPrice, @ClientID, @OrganizationID, @Qty, @SupplyOrderNumberID, @SupplyProFormID, " +
            "@PortWorkServiceID, @TransportationServiceID, @DateFrom, @CustomAgencyServiceId, @PortCustomAgencyServiceId, @PlaneDeliveryServiceId, " +
            "@VehicleDeliveryServiceId, @IsDocumentSet, 0, @Comment, @ClientAgreementId, 0, 0, 0, @IsOrderInsidePoland, 0.00, 0.00, NULL, NULL, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyOrder
        ).Single();
    }

    public List<SupplyOrder> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<SupplyOrder> toReturn = new();

        string sqlExpression =
            ";WITH " +
            "[SupplyOrder_All_SearchCTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID DESC) AS RowNumber " +
            ", ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [SupplyOrder].ID " +
            "FROM [SupplyOrder] ";

        if (clientNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [Client] " +
                "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression +=
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrder].SupplyOrderNumberID = [SupplyOrderNumber].ID " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderContainerService] " +
            "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
            "LEFT JOIN [ContainerService] " +
            "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
            "LEFT JOIN [BillOfLadingDocument] " +
            "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "LEFT JOIN [CustomService] " +
            "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
            "LEFT JOIN [PlaneDeliveryService] " +
            "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "LEFT JOIN [PortWorkService] " +
            "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
            "LEFT JOIN [TransportationService] " +
            "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
            "LEFT JOIN [VehicleDeliveryService] " +
            "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
            "LEFT JOIN [SupplyInvoice] [SupplyInvoiceInvoice] " +
            "ON [SupplyInvoiceInvoice].[SupplyOrderID] = [SupplyOrder].[ID] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].[SupplyInvoiceID] = [SupplyInvoiceInvoice].[ID] " +
            "LEFT JOIN [Product] " +
            "ON [SupplyInvoiceOrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "WHERE [SupplyOrder].Deleted = 0 " +
            "AND [SupplyOrder].Created >= @From " +
            "AND [SupplyOrder].Created <= @To " +
            "AND [Organization].Culture = N'pl' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            "AND ( " +
            "( " +
            "[Product].Name like '%' + @Value + '%' " +
            "OR " +
            "[Product].Description like '%' + @Value + '%' " +
            "OR " +
            "[Product].VendorCode like '%' + @Value + '%' " +
            "OR " +
            "[Product].MainOriginalNumber like '%' + @Value + '%' " +
            ") " +
            "OR " +
            "[SupplyOrderNumber].Number like '%' + @Value + '%' " +
            "OR " +
            "[SupplyProForm].Number like '%' + @Value + '%' " +
            "OR " +
            "[SupplyInvoice].Number like '%' + @Value + '%' " +
            "OR " +
            "[SupplyInvoice].Number like '%' + @Value + '%' " +
            "OR " +
            "[BillOfLadingDocument].Number like '%' + @Value + '%' " +
            "OR " +
            "[CustomAgencyService].Number like '%' + @Value + '%' " +
            "OR " +
            "[CustomService].Number like '%' + @Value + '%' " +
            "OR " +
            "[PlaneDeliveryService].Number like '%' + @Value + '%' " +
            "OR " +
            "[PortCustomAgencyService].Number like '%' + @Value + '%' " +
            "OR " +
            "[PortWorkService].Number like '%' + @Value + '%' " +
            "OR " +
            "[TransportationService].Number like '%' + @Value + '%' " +
            "OR " +
            "[VehicleDeliveryService].Number like '%' + @Value + '%' " +
            "OR " +
            "[ContainerService].ContainerNumber like '%' + @Value + '%' " +
            ") " +
            ") [Distincts] " +
            ") " +
            "SELECT [SupplyOrder].* " +
            ", (" +
            "SELECT STRING_AGG([AggSupplyInvoice].[Number], N', ') " +
            "FROM [SupplyInvoice] AS [AggSupplyInvoice] " +
            "WHERE [AggSupplyInvoice].SupplyOrderId = [SupplyOrder].Id " +
            "AND [AggSupplyInvoice].Deleted = 0" +
            ") [InvoiceNumbers] " +
            ", (" +
            "SELECT STRING_AGG([AggPackingList].[No], N', ') " +
            "FROM [SupplyInvoice] AS [AggSupplyInvoice] " +
            "LEFT JOIN [PackingList] AS [AggPackingList] " +
            "ON [AggPackingList].SupplyInvoiceId = [AggSupplyInvoice].Id " +
            "AND [AggPackingList].Deleted = 0 " +
            "WHERE [AggSupplyInvoice].SupplyOrderId = [SupplyOrder].Id " +
            "AND [AggSupplyInvoice].Deleted = 0" +
            ") [PackListNumbers] " +
            ", [SupplyOrderNumber].* " +
            ", [Client].* " +
            ", [SupplyOrderOrganization].* " +
            ", [SupplyOrderClientAgreement].* " +
            ", [SupplyOrderAgreement].* " +
            ", [SupplyOrderCurrency].* " +
            ", [SupplyInvoice].* " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
            "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
            "AND [SupplyOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderClientAgreement] " +
            "ON [SupplyOrderClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] AS [SupplyOrderAgreement] " +
            "ON [SupplyOrderAgreement].ID = [SupplyOrderClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [SupplyOrderCurrency] " +
            "ON [SupplyOrderCurrency].ID = [SupplyOrderAgreement].CurrencyID " +
            "AND [SupplyOrderCurrency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyInvoice].Deleted = 0 " +
            "WHERE [SupplyOrder].ID IN (" +
            "SELECT [SupplyOrder_All_SearchCTE].ID " +
            "FROM [SupplyOrder_All_SearchCTE] " +
            "WHERE [SupplyOrder_All_SearchCTE].RowNumber > @Offset " +
            "AND [SupplyOrder_All_SearchCTE].RowNumber <= @Limit + @Offset" +
            ") " +
            "ORDER BY [SupplyOrder].DateFrom DESC, [SupplyOrder].ID DESC";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(SupplyInvoice)
        };

        var props = new {
            Value = value,
            Limit = limit,
            Offset = offset,
            From = from,
            To = to,
            ClientNetId = clientNetId,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[1];
            Client client = (Client)objects[2];
            Organization supplyOrderOrganization = (Organization)objects[3];
            ClientAgreement supplyOrderClientAgreement = (ClientAgreement)objects[4];
            Agreement supplyOrderAgreement = (Agreement)objects[5];
            Currency supplyOrderCurrency = (Currency)objects[6];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[7];

            if (!toReturn.Any(o => o.Id.Equals(supplyOrder.Id))) {
                if (supplyOrderClientAgreement != null) {
                    supplyOrderAgreement.Currency = supplyOrderCurrency;

                    supplyOrderClientAgreement.Agreement = supplyOrderAgreement;
                }

                if (supplyInvoice != null) supplyOrder.SupplyInvoices.Add(supplyInvoice);

                supplyOrder.Client = client;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                supplyOrder.Organization = supplyOrderOrganization;
                supplyOrder.ClientAgreement = supplyOrderClientAgreement;

                toReturn.Add(supplyOrder);
            } else if (supplyInvoice != null) {
                toReturn.First(o => o.Id.Equals(supplyOrder.Id)).SupplyInvoices.Add(supplyInvoice);
            }

            return supplyOrder;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public List<SupplyOrder> GetAllFromSearchForUkOrganizations(
        string value,
        long limit,
        long offset,
        DateTime from,
        DateTime to,
        string supplierName,
        long? currencyId,
        Guid? clientNetId) {
        List<SupplyOrder> supplyOrders = new();

        string sqlExpression =
            ";WITH " +
            "[SupplyOrder_All_SearchCTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY DateFrom DESC) AS RowNumber " +
            ", ID " +
            ", COUNT(*) OVER() [TotalRowsQty] " +
            "FROM " +
            "( " +
            "SELECT [SupplyOrder].ID, [SupplyOrder].DateFrom " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] " +
            "ON [CurrencyView].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrder].SupplyOrderNumberID = [SupplyOrderNumber].ID " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyOrder].SupplyProFormID = [SupplyProForm].ID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderContainerService] " +
            "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
            "LEFT JOIN [ContainerService] " +
            "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
            "LEFT JOIN [BillOfLadingDocument] " +
            "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "LEFT JOIN [CustomService] " +
            "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
            "LEFT JOIN [PlaneDeliveryService] " +
            "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "LEFT JOIN [PortWorkService] " +
            "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
            "LEFT JOIN [TransportationService] " +
            "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
            "LEFT JOIN [VehicleDeliveryService] " +
            "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
            //"LEFT JOIN [SupplyOrderItem] " +
            //"ON [SupplyOrder].ID = [SupplyOrderItem].SupplyOrderID " +
            "LEFT JOIN [SupplyInvoice] [SupplyInvoiceInvoice] " +
            "ON [SupplyInvoiceInvoice].[SupplyOrderID] = [SupplyOrder].[ID] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].[SupplyInvoiceID] = [SupplyInvoiceInvoice].[ID] " +
            "LEFT JOIN [Product] " +
            "ON [SupplyInvoiceOrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "WHERE [SupplyOrder].Deleted = 0 " +
            "AND [SupplyOrder].DateFrom >= @From " +
            "AND [SupplyOrder].DateFrom <= @To " +
            "AND [Organization].Culture = N'uk' " +
            "AND [Client].FullName LIKE N'%' + @Name + N'%' ";

        if (currencyId.HasValue) sqlExpression += "AND [CurrencyView].ID = @CurrencyId ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            "AND ( " +
            "( " +
            "[Product].Name like '%' + @Value + '%' " +
            "OR " +
            "[Product].Description like '%' + @Value + '%' " +
            "OR " +
            "[Product].VendorCode like '%' + @Value + '%' " +
            "OR " +
            "[Product].MainOriginalNumber like '%' + @Value + '%' " +
            ") " +
            "OR " +
            "[SupplyOrderNumber].Number like '%' + @Value + '%' " +
            "OR " +
            "[SupplyProForm].Number like '%' + @Value + '%' " +
            "OR " +
            "[SupplyInvoice].Number like '%' + @Value + '%' " +
            "OR " +
            "[SupplyInvoice].Number like '%' + @Value + '%' " +
            "OR " +
            "[BillOfLadingDocument].Number like '%' + @Value + '%' " +
            "OR " +
            "[CustomAgencyService].Number like '%' + @Value + '%' " +
            "OR " +
            "[CustomService].Number like '%' + @Value + '%' " +
            "OR " +
            "[PlaneDeliveryService].Number like '%' + @Value + '%' " +
            "OR " +
            "[PortCustomAgencyService].Number like '%' + @Value + '%' " +
            "OR " +
            "[PortWorkService].Number like '%' + @Value + '%' " +
            "OR " +
            "[TransportationService].Number like '%' + @Value + '%' " +
            "OR " +
            "[VehicleDeliveryService].Number like '%' + @Value + '%' " +
            "OR " +
            "[ContainerService].ContainerNumber like '%' + @Value + '%' " +
            ") " +
            "GROUP BY [SupplyOrder].ID, [SupplyOrder].DateFrom" +
            ") [Distincts] " +
            ") " +
            "SELECT [SupplyOrder].* " +
            ", (SELECT TOP 1 TotalRowsQty FROM [SupplyOrder_All_SearchCTE]) AS TotalRowsQty " +
            ", ( " +
            "ROUND( " +
            "( " +
            "SELECT SUM( " +
            "ROUND([Item].UnitPrice * [Item].Qty, 2) " +
            ") " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [Item] " +
            "ON [Item].PackingListID = [PackingList].ID " +
            "AND [Item].Deleted = 0 " +
            "WHERE [SupplyInvoice].Deleted = 0 " +
            "AND [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            ") " +
            ", 2) " +
            ") AS [TotalNetPrice] " +
            ", ( " +
            "ROUND( " +
            "( " +
            "SELECT SUM( " +
            "ROUND([Item].VatAmount / CASE WHEN [Item].ExchangeRateAmount = 0 OR [Item].ExchangeRateAmount IS NULL THEN 1 ELSE [Item].ExchangeRateAmount END, 2) " +
            ") " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [Item] " +
            "ON [Item].PackingListID = [PackingList].ID " +
            "AND [Item].Deleted = 0 " +
            "WHERE [SupplyInvoice].Deleted = 0 " +
            "AND [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            ") " +
            ", 2) " +
            ") AS [TotalVat] " +
            ", ( " +
            "SELECT SUM([Item].Qty) " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [Item] " +
            "ON [Item].PackingListID = [PackingList].ID " +
            "AND [Item].Deleted = 0 " +
            "WHERE [SupplyInvoice].Deleted = 0 " +
            "AND [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            ") AS [TotalQuantity] " +
            ",[SupplyOrderNumber].* " +
            ",[User].* " +
            ",[Client].* " +
            ",[SupplyOrderOrganization].* " +
            ",[SupplyOrderClientAgreement].* " +
            ",[SupplyOrderAgreement].* " +
            ",[SupplyOrderCurrency].* " +
            ",[SupplyInvoice].* " +
            ", ( " +
            "ROUND( " +
            "( " +
            "SELECT SUM( " +
            "ROUND([Item].UnitPrice * [Item].Qty, 2) " +
            ") " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [Item] " +
            "ON [Item].PackingListID = [PackingList].ID " +
            "AND [Item].Deleted = 0 " +
            "WHERE [PackingList].Deleted = 0 " +
            "AND [PackingList].[SupplyInvoiceID] = [SupplyInvoice].ID " +
            ") " +
            ", 2) " +
            ") AS [TotalNetPrice] " +
            ", ( " +
            "ROUND( " +
            "( " +
            "SELECT SUM( " +
            "ROUND([Item].VatAmount, 2) " +
            ") " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [Item] " +
            "ON [Item].PackingListID = [PackingList].ID " +
            "AND [Item].Deleted = 0 " +
            "WHERE [PackingList].Deleted = 0 " +
            "AND [PackingList].[SupplyInvoiceID] = [SupplyInvoice].ID " +
            ") " +
            ", 2) " +
            ") AS [TotalVatAmount] " +
            ", ( " +
            "SELECT SUM([Item].Qty) " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [Item] " +
            "ON [Item].PackingListID = [PackingList].ID " +
            "AND [Item].Deleted = 0 " +
            "WHERE [PackingList].Deleted = 0 " +
            "AND [PackingList].[SupplyInvoiceID] = [SupplyInvoice].ID " +
            ") AS [TotalQuantity] " +
            ",[AdditionalPaymentCurrency].*, " +
            "ISNULL( " +
            "(SELECT TOP 1 CASE WHEN [PackingListPackageOrderItem].[ExchangeRateAmount] = 0 THEN 1 ELSE [PackingListPackageOrderItem].[ExchangeRateAmount] END " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "WHERE [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID]) " +
            ", 1) [ExchangeRate] " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [SupplyOrder].ResponsibleId " +
            "LEFT JOIN [views].[OrganizationView] AS [SupplyOrderOrganization] " +
            "ON [SupplyOrderOrganization].ID = [SupplyOrder].OrganizationID " +
            "AND [SupplyOrderOrganization].CultureCode = @Culture " +
            "LEFT JOIN [ClientAgreement] AS [SupplyOrderClientAgreement] " +
            "ON [SupplyOrderClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] AS [SupplyOrderAgreement] " +
            "ON [SupplyOrderAgreement].ID = [SupplyOrderClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [SupplyOrderCurrency] " +
            "ON [SupplyOrderCurrency].ID = [SupplyOrderAgreement].CurrencyID " +
            "AND [SupplyOrderCurrency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyInvoice].Deleted = 0 " +
            "LEFT JOIN [views].[CurrencyView] AS [AdditionalPaymentCurrency] " +
            "ON [AdditionalPaymentCurrency].ID = [SupplyOrder].AdditionalPaymentCurrencyID " +
            "AND [AdditionalPaymentCurrency].CultureCode = @Culture " +
            "WHERE [SupplyOrder].ID IN (" +
            "SELECT [SupplyOrder_All_SearchCTE].ID " +
            "FROM [SupplyOrder_All_SearchCTE] " +
            "WHERE [SupplyOrder_All_SearchCTE].RowNumber > @Offset " +
            "AND [SupplyOrder_All_SearchCTE].RowNumber <= @Limit + @Offset" +
            ") " +
            "ORDER BY [SupplyOrder].DateFrom DESC";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(User),
            typeof(Client),
            typeof(Organization),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(SupplyInvoice),
            typeof(Currency),
            typeof(decimal)
        };

        var props = new {
            Value = value,
            Limit = limit,
            Offset = offset,
            From = from,
            To = to,
            Name = supplierName,
            CurrencyId = currencyId,
            ClientNetId = clientNetId,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[1];
            User user = (User)objects[2];
            Client client = (Client)objects[3];
            Organization supplyOrderOrganization = (Organization)objects[4];
            ClientAgreement supplyOrderClientAgreement = (ClientAgreement)objects[5];
            Agreement supplyOrderAgreement = (Agreement)objects[6];
            Currency supplyOrderCurrency = (Currency)objects[7];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[8];
            Currency additionalPaymentCurrency = (Currency)objects[9];
            decimal exchangeRate = (decimal)objects[10];

            if (!supplyOrders.Any(o => o.Id.Equals(supplyOrder.Id))) {
                if (supplyOrderClientAgreement != null) {
                    supplyOrderAgreement.Currency = supplyOrderCurrency;

                    supplyOrderClientAgreement.Agreement = supplyOrderAgreement;
                }

                if (supplyInvoice != null) {
                    supplyInvoice.TotalValueWithVat = supplyInvoice.TotalNetPrice + supplyInvoice.TotalVatAmount / exchangeRate;

                    supplyOrder.SupplyInvoices.Add(supplyInvoice);
                }

                supplyOrder.Responsible = user;
                supplyOrder.Client = client;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                supplyOrder.Organization = supplyOrderOrganization;
                supplyOrder.AdditionalPaymentCurrency = additionalPaymentCurrency;
                supplyOrder.ClientAgreement = supplyOrderClientAgreement;

                supplyOrders.Add(supplyOrder);
            } else if (supplyInvoice != null) {
                supplyInvoice.TotalValueWithVat = supplyInvoice.TotalNetPrice + supplyInvoice.TotalVatAmount / exchangeRate;

                supplyOrders.First(o => o.Id.Equals(supplyOrder.Id)).SupplyInvoices.Add(supplyInvoice);
            }

            return supplyOrder;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props,
            splitOn: "ID,ExchangeRate"
        );

        foreach (SupplyOrder order in supplyOrders)
            order.SupplyOrderItems =
                _connection.Query<SupplyOrderItem>(
                    "SELECT * " +
                    "FROM [SupplyOrderItem] " +
                    "WHERE [SupplyOrderItem].Deleted = 0 " +
                    "AND [SupplyOrderItem].SupplyOrderID = @Id",
                    new { order.Id }
                ).ToList();

        return supplyOrders;
    }

    public List<SupplyOrder> GetAllFromSearchByOrderNumber(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<SupplyOrder> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [SupplyOrder].ID DESC) AS RowNumber " +
            ", [SupplyOrder].ID " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrder].SupplyOrderNumberID = [SupplyOrderNumber].ID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "WHERE [SupplyOrder].Deleted = 0 " +
            "AND [SupplyOrder].Created >= @From " +
            "AND [SupplyOrder].Created <= @To " +
            "AND [SupplyOrderNumber].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ")" +
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrder].SupplyOrderNumberID = [SupplyOrderNumber].ID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
            "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
            "AND [AgreementOrganization].CultureCode = @Culture " +
            "WHERE [SupplyOrder].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization),
            typeof(Organization)
        };

        var props = new {
            Value = value,
            Limit = limit,
            Offset = offset,
            From = from,
            To = to,
            ClientNetId = clientNetId,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[1];
            Client client = (Client)objects[2];
            ClientAgreement clientAgreement = (ClientAgreement)objects[3];
            Agreement agreement = (Agreement)objects[4];
            Currency currency = (Currency)objects[5];
            Organization organization = (Organization)objects[6];
            Organization agreementOrganization = (Organization)objects[7];

            if (toReturn.Any(o => o.Id.Equals(supplyOrder.Id))) return supplyOrder;

            agreement.Currency = currency;
            agreement.Organization = agreementOrganization;

            clientAgreement.Agreement = agreement;

            supplyOrder.Client = client;
            supplyOrder.Organization = organization;
            supplyOrder.ClientAgreement = clientAgreement;
            supplyOrder.SupplyOrderNumber = supplyOrderNumber;

            toReturn.Add(supplyOrder);

            return supplyOrder;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public List<SupplyOrder> GetAllFromSearchByProduct(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<SupplyOrder> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [SupplyOrder].ID " +
            "FROM [SupplyOrder] ";

        if (clientNetId.HasValue)
            sqlExpression +=
                "LEFT JOIN [Client] " +
                "ON [SupplyOrder].ClientID = [Client].ID ";

        sqlExpression +=
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [Product] " +
            "ON [SupplyOrderItem].ProductID = [Product].ID " +
            "WHERE [SupplyOrder].Deleted = 0 " +
            "AND [SupplyOrder].Created >= @From " +
            "AND [SupplyOrder].Created <= @To " +
            "AND ( " +
            "[Product].Name like '%' + @Value + '%' " +
            "OR " +
            "[Product].Description like '%' + @Value + '%' " +
            "OR " +
            "[Product].VendorCode like '%' + @Value + '%' " +
            "OR " +
            "[Product].MainOriginalNumber like '%' + @Value + '%' " +
            ") ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrder].SupplyOrderNumberID = [SupplyOrderNumber].ID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
            "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
            "AND [AgreementOrganization].CultureCode = @Culture " +
            "WHERE [SupplyOrder].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization),
            typeof(Organization)
        };

        var props = new {
            Value = value,
            Limit = limit,
            Offset = offset,
            From = from,
            To = to,
            ClientNetId = clientNetId,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[1];
            Client client = (Client)objects[2];
            ClientAgreement clientAgreement = (ClientAgreement)objects[3];
            Agreement agreement = (Agreement)objects[4];
            Currency currency = (Currency)objects[5];
            Organization organization = (Organization)objects[6];
            Organization agreementOrganization = (Organization)objects[7];

            if (toReturn.Any(o => o.Id.Equals(supplyOrder.Id))) return supplyOrder;

            agreement.Currency = currency;
            agreement.Organization = agreementOrganization;

            clientAgreement.Agreement = agreement;

            supplyOrder.Client = client;
            supplyOrder.Organization = organization;
            supplyOrder.ClientAgreement = clientAgreement;
            supplyOrder.SupplyOrderNumber = supplyOrderNumber;

            toReturn.Add(supplyOrder);

            return supplyOrder;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public List<SupplyOrder> GetAll() {
        return _connection.Query<SupplyOrder, Client, Organization, SupplyOrderNumber, SupplyOrder>(
            "SELECT * FROM SupplyOrder " +
            "LEFT OUTER JOIN Client " +
            "ON Client.ID = SupplyOrder.ClientID " +
            "LEFT OUTER JOIN Organization " +
            "ON Organization.ID = SupplyOrder.OrganizationID " +
            "LEFT OUTER JOIN SupplyOrderNumber " +
            "ON SupplyOrderNumber.ID = SupplyOrder.SupplyOrderNumberID",
            (supplyOrder, client, organization, supplyOrderNumber) => {
                if (client != null) supplyOrder.Client = client;

                if (organization != null) supplyOrder.Organization = organization;

                if (supplyOrderNumber != null) supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                return supplyOrder;
            }
        ).ToList();
    }


    public List<SupplyOrder> GetAll(DateTime from, DateTime to) {
        return _connection.Query<SupplyOrder, Client, Organization, SupplyOrderNumber, SupplyOrder>(
            "SELECT * FROM SupplyOrder " +
            "LEFT OUTER JOIN Client " +
            "ON Client.ID = SupplyOrder.ClientID " +
            "LEFT OUTER JOIN Organization " +
            "ON Organization.ID = SupplyOrder.OrganizationID " +
            "LEFT OUTER JOIN SupplyOrderNumber " +
            "ON SupplyOrderNumber.ID = SupplyOrder.SupplyOrderNumberID " +
            "WHERE [SupplyOrder].Created >= @From " +
            "AND [SupplyOrder].Created <= @To ",
            (supplyOrder, client, organization, supplyOrderNumber) => {
                if (client != null) supplyOrder.Client = client;

                if (organization != null) supplyOrder.Organization = organization;

                if (supplyOrderNumber != null) supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                return supplyOrder;
            },
            new { From = from, To = to }
        ).ToList();
    }

    public List<SupplyOrder> GetAllForPlacement() {
        List<SupplyOrder> toReturn = new();

        List<long> invoiceIds = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [InvoiceDocument] AS [SupplyInvoiceDocument] " +
            "ON [SupplyInvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] " +
            "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
            "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
            "ON [SupplyOrderPaymentDeliveryProtocol].UserID = [SupplyOrderPaymentDeliveryProtocolUser].ID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] " +
            "ON [SupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
            "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [SupplyInformationDeliveryProtocolKey] " +
            "ON [SupplyInformationDeliveryProtocolKey].ID = [SupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "AND [SupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [SupplyInformationDeliveryProtocolUser] " +
            "ON [SupplyInformationDeliveryProtocolUser].ID = [SupplyInformationDeliveryProtocol].UserID " +
            "WHERE [SupplyOrder].Deleted = 0 " +
            "AND [SupplyOrder].IsPlaced = 0 " +
            "AND [SupplyOrder].IsCompleted = 1 " +
            "ORDER BY [SupplyOrder].ID";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(SupplyInvoice),
            typeof(InvoiceDocument),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(User)
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            Client client = (Client)objects[1];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[2];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[3];
            InvoiceDocument supplyInvoiceDocument = (InvoiceDocument)objects[4];
            SupplyOrderPaymentDeliveryProtocol paymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[5];
            SupplyOrderPaymentDeliveryProtocolKey paymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[6];
            User supplyOrderPaymentDeliveryProtocolUser = (User)objects[7];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[8];
            User supplyPaymentTaskUser = (User)objects[9];
            SupplyInformationDeliveryProtocol informationDeliveryProtocol = (SupplyInformationDeliveryProtocol)objects[10];
            SupplyInformationDeliveryProtocolKey informationDeliveryProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[11];
            User informationDeliveryProtocolUser = (User)objects[12];

            if (toReturn.Any(o => o.Id.Equals(supplyOrder.Id))) {
                SupplyOrder fromList = toReturn.First(o => o.Id.Equals(supplyOrder.Id));

                if (supplyInvoice == null) return supplyOrder;

                if (fromList.SupplyInvoices.Any(i => i.Id.Equals(supplyInvoice.Id))) {
                    SupplyInvoice invoiceFromList = fromList.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

                    if (supplyInvoiceDocument != null && !invoiceFromList.InvoiceDocuments.Any(d => d.Id.Equals(supplyInvoiceDocument.Id)))
                        invoiceFromList.InvoiceDocuments.Add(supplyInvoiceDocument);

                    if (paymentDeliveryProtocol != null && !invoiceFromList.PaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentDeliveryProtocol.Id))) {
                        if (supplyPaymentTask != null) {
                            supplyPaymentTask.User = supplyPaymentTaskUser;

                            paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                        }

                        paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                        paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                        invoiceFromList.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
                    }

                    if (informationDeliveryProtocol == null ||
                        invoiceFromList.InformationDeliveryProtocols.Any(p => p.Id.Equals(informationDeliveryProtocol.Id)))
                        return supplyOrder;

                    informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
                    informationDeliveryProtocol.User = informationDeliveryProtocolUser;

                    invoiceFromList.InformationDeliveryProtocols.Add(informationDeliveryProtocol);
                } else {
                    if (supplyInvoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(supplyInvoiceDocument);

                    if (paymentDeliveryProtocol != null) {
                        if (supplyPaymentTask != null) {
                            supplyPaymentTask.User = supplyPaymentTaskUser;

                            paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                        }

                        paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                        paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                        supplyInvoice.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
                    }

                    if (informationDeliveryProtocol != null) {
                        informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
                        informationDeliveryProtocol.User = informationDeliveryProtocolUser;

                        supplyInvoice.InformationDeliveryProtocols.Add(informationDeliveryProtocol);
                    }

                    fromList.SupplyInvoices.Add(supplyInvoice);

                    invoiceIds.Add(supplyInvoice.Id);
                }
            } else {
                if (supplyInvoice != null) {
                    if (supplyInvoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(supplyInvoiceDocument);

                    if (paymentDeliveryProtocol != null) {
                        if (supplyPaymentTask != null) {
                            supplyPaymentTask.User = supplyPaymentTaskUser;

                            paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                        }

                        paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                        paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                        supplyInvoice.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
                    }

                    if (informationDeliveryProtocol != null) {
                        informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
                        informationDeliveryProtocol.User = informationDeliveryProtocolUser;

                        supplyInvoice.InformationDeliveryProtocols.Add(informationDeliveryProtocol);
                    }

                    supplyOrder.SupplyInvoices.Add(supplyInvoice);

                    invoiceIds.Add(supplyInvoice.Id);
                }

                supplyOrder.Client = client;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                toReturn.Add(supplyOrder);
            }

            return supplyOrder;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (!invoiceIds.Any()) return toReturn;

        var props = new { InvoiceIds = invoiceIds, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        types = new[] {
            typeof(SupplyInvoice),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductSpecification),
            typeof(User)
        };

        Func<object[], SupplyInvoice> orderItemsMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[1];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductSpecification productSpecification = (ProductSpecification)objects[5];
            User user = (User)objects[6];

            SupplyOrder orderFromList = toReturn.First(o => o.Id.Equals(supplyInvoice.SupplyOrderId));

            SupplyInvoice invoiceFromList = orderFromList.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

            if (supplyInvoiceOrderItem == null) return supplyInvoice;

            if (!invoiceFromList.SupplyInvoiceOrderItems.Any(i => i.Id.Equals(supplyInvoiceOrderItem.Id))) {
                if (productSpecification != null) {
                    productSpecification.AddedBy = user;

                    product.ProductSpecifications.Add(productSpecification);
                }

                product.MeasureUnit = measureUnit;

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;

                invoiceFromList.SupplyInvoiceOrderItems.Add(supplyInvoiceOrderItem);
            } else if (productSpecification != null) {
                SupplyInvoiceOrderItem fromList = invoiceFromList.SupplyInvoiceOrderItems.First(i => i.Id.Equals(supplyInvoiceOrderItem.Id));

                if (!fromList.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id))) fromList.Product.ProductSpecifications.Add(productSpecification);
            }

            return supplyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds ",
            types,
            orderItemsMapper,
            props
        );

        types = new[] {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(PackingListPackage),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice)
        };

        Func<object[], PackingList> packingListMapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[7];
            SupplyInvoiceOrderItem packageSupplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem packageSupplyOrderItem = (SupplyOrderItem)objects[9];
            Product packageProduct = (Product)objects[10];
            MeasureUnit packageProductMeasureUnit = (MeasureUnit)objects[11];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[12];

            SupplyOrder orderFromList = toReturn.First(o => o.Id.Equals(supplyInvoice.SupplyOrderId));

            SupplyInvoice invoiceFromList = orderFromList.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

            if (packingList == null) return null;

            if (!invoiceFromList.PackingLists.Any(p => p.Id.Equals(packingList.Id))) {
                if (packingListPackageOrderItem != null) {
                    product.MeasureUnit = measureUnit;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                }

                if (package != null) {
                    if (packageOrderItem != null) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        package.PackingListPackageOrderItems.Add(packageOrderItem);
                    }

                    if (package.Type.Equals(PackingListPackageType.Box))
                        packingList.PackingListBoxes.Add(package);
                    else
                        packingList.PackingListPallets.Add(package);
                }

                invoiceFromList.PackingLists.Add(packingList);
            } else {
                PackingList fromList = invoiceFromList.PackingLists.First(p => p.Id.Equals(packingList.Id));

                if (packingListPackageOrderItem != null)
                    if (!fromList.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        if (supplyOrderItem != null)
                            supplyOrderItem.Product = product;

                        supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                        supplyInvoiceOrderItem.Product = product;

                        packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                        fromList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                    }

                if (package == null) return packingList;

                if (package.Type.Equals(PackingListPackageType.Box)) {
                    if (fromList.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                        PackingListPackage packageFromList = fromList.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                        if (packageOrderItem == null || packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) return packingList;

                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else {
                        if (packageOrderItem != null) {
                            packageProduct.MeasureUnit = packageProductMeasureUnit;

                            if (packageSupplyOrderItem != null)
                                packageSupplyOrderItem.Product = packageProduct;

                            packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                            packageSupplyInvoiceOrderItem.Product = product;

                            packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                            package.PackingListPackageOrderItems.Add(packageOrderItem);
                        }

                        fromList.PackingListBoxes.Add(package);
                    }
                } else {
                    if (fromList.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                        PackingListPackage packageFromList = fromList.PackingListPallets.First(p => p.Id.Equals(package.Id));

                        if (packageOrderItem == null ||
                            packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) return packingList;

                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else {
                        if (packageOrderItem != null) {
                            packageProduct.MeasureUnit = packageProductMeasureUnit;

                            if (packageSupplyOrderItem != null)
                                packageSupplyOrderItem.Product = packageProduct;

                            packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                            packageSupplyInvoiceOrderItem.Product = product;

                            packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                            package.PackingListPackageOrderItems.Add(packageOrderItem);
                        }

                        fromList.PackingListPallets.Add(package);
                    }
                }
            }

            return packingList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [PackingListPackage] AS [Pallet] " +
            "ON [PackingList].ID = [Pallet].PackingListID " +
            "AND [Pallet].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [PalletPackageOrderItem] " +
            "ON [Pallet].ID = [PalletPackageOrderItem].PackingListPackageID " +
            "AND [PalletPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] AS [PalletInvoiceOrderItem] " +
            "ON [PalletPackageOrderItem].SupplyInvoiceOrderItemID = [PalletInvoiceOrderItem].ID " +
            "LEFT JOIN [SupplyOrderItem] AS [PalletOrderItem] " +
            "ON [PalletInvoiceOrderItem].SupplyOrderItemID = [PalletOrderItem].ID " +
            "LEFT JOIN [Product] AS [PalletOrderItemProduct] " +
            "ON [PalletOrderItemProduct].ID = [PalletOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [PalletOrderItemProductMeasureUnit] " +
            "ON [PalletOrderItemProductMeasureUnit].ID = [PalletOrderItemProduct].MeasureUnitID " +
            "AND [PalletOrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds " +
            "AND [PackingList].Deleted = 0 ",
            types,
            packingListMapper,
            props
        );

        _connection.Query<PackingList, InvoiceDocument, SupplyInvoice, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds " +
            "AND [PackingList].Deleted = 0",
            (packingList, document, supplyInvoice) => {
                SupplyOrder orderFromList = toReturn.First(o => o.Id.Equals(supplyInvoice.SupplyOrderId));

                SupplyInvoice invoiceFromList = orderFromList.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

                if (document != null) invoiceFromList.PackingLists.First(p => p.Id.Equals(packingList.Id)).InvoiceDocuments.Add(document);

                return packingList;
            },
            props
        );

        return toReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [SupplyOrder] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Update(SupplyOrder supplyOrder) {
        _connection.Execute(
            "UPDATE SupplyOrder " +
            "SET IsOrderArrived = @IsOrderArrived, OrderArrivedDate = @OrderArrivedDate, VechicalArrived = @VechicalArrived, PlaneArrived = @PlaneArrived, " +
            "ShipArrived = @ShipArrived, CompleteDate = @CompleteDate, OrderShippedDate = @OrderShippedDate, IsOrderShipped = @IsOrderShipped, IsCompleted = @IsCompleted, " +
            "TransportationType = @TransportationType, GrossPrice = @GrossPrice, NetPrice = @NetPrice, Comment = @Comment, " +
            "ClientID = @ClientID, OrganizationID = @OrganizationID, Qty = @Qty, SupplyOrderNumberID = @SupplyOrderNumberID, " +
            "SupplyProFormID = @SupplyProFormID, PortWorkServiceID = @PortWorkServiceID, TransportationServiceID = @TransportationServiceID, DateFrom = @DateFrom, " +
            "CustomAgencyServiceId = @CustomAgencyServiceId, PortCustomAgencyServiceId = @PortCustomAgencyServiceId, " +
            "PlaneDeliveryServiceId = @PlaneDeliveryServiceId, VehicleDeliveryServiceId = @VehicleDeliveryServiceId, " +
            "IsDocumentSet = @IsDocumentSet, IsGrossPricesCalculated = @IsGrossPricesCalculated, Updated = getutcdate(), IsApproved = @IsApproved " +
            "WHERE NetUID = @NetUID",
            supplyOrder
        );
    }

    public void UpdateAdditionalPaymentFields(SupplyOrder supplyOrder) {
        _connection.Execute(
            "UPDATE [SupplyOrder] " +
            "SET AdditionalAmount = @AdditionalAmount, AdditionalPercent = @AdditionalPercent, AdditionalPaymentFromDate = @AdditionalPaymentFromDate, " +
            "AdditionalPaymentCurrencyId = @AdditionalPaymentCurrencyId, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            supplyOrder
        );
    }

    public void SetPartiallyPlaced(long id, bool value) {
        _connection.Execute(
            "UPDATE [SupplyOrder] " +
            "SET IsPartiallyPlaced = @Value " +
            "WHERE ID = @Id",
            new { Id = id, Value = value }
        );
    }

    public void SetFullyPlaced(long id, bool value) {
        _connection.Execute(
            "UPDATE [SupplyOrder] " +
            "SET IsFullyPlaced = @Value " +
            "WHERE ID = @Id",
            new { Id = id, Value = value }
        );
    }

    public Guid GetNetIdById(long id) {
        return _connection.Query<Guid>(
            "SELECT [SupplyOrder].NetUID " +
            "FROM [SupplyOrder] " +
            "WHERE [SupplyOrder].ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyOrder GetById(long id) {
        SupplyOrder supplyOrderToReturn = null;

        string sqlExpression =
            "SELECT * FROM SupplyOrder " +
            "LEFT OUTER JOIN Client " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT OUTER JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "ON Client.ID = SupplyOrder.ClientID " +
            "LEFT OUTER JOIN Organization " +
            "ON Organization.ID = SupplyOrder.OrganizationID " +
            "LEFT OUTER JOIN SupplyOrderNumber " +
            "ON SupplyOrderNumber.ID = SupplyOrder.SupplyOrderNumberID " +
            "LEFT OUTER JOIN ResponsibilityDeliveryProtocol " +
            "ON ResponsibilityDeliveryProtocol.SupplyOrderID = SupplyOrder.ID " +
            "AND ResponsibilityDeliveryProtocol.Deleted = 0 " +
            "LEFT OUTER JOIN [User] " +
            "ON [User].ID = ResponsibilityDeliveryProtocol.UserID " +
            "LEFT OUTER JOIN [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "LEFT OUTER JOIN SupplyOrderDeliveryDocument " +
            "ON SupplyOrderDeliveryDocument.SupplyOrderID = SupplyOrder.ID " +
            "LEFT OUTER JOIN SupplyDeliveryDocument " +
            "ON SupplyDeliveryDocument.ID = SupplyOrderDeliveryDocument.SupplyDeliveryDocumentID " +
            "LEFT OUTER JOIN [User] AS [SupplyOrderDeliveryDocument.User] " +
            "ON [SupplyOrderDeliveryDocument.User].ID = SupplyOrderDeliveryDocument.UserID " +
            "LEFT JOIN [ClientAgreement] AS [SelectedClientAgreement] " +
            "ON [SelectedClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "WHERE SupplyOrder.ID = @Id";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(SupplyOrderNumber),
            typeof(ResponsibilityDeliveryProtocol),
            typeof(User),
            typeof(UserRole),
            typeof(SupplyOrderDeliveryDocument),
            typeof(SupplyDeliveryDocument),
            typeof(User),
            typeof(ClientAgreement)
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            Client client = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Organization organization = (Organization)objects[4];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[5];
            ResponsibilityDeliveryProtocol responsibilityDeliveryProtocol = (ResponsibilityDeliveryProtocol)objects[6];
            User user = (User)objects[7];
            UserRole userRole = (UserRole)objects[8];
            SupplyOrderDeliveryDocument supplyOrderDeliveryDocument = (SupplyOrderDeliveryDocument)objects[9];
            SupplyDeliveryDocument supplyDeliveryDocument = (SupplyDeliveryDocument)objects[10];
            User supplyOrderDeliveryDocumentUser = (User)objects[11];
            ClientAgreement selectedClientAgreement = (ClientAgreement)objects[12];

            if (client != null) {
                if (clientAgreement != null) {
                    clientAgreement.Agreement = agreement;
                    client.ClientAgreements.Add(clientAgreement);
                }

                supplyOrder.Client = client;
            }

            supplyOrder.ClientAgreement = selectedClientAgreement;

            if (organization != null) supplyOrder.Organization = organization;

            if (supplyOrderNumber != null) supplyOrder.SupplyOrderNumber = supplyOrderNumber;

            if (supplyOrderDeliveryDocument != null) {
                supplyOrderDeliveryDocument.SupplyDeliveryDocument = supplyDeliveryDocument;
                supplyOrderDeliveryDocument.User = supplyOrderDeliveryDocumentUser;

                supplyOrder.SupplyOrderDeliveryDocuments.Add(supplyOrderDeliveryDocument);
            }

            if (responsibilityDeliveryProtocol != null) {
                if (user != null && userRole != null) {
                    user.UserRole = userRole;
                    responsibilityDeliveryProtocol.User = user;
                }

                supplyOrder.ResponsibilityDeliveryProtocols.Add(responsibilityDeliveryProtocol);
            }


            if (supplyOrderToReturn != null) {
                if (supplyOrderDeliveryDocument != null && !supplyOrderToReturn.SupplyOrderDeliveryDocuments.Any(d => d.Id.Equals(supplyOrderDeliveryDocument.Id)))
                    supplyOrderToReturn.SupplyOrderDeliveryDocuments.Add(supplyOrderDeliveryDocument);

                if (supplyOrderToReturn.Client != null && clientAgreement != null &&
                    !supplyOrderToReturn.Client.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id)))
                    supplyOrderToReturn.Client.ClientAgreements.Add(clientAgreement);

                if (responsibilityDeliveryProtocol != null && !supplyOrderToReturn.ResponsibilityDeliveryProtocols.Any(p => p.Id.Equals(responsibilityDeliveryProtocol.Id)))
                    supplyOrderToReturn.ResponsibilityDeliveryProtocols.Add(responsibilityDeliveryProtocol);
            } else {
                supplyOrderToReturn = supplyOrder;
            }

            return supplyOrder;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);

        return supplyOrderToReturn;
    }

    public SupplyOrder GetByIdWithoutIncludes(long id) {
        return _connection.Query<SupplyOrder>(
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "WHERE [SupplyOrder].ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyOrder GetByNetIdWithOrganization(Guid netId) {
        return _connection.Query<SupplyOrder, Organization, SupplyOrder>(
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "WHERE [SupplyOrder].NetUID = @NetId",
            (supplyOrder, organization) => {
                supplyOrder.Organization = organization;

                return supplyOrder;
            },
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public SupplyOrder GetByNetIdIfExist(Guid netId) {
        return _connection.Query<SupplyOrder>(
            "SELECT * FROM SupplyOrder " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public SupplyOrder GetByIdIfExist(long id) {
        return _connection.Query<SupplyOrder>(
            "SELECT * FROM SupplyOrder " +
            "WHERE ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public SupplyOrder GetByPackingListId(long id) {
        return _connection.Query<SupplyOrder>(
            "SELECT TOP(1) [SupplyOrder].* " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].SupplyInvoiceID = [SupplyInvoice].ID " +
            "WHERE [PackingList].ID = @Id",
            new { Id = id }
        ).Single();
    }

    public SupplyOrder GetByIdWithAllIncludes(long id) {
        SupplyOrder supplyOrderToReturn =
            _connection.Query<SupplyOrder>(
                "SELECT [SupplyOrder].* " +
                "FROM [SupplyOrder] " +
                "WHERE [SupplyOrder].ID = @Id",
                new { Id = id }
            ).SingleOrDefault();

        if (supplyOrderToReturn == null) return null;

        switch (supplyOrderToReturn.TransportationType) {
            case SupplyTransportationType.Vehicle:
                supplyOrderToReturn = GetSupplyOrderWithVehicleServicesByNetId(supplyOrderToReturn.NetUid);
                break;
            case SupplyTransportationType.Ship:
                supplyOrderToReturn = GetSupplyOrderWithShipServicesByNetId(supplyOrderToReturn.NetUid);
                break;
            case SupplyTransportationType.Plane:
                supplyOrderToReturn = GetSupplyOrderWithPlaneServicesByNetId(supplyOrderToReturn.NetUid);
                break;
        }

        if (supplyOrderToReturn == null) return null;

        LoadAdditionalCollections(supplyOrderToReturn);

        supplyOrderToReturn.SupplyOrderTotals =
            _connection.Query<SupplyOrderTotals>(
                "SELECT " +
                "( " +
                "SELECT ROUND( " +
                "ISNULL( " +
                "SUM([SupplyOrderItem].NetWeight * [SupplyOrderItem].Qty) " +
                ", 0) " +
                ", 3) " +
                "FROM [SupplyOrderItem] " +
                "WHERE [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
                "AND [SupplyOrderItem].Deleted = 0 " +
                ") AS [TotalNetWeight] " +
                ", ( " +
                "SELECT ROUND( " +
                "ISNULL( " +
                "SUM([SupplyOrderItem].GrossWeight * [SupplyOrderItem].Qty) " +
                ", 0) " +
                ", 3) " +
                "FROM [SupplyOrderItem] " +
                "WHERE [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
                "AND [SupplyOrderItem].Deleted = 0 " +
                ") AS [TotalGrossWeight] " +
                ", ROUND( " +
                "dbo.GetExchangedToEuroValue( " +
                "( " +
                "SELECT " +
                "ISNULL( " +
                "SUM([SupplyOrderItem].UnitPrice * [SupplyOrderItem].Qty) " +
                ", 0) " +
                "FROM [SupplyOrderItem] " +
                "WHERE [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
                "AND [SupplyOrderItem].Deleted = 0 " +
                "), " +
                "ISNULL([Currency].ID, 0), " +
                "[SupplyOrder].DateFrom " +
                ") " +
                ", 2) AS [TotalNetPrice] " +
                ", ROUND( " +
                "( " +
                "SELECT " +
                "dbo.GetExchangedToEuroValue( " +
                "( " +
                "SELECT " +
                "ISNULL( " +
                "SUM([SupplyOrderItem].UnitPrice * [SupplyOrderItem].Qty) " +
                ", 0) " +
                "FROM [SupplyOrderItem] " +
                "WHERE [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
                "AND [SupplyOrderItem].Deleted = 0 " +
                "), " +
                "ISNULL([Currency].ID, 0), " +
                "[SupplyOrder].DateFrom " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM([SupplyOrderPolandPaymentDeliveryProtocol].GrossPrice), 0) " +
                "FROM [SupplyOrderPolandPaymentDeliveryProtocol] " +
                "WHERE [SupplyOrderPolandPaymentDeliveryProtocol].SupplyOrderID = [SupplyOrder].ID " +
                "AND [SupplyOrderPolandPaymentDeliveryProtocol].Deleted = 0 " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM( " +
                "dbo.GetExchangedToEuroValue( " +
                "ISNULL([MergedService].GrossPrice, 0), " +
                "ISNULL([Currency].ID, 0), " +
                "[MergedService].FromDate " +
                ") " +
                "), 0) " +
                "FROM [MergedService] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "WHERE [MergedService].SupplyOrderID = [SupplyOrder].ID " +
                "AND [MergedService].Deleted = 0 " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM( " +
                "dbo.GetExchangedToEuroValue( " +
                "ISNULL([CustomService].GrossPrice, 0), " +
                "ISNULL([Currency].ID, 0), " +
                "[CustomService].FromDate " +
                ") " +
                "), 0) " +
                "FROM [CustomService] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "WHERE [CustomService].SupplyOrderID = [SupplyOrder].ID " +
                "AND [CustomService].Deleted = 0 " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM( " +
                "dbo.GetExchangedToEuroValue( " +
                "ISNULL([PortWorkService].GrossPrice, 0), " +
                "ISNULL([Currency].ID, 0), " +
                "[PortWorkService].FromDate " +
                ") " +
                "), 0) " +
                "FROM [PortWorkService] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "WHERE [PortWorkService].ID = [SupplyOrder].PortWorkServiceID " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM( " +
                "dbo.GetExchangedToEuroValue( " +
                "ISNULL([TransportationService].GrossPrice, 0), " +
                "ISNULL([Currency].ID, 0), " +
                "[TransportationService].FromDate " +
                ") " +
                "), 0) " +
                "FROM [TransportationService] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "WHERE [TransportationService].ID = [SupplyOrder].TransportationServiceID " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM( " +
                "dbo.GetExchangedToEuroValue( " +
                "ISNULL([CustomAgencyService].GrossPrice, 0), " +
                "ISNULL([Currency].ID, 0), " +
                "[CustomAgencyService].FromDate " +
                ") " +
                "), 0) " +
                "FROM [CustomAgencyService] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "WHERE [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM( " +
                "dbo.GetExchangedToEuroValue( " +
                "ISNULL([PortCustomAgencyService].GrossPrice, 0), " +
                "ISNULL([Currency].ID, 0), " +
                "[PortCustomAgencyService].FromDate " +
                ") " +
                "), 0) " +
                "FROM [PortCustomAgencyService] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "WHERE [PortCustomAgencyService].ID = [SupplyOrder].PortCustomAgencyServiceID " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM( " +
                "dbo.GetExchangedToEuroValue( " +
                "ISNULL([PlaneDeliveryService].GrossPrice, 0), " +
                "ISNULL([Currency].ID, 0), " +
                "[PlaneDeliveryService].FromDate " +
                ") " +
                "), 0) " +
                "FROM [PlaneDeliveryService] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "WHERE [PlaneDeliveryService].ID = [SupplyOrder].PlaneDeliveryServiceID " +
                ") + " +
                "( " +
                "SELECT ISNULL(SUM( " +
                "dbo.GetExchangedToEuroValue( " +
                "ISNULL([VehicleDeliveryService].GrossPrice, 0), " +
                "ISNULL([Currency].ID, 0), " +
                "[VehicleDeliveryService].FromDate " +
                ") " +
                "), 0) " +
                "FROM [VehicleDeliveryService] " +
                "LEFT JOIN [SupplyOrganizationAgreement] " +
                "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                "WHERE [VehicleDeliveryService].ID = [SupplyOrder].VehicleDeliveryServiceID " +
                ") " +
                ") " +
                ", 2) AS [TotalGrossPrice] " +
                "FROM [SupplyOrder] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [Agreement].CurrencyID " +
                "WHERE [SupplyOrder].NetUID = @NetId",
                new { NetId = supplyOrderToReturn.NetUid }
            ).SingleOrDefault();

        return supplyOrderToReturn;
    }

    public SupplyOrder GetByNetId(Guid netId) {
        SupplyOrder supplyOrderToReturn = null;

        try {
            int? type = _connection.Query<int?>(
                "SELECT [SupplyOrder].TransportationType " +
                "FROM [SupplyOrder] " +
                "WHERE [SupplyOrder].NetUID = @NetId",
                new { NetId = netId }
            ).SingleOrDefault();

            if (type.HasValue)
                switch ((SupplyTransportationType)type.Value) {
                    case SupplyTransportationType.Vehicle:
                        supplyOrderToReturn = GetSupplyOrderWithVehicleServicesByNetId(netId);
                        break;
                    case SupplyTransportationType.Ship:
                        supplyOrderToReturn = GetSupplyOrderWithShipServicesByNetId(netId);
                        break;
                    case SupplyTransportationType.Plane:
                        supplyOrderToReturn = GetSupplyOrderWithPlaneServicesByNetId(netId);
                        break;
                }

            if (supplyOrderToReturn == null) return null;

            LoadAdditionalCollections(supplyOrderToReturn);

            supplyOrderToReturn.SupplyOrderTotals =
                _connection.Query<SupplyOrderTotals>(
                    "SELECT " +
                    "( " +
                    "SELECT ROUND( " +
                    "ISNULL( " +
                    "SUM([SupplyOrderItem].NetWeight * [SupplyOrderItem].Qty) " +
                    ", 0) " +
                    ", 3) " +
                    "FROM [SupplyOrderItem] " +
                    "WHERE [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
                    "AND [SupplyOrderItem].Deleted = 0 " +
                    ") AS [TotalNetWeight] " +
                    ", ( " +
                    "SELECT ROUND( " +
                    "ISNULL( " +
                    "SUM([SupplyOrderItem].GrossWeight * [SupplyOrderItem].Qty) " +
                    ", 0) " +
                    ", 3) " +
                    "FROM [SupplyOrderItem] " +
                    "WHERE [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
                    "AND [SupplyOrderItem].Deleted = 0 " +
                    ") AS [TotalGrossWeight] " +
                    ", ROUND( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "( " +
                    "SELECT " +
                    "ISNULL( " +
                    "SUM([SupplyOrderItem].UnitPrice * [SupplyOrderItem].Qty) " +
                    ", 0) " +
                    "FROM [SupplyOrderItem] " +
                    "WHERE [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
                    "AND [SupplyOrderItem].Deleted = 0 " +
                    "), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[SupplyOrder].DateFrom " +
                    ") " +
                    ", 2) AS [TotalNetPrice] " +
                    ", ROUND( " +
                    "( " +
                    "SELECT " +
                    "dbo.GetExchangedToEuroValue( " +
                    "( " +
                    "SELECT " +
                    "ISNULL( " +
                    "SUM([SupplyOrderItem].UnitPrice * [SupplyOrderItem].Qty) " +
                    ", 0) " +
                    "FROM [SupplyOrderItem] " +
                    "WHERE [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
                    "AND [SupplyOrderItem].Deleted = 0 " +
                    "), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[SupplyOrder].DateFrom " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM([SupplyOrderPolandPaymentDeliveryProtocol].GrossPrice), 0) " +
                    "FROM [SupplyOrderPolandPaymentDeliveryProtocol] " +
                    "WHERE [SupplyOrderPolandPaymentDeliveryProtocol].SupplyOrderID = [SupplyOrder].ID " +
                    "AND [SupplyOrderPolandPaymentDeliveryProtocol].Deleted = 0 " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "ISNULL([MergedService].GrossPrice, 0), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[MergedService].FromDate " +
                    ") " +
                    "), 0) " +
                    "FROM [MergedService] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "WHERE [MergedService].SupplyOrderID = [SupplyOrder].ID " +
                    "AND [MergedService].Deleted = 0 " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "ISNULL([CustomService].GrossPrice, 0), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[CustomService].FromDate " +
                    ") " +
                    "), 0) " +
                    "FROM [CustomService] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "WHERE [CustomService].SupplyOrderID = [SupplyOrder].ID " +
                    "AND [CustomService].Deleted = 0 " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "ISNULL([PortWorkService].GrossPrice, 0), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[PortWorkService].FromDate " +
                    ") " +
                    "), 0) " +
                    "FROM [PortWorkService] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "WHERE [PortWorkService].ID = [SupplyOrder].PortWorkServiceID " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "ISNULL([TransportationService].GrossPrice, 0), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[TransportationService].FromDate " +
                    ") " +
                    "), 0) " +
                    "FROM [TransportationService] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "WHERE [TransportationService].ID = [SupplyOrder].TransportationServiceID " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "ISNULL([CustomAgencyService].GrossPrice, 0), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[CustomAgencyService].FromDate " +
                    ") " +
                    "), 0) " +
                    "FROM [CustomAgencyService] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "WHERE [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "ISNULL([PortCustomAgencyService].GrossPrice, 0), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[PortCustomAgencyService].FromDate " +
                    ") " +
                    "), 0) " +
                    "FROM [PortCustomAgencyService] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "WHERE [PortCustomAgencyService].ID = [SupplyOrder].PortCustomAgencyServiceID " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "ISNULL([PlaneDeliveryService].GrossPrice, 0), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[PlaneDeliveryService].FromDate " +
                    ") " +
                    "), 0) " +
                    "FROM [PlaneDeliveryService] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "WHERE [PlaneDeliveryService].ID = [SupplyOrder].PlaneDeliveryServiceID " +
                    ") + " +
                    "( " +
                    "SELECT ISNULL(SUM( " +
                    "dbo.GetExchangedToEuroValue( " +
                    "ISNULL([VehicleDeliveryService].GrossPrice, 0), " +
                    "ISNULL([Currency].ID, 0), " +
                    "[VehicleDeliveryService].FromDate " +
                    ") " +
                    "), 0) " +
                    "FROM [VehicleDeliveryService] " +
                    "LEFT JOIN [SupplyOrganizationAgreement] " +
                    "ON [SupplyOrganizationAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                    "WHERE [VehicleDeliveryService].ID = [SupplyOrder].VehicleDeliveryServiceID " +
                    ") " +
                    ") " +
                    ", 2) AS [TotalGrossPrice] " +
                    "FROM [SupplyOrder] " +
                    "LEFT JOIN [ClientAgreement] " +
                    "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
                    "LEFT JOIN [Agreement] " +
                    "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [Agreement].CurrencyID " +
                    "WHERE [SupplyOrder].NetUID = @NetId",
                    new { NetId = netId }
                ).SingleOrDefault();

            return supplyOrderToReturn;
        } catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    public Currency GetCurrencyByOrderNetId(Guid netId) {
        return _connection.Query<Currency>(
            "SELECT " +
            "[Currency].* " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "WHERE [SupplyOrder].NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public SupplyOrder GetByNetIdForPlacement(Guid netId) {
        SupplyOrder toReturn = null;

        List<long> invoiceIds = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [InvoiceDocument] AS [SupplyInvoiceDocument] " +
            "ON [SupplyInvoiceDocument].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] " +
            "ON [SupplyInvoice].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocolKey] " +
            "ON [SupplyOrderPaymentDeliveryProtocolKey].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN [User] AS [SupplyOrderPaymentDeliveryProtocolUser] " +
            "ON [SupplyOrderPaymentDeliveryProtocol].UserID = [SupplyOrderPaymentDeliveryProtocolUser].ID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [SupplyOrderPaymentDeliveryProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [SupplyPaymentTaskUser] " +
            "ON [SupplyPaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] " +
            "ON [SupplyInformationDeliveryProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
            "LEFT JOIN [views].[SupplyInformationDeliveryProtocolKeyView] AS [SupplyInformationDeliveryProtocolKey] " +
            "ON [SupplyInformationDeliveryProtocolKey].ID = [SupplyInformationDeliveryProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "AND [SupplyInformationDeliveryProtocolKey].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [SupplyInformationDeliveryProtocolUser] " +
            "ON [SupplyInformationDeliveryProtocolUser].ID = [SupplyInformationDeliveryProtocol].UserID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber),
            typeof(SupplyInvoice),
            typeof(InvoiceDocument),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyOrderItem)
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            Client client = (Client)objects[1];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[2];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[3];
            InvoiceDocument supplyInvoiceDocument = (InvoiceDocument)objects[4];
            SupplyOrderPaymentDeliveryProtocol paymentDeliveryProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[5];
            SupplyOrderPaymentDeliveryProtocolKey paymentDeliveryProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[6];
            User supplyOrderPaymentDeliveryProtocolUser = (User)objects[7];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[8];
            User supplyPaymentTaskUser = (User)objects[9];
            SupplyInformationDeliveryProtocol informationDeliveryProtocol = (SupplyInformationDeliveryProtocol)objects[10];
            SupplyInformationDeliveryProtocolKey informationDeliveryProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[11];
            User informationDeliveryProtocolUser = (User)objects[12];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[13];

            if (toReturn != null) {
                if (supplyOrderItem != null)
                    if (!toReturn.SupplyOrderItems.Any(i => i.Id.Equals(supplyOrderItem.Id)))
                        toReturn.SupplyOrderItems.Add(supplyOrderItem);

                if (supplyInvoice == null) return supplyOrder;

                if (toReturn.SupplyInvoices.Any(i => i.Id.Equals(supplyInvoice.Id))) {
                    SupplyInvoice invoiceFromList = toReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

                    if (supplyInvoiceDocument != null && !invoiceFromList.InvoiceDocuments.Any(d => d.Id.Equals(supplyInvoiceDocument.Id)))
                        invoiceFromList.InvoiceDocuments.Add(supplyInvoiceDocument);

                    if (paymentDeliveryProtocol != null && !invoiceFromList.PaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentDeliveryProtocol.Id))) {
                        if (supplyPaymentTask != null) {
                            supplyPaymentTask.User = supplyPaymentTaskUser;

                            paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                        }

                        paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                        paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                        invoiceFromList.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
                    }

                    if (informationDeliveryProtocol == null || invoiceFromList.InformationDeliveryProtocols.Any(p => p.Id.Equals(informationDeliveryProtocol.Id)))
                        return supplyOrder;

                    informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
                    informationDeliveryProtocol.User = informationDeliveryProtocolUser;

                    invoiceFromList.InformationDeliveryProtocols.Add(informationDeliveryProtocol);
                } else {
                    if (supplyInvoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(supplyInvoiceDocument);

                    if (paymentDeliveryProtocol != null) {
                        if (supplyPaymentTask != null) {
                            supplyPaymentTask.User = supplyPaymentTaskUser;

                            paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                        }

                        paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                        paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                        supplyInvoice.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
                    }

                    if (informationDeliveryProtocol != null) {
                        informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
                        informationDeliveryProtocol.User = informationDeliveryProtocolUser;

                        supplyInvoice.InformationDeliveryProtocols.Add(informationDeliveryProtocol);
                    }

                    toReturn.SupplyInvoices.Add(supplyInvoice);

                    invoiceIds.Add(supplyInvoice.Id);
                }
            } else {
                if (supplyOrderItem != null) supplyOrder.SupplyOrderItems.Add(supplyOrderItem);

                if (supplyInvoice != null) {
                    if (supplyInvoiceDocument != null) supplyInvoice.InvoiceDocuments.Add(supplyInvoiceDocument);

                    if (paymentDeliveryProtocol != null) {
                        if (supplyPaymentTask != null) {
                            supplyPaymentTask.User = supplyPaymentTaskUser;

                            paymentDeliveryProtocol.SupplyPaymentTask = supplyPaymentTask;
                        }

                        paymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = paymentDeliveryProtocolKey;
                        paymentDeliveryProtocol.User = supplyOrderPaymentDeliveryProtocolUser;

                        supplyInvoice.PaymentDeliveryProtocols.Add(paymentDeliveryProtocol);
                    }

                    if (informationDeliveryProtocol != null) {
                        informationDeliveryProtocol.SupplyInformationDeliveryProtocolKey = informationDeliveryProtocolKey;
                        informationDeliveryProtocol.User = informationDeliveryProtocolUser;

                        supplyInvoice.InformationDeliveryProtocols.Add(informationDeliveryProtocol);
                    }

                    supplyOrder.SupplyInvoices.Add(supplyInvoice);

                    invoiceIds.Add(supplyInvoice.Id);
                }

                supplyOrder.Client = client;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                toReturn = supplyOrder;
            }

            return supplyOrder;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (!invoiceIds.Any()) return toReturn;

        var props = new { InvoiceIds = invoiceIds, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        types = new[] {
            typeof(SupplyInvoice),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductSpecification),
            typeof(User)
        };

        Func<object[], SupplyInvoice> orderItemsMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[1];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductSpecification productSpecification = (ProductSpecification)objects[5];
            User user = (User)objects[6];

            SupplyInvoice invoiceFromList = toReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

            if (supplyInvoiceOrderItem == null) return supplyInvoice;

            if (!invoiceFromList.SupplyInvoiceOrderItems.Any(i => i.Id.Equals(supplyInvoiceOrderItem.Id))) {
                if (productSpecification != null) {
                    productSpecification.AddedBy = user;

                    product.ProductSpecifications.Add(productSpecification);
                }

                product.MeasureUnit = measureUnit;

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;

                invoiceFromList.SupplyInvoiceOrderItems.Add(supplyInvoiceOrderItem);
            } else if (productSpecification != null) {
                SupplyInvoiceOrderItem fromList = invoiceFromList.SupplyInvoiceOrderItems.First(i => i.Id.Equals(supplyInvoiceOrderItem.Id));

                if (!fromList.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id))) fromList.Product.ProductSpecifications.Add(productSpecification);
            }

            return supplyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds ",
            types,
            orderItemsMapper,
            props
        );

        types = new[] {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(PackingListPackage),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice)
        };

        Func<object[], PackingList> packingListMapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[7];
            SupplyInvoiceOrderItem packageSupplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem packageSupplyOrderItem = (SupplyOrderItem)objects[9];
            Product packageProduct = (Product)objects[10];
            MeasureUnit packageProductMeasureUnit = (MeasureUnit)objects[11];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[12];

            SupplyInvoice invoiceFromList = toReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

            if (packingList == null) return null;

            if (!invoiceFromList.PackingLists.Any(p => p.Id.Equals(packingList.Id))) {
                if (packingListPackageOrderItem != null) {
                    product.MeasureUnit = measureUnit;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                }

                if (package != null) {
                    if (packageOrderItem != null) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        package.PackingListPackageOrderItems.Add(packageOrderItem);
                    }

                    if (package.Type.Equals(PackingListPackageType.Box))
                        packingList.PackingListBoxes.Add(package);
                    else
                        packingList.PackingListPallets.Add(package);
                }

                invoiceFromList.PackingLists.Add(packingList);
            } else {
                PackingList fromList = invoiceFromList.PackingLists.First(p => p.Id.Equals(packingList.Id));

                if (packingListPackageOrderItem != null)
                    if (!fromList.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        if (supplyOrderItem != null)
                            supplyOrderItem.Product = product;

                        supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                        supplyInvoiceOrderItem.Product = product;

                        packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                        fromList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                    }

                if (package == null) return packingList;

                if (package.Type.Equals(PackingListPackageType.Box)) {
                    if (fromList.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                        PackingListPackage packageFromList = fromList.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                        if (packageOrderItem == null || packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) return packingList;

                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else {
                        if (packageOrderItem != null) {
                            packageProduct.MeasureUnit = packageProductMeasureUnit;

                            if (packageSupplyOrderItem != null)
                                packageSupplyOrderItem.Product = packageProduct;

                            packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                            packageSupplyInvoiceOrderItem.Product = product;

                            packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                            package.PackingListPackageOrderItems.Add(packageOrderItem);
                        }

                        fromList.PackingListBoxes.Add(package);
                    }
                } else {
                    if (fromList.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                        PackingListPackage packageFromList = fromList.PackingListPallets.First(p => p.Id.Equals(package.Id));

                        if (packageOrderItem == null || packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) return packingList;

                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else {
                        if (packageOrderItem != null) {
                            packageProduct.MeasureUnit = packageProductMeasureUnit;

                            if (packageSupplyOrderItem != null)
                                packageSupplyOrderItem.Product = packageProduct;

                            packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                            packageSupplyInvoiceOrderItem.Product = product;

                            packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                            package.PackingListPackageOrderItems.Add(packageOrderItem);
                        }

                        fromList.PackingListPallets.Add(package);
                    }
                }
            }

            return packingList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [PackingListPackage] AS [Pallet] " +
            "ON [PackingList].ID = [Pallet].PackingListID " +
            "AND [Pallet].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [PalletPackageOrderItem] " +
            "ON [Pallet].ID = [PalletPackageOrderItem].PackingListPackageID " +
            "AND [PalletPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] AS [PalletInvoiceOrderItem] " +
            "ON [PalletPackageOrderItem].SupplyInvoiceOrderItemID = [PalletInvoiceOrderItem].ID " +
            "LEFT JOIN [SupplyOrderItem] AS [PalletOrderItem] " +
            "ON [PalletInvoiceOrderItem].SupplyOrderItemID = [PalletOrderItem].ID " +
            "LEFT JOIN [Product] AS [PalletOrderItemProduct] " +
            "ON [PalletOrderItemProduct].ID = [PalletOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [PalletOrderItemProductMeasureUnit] " +
            "ON [PalletOrderItemProductMeasureUnit].ID = [PalletOrderItemProduct].MeasureUnitID " +
            "AND [PalletOrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds " +
            "AND [PackingList].Deleted = 0 ",
            types,
            packingListMapper,
            props
        );

        _connection.Query<PackingList, InvoiceDocument, SupplyInvoice, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds " +
            "AND [PackingList].Deleted = 0",
            (packingList, document, supplyInvoice) => {
                SupplyInvoice invoiceFromList = toReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

                if (document != null) invoiceFromList.PackingLists.First(p => p.Id.Equals(packingList.Id)).InvoiceDocuments.Add(document);

                return packingList;
            },
            props
        );

        return toReturn;
    }

    public SupplyOrder GetByNetIdForDocumentUpload(Guid netId) {
        SupplyOrder toReturn = null;

        List<long> invoiceIds = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyProForm].ID = [SupplyOrder].SupplyProFormID " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(SupplyInvoice),
            typeof(SupplyProForm)
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[1];
            SupplyProForm supplyProForm = (SupplyProForm)objects[2];

            if (toReturn != null) {
                if (supplyInvoice == null || toReturn.SupplyInvoices.Any(i => i.Id.Equals(supplyInvoice.Id))) return supplyOrder;

                toReturn.SupplyInvoices.Add(supplyInvoice);

                invoiceIds.Add(supplyInvoice.Id);
            } else {
                if (supplyInvoice != null) {
                    supplyOrder.SupplyInvoices.Add(supplyInvoice);

                    invoiceIds.Add(supplyInvoice.Id);
                }

                supplyOrder.SupplyProForm = supplyProForm;

                toReturn = supplyOrder;
            }

            return supplyOrder;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (toReturn == null) return null;

        toReturn.SupplyOrderItems =
            _connection.Query<SupplyOrderItem>(
                "SELECT * " +
                "FROM [SupplyOrderItem] " +
                "WHERE Deleted = 0 " +
                "AND SupplyOrderID = @Id",
                new { toReturn.Id }
            ).ToList();

        if (!invoiceIds.Any()) return toReturn;

        var props = new { InvoiceIds = invoiceIds, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        types = new[] {
            typeof(SupplyInvoice),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductSpecification),
            typeof(User)
        };

        Func<object[], SupplyInvoice> orderItemsMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[1];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[2];
            Product product = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductSpecification productSpecification = (ProductSpecification)objects[5];
            User user = (User)objects[6];

            SupplyInvoice invoiceFromList = toReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

            if (supplyInvoiceOrderItem == null) return supplyInvoice;

            if (!invoiceFromList.SupplyInvoiceOrderItems.Any(i => i.Id.Equals(supplyInvoiceOrderItem.Id))) {
                if (productSpecification != null) {
                    productSpecification.AddedBy = user;

                    product.ProductSpecifications.Add(productSpecification);
                }

                product.MeasureUnit = measureUnit;

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;

                invoiceFromList.SupplyInvoiceOrderItems.Add(supplyInvoiceOrderItem);
            } else if (productSpecification != null) {
                SupplyInvoiceOrderItem fromList = invoiceFromList.SupplyInvoiceOrderItems.First(i => i.Id.Equals(supplyInvoiceOrderItem.Id));

                if (!fromList.Product.ProductSpecifications.Any(s => s.Id.Equals(productSpecification.Id))) fromList.Product.ProductSpecifications.Add(productSpecification);
            }

            return supplyInvoice;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [SupplyInvoiceOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds ",
            types,
            orderItemsMapper,
            props
        );

        types = new[] {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(PackingListPackage),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice)
        };

        Func<object[], PackingList> packingListMapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingListPackageOrderItem packageOrderItem = (PackingListPackageOrderItem)objects[7];
            SupplyInvoiceOrderItem packageSupplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyOrderItem packageSupplyOrderItem = (SupplyOrderItem)objects[9];
            Product packageProduct = (Product)objects[10];
            MeasureUnit packageProductMeasureUnit = (MeasureUnit)objects[11];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[12];

            SupplyInvoice invoiceFromList = toReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

            if (packingList == null) return null;

            if (!invoiceFromList.PackingLists.Any(p => p.Id.Equals(packingList.Id))) {
                if (packingListPackageOrderItem != null) {
                    product.MeasureUnit = measureUnit;

                    if (supplyOrderItem != null)
                        supplyOrderItem.Product = product;

                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    supplyInvoiceOrderItem.Product = product;

                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                }

                if (package != null) {
                    if (packageOrderItem != null) {
                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        package.PackingListPackageOrderItems.Add(packageOrderItem);
                    }

                    if (package.Type.Equals(PackingListPackageType.Box))
                        packingList.PackingListBoxes.Add(package);
                    else
                        packingList.PackingListPallets.Add(package);
                }

                invoiceFromList.PackingLists.Add(packingList);
            } else {
                PackingList fromList = invoiceFromList.PackingLists.First(p => p.Id.Equals(packingList.Id));

                if (packingListPackageOrderItem != null)
                    if (!fromList.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                        product.MeasureUnit = measureUnit;

                        if (supplyOrderItem != null)
                            supplyOrderItem.Product = product;

                        supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                        supplyInvoiceOrderItem.Product = product;

                        packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                        fromList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                    }

                if (package == null) return packingList;

                if (package.Type.Equals(PackingListPackageType.Box)) {
                    if (fromList.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                        PackingListPackage packageFromList = fromList.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                        if (packageOrderItem == null || packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) return packingList;

                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else {
                        if (packageOrderItem != null) {
                            packageProduct.MeasureUnit = packageProductMeasureUnit;

                            if (packageSupplyOrderItem != null)
                                packageSupplyOrderItem.Product = packageProduct;

                            packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                            packageSupplyInvoiceOrderItem.Product = product;

                            packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                            package.PackingListPackageOrderItems.Add(packageOrderItem);
                        }

                        fromList.PackingListBoxes.Add(package);
                    }
                } else {
                    if (fromList.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                        PackingListPackage packageFromList = fromList.PackingListPallets.First(p => p.Id.Equals(package.Id));

                        if (packageOrderItem == null || packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) return packingList;

                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                        if (packageSupplyOrderItem != null)
                            packageSupplyOrderItem.Product = packageProduct;

                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                        packageSupplyInvoiceOrderItem.Product = product;

                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                    } else {
                        if (packageOrderItem != null) {
                            packageProduct.MeasureUnit = packageProductMeasureUnit;

                            if (packageSupplyOrderItem != null)
                                packageSupplyOrderItem.Product = packageProduct;

                            packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                            packageSupplyInvoiceOrderItem.Product = product;

                            packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                            package.PackingListPackageOrderItems.Add(packageOrderItem);
                        }

                        fromList.PackingListPallets.Add(package);
                    }
                }
            }

            return packingList;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [PackingListPackage] AS [Pallet] " +
            "ON [PackingList].ID = [Pallet].PackingListID " +
            "AND [Pallet].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] AS [PalletPackageOrderItem] " +
            "ON [Pallet].ID = [PalletPackageOrderItem].PackingListPackageID " +
            "AND [PalletPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] AS [PalletInvoiceOrderItem] " +
            "ON [PalletPackageOrderItem].SupplyInvoiceOrderItemID = [PalletInvoiceOrderItem].ID " +
            "LEFT JOIN [SupplyOrderItem] AS [PalletOrderItem] " +
            "ON [PalletInvoiceOrderItem].SupplyOrderItemID = [PalletOrderItem].ID " +
            "LEFT JOIN [Product] AS [PalletOrderItemProduct] " +
            "ON [PalletOrderItemProduct].ID = [PalletOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [PalletOrderItemProductMeasureUnit] " +
            "ON [PalletOrderItemProductMeasureUnit].ID = [PalletOrderItemProduct].MeasureUnitID " +
            "AND [PalletOrderItemProductMeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds " +
            "AND [PackingList].Deleted = 0 ",
            types,
            packingListMapper,
            props
        );

        _connection.Query<PackingList, InvoiceDocument, SupplyInvoice, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "WHERE [SupplyInvoice].ID IN @InvoiceIds " +
            "AND [PackingList].Deleted = 0",
            (packingList, document, supplyInvoice) => {
                SupplyInvoice invoiceFromList = toReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

                if (document != null) invoiceFromList.PackingLists.First(p => p.Id.Equals(packingList.Id)).InvoiceDocuments.Add(document);

                return packingList;
            },
            props
        );

        return toReturn;
    }

    public double GetQtySupplyInvoiceById(long id) {
        return _connection.Query<double>(
            "SELECT COUNT(1) FROM [SupplyInvoice] " +
            "WHERE [SupplyInvoice].[SupplyOrderID] = @Id " +
            "AND [SupplyInvoice].[Deleted] = 0; ",
            new { Id = id }).Single();
    }

    public List<SupplyOrderModel> GetAllForPrint(DateTime from, DateTime to) {
        List<SupplyOrderModel> toReturn = _connection.Query<SupplyOrderModel>(
            ";WITH [TOTAL_VALUE_SUPPLY_ORDER] AS ( " +
            "SELECT " +
            "[SupplyOrder].[ID] " +
            ", ROUND(SUM([PackingListPackageOrderItem].[UnitPrice] * [PackingListPackageOrderItem].[Qty]), 2) AS [TotalNetPrice] " +
            ", SUM([PackingListPackageOrderItem].[Qty]) AS [TotalQty] " +
            ", SUM( " +
            "CASE " +
            "WHEN [SupplyInvoice].[IsFullyPlaced] = 1 " +
            "THEN 1 " +
            "ELSE 0 " +
            "END) AS [IsPlaced] " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[SupplyOrderID] = [SupplyOrder].[ID] " +
            "AND [SupplyInvoice].[Deleted] = 0 " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "AND [PackingList].[Deleted] = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
            "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
            "WHERE [SupplyOrder].[Deleted] = 0 " +
            "AND [SupplyOrder].[DateFrom] >= @From " +
            "AND [SupplyOrder].[DateFrom] <= @To " +
            "GROUP BY [SupplyOrder].[ID] " +
            ") " +
            "SELECT " +
            "[SupplyOrderNumber].[Number] " +
            ", [SupplyOrder].[Created] " +
            ", [SupplyOrder].[DateFrom] As [FromDate] " +
            ", CASE " +
            "WHEN [TOTAL_VALUE_SUPPLY_ORDER].[TotalNetPrice] IS NULL " +
            "THEN 0 " +
            "ELSE [TOTAL_VALUE_SUPPLY_ORDER].[TotalNetPrice] " +
            "END AS [TotalPrice] " +
            ", CASE " +
            "WHEN [Client].[Name] IS NULL " +
            "THEN [Client].[FullName] " +
            "ELSE [Client].[Name] " +
            "END AS [Supplier] " +
            ", [Agreement].[Name] AS [Agreement] " +
            ", [Currency].[Code] AS [Currency] " +
            ", CASE " +
            "WHEN [TOTAL_VALUE_SUPPLY_ORDER].[TotalQty] IS NULL " +
            "THEN 0 " +
            "ELSE [TOTAL_VALUE_SUPPLY_ORDER].[TotalQty] " +
            "END AS [Qty] " +
            ", [SupplyOrder].[AdditionalAmount] AS [AdditionalPrice] " +
            ", CASE " +
            "WHEN [Organization].[Name] IS NULL " +
            "THEN [Organization].[FullName] " +
            "ELSE [Organization].[Name] " +
            "END AS [Organization] " +
            ", CASE " +
            "WHEN [TOTAL_VALUE_SUPPLY_ORDER].[IsPlaced] > 0 " +
            "THEN N'Так' " +
            "ELSE N'Ні' " +
            "END [Placed] " +
            ", '' AS [Responsible] " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].[ID] = [SupplyOrder].[SupplyOrderNumberID] " +
            "LEFT JOIN [TOTAL_VALUE_SUPPLY_ORDER] " +
            "ON [TOTAL_VALUE_SUPPLY_ORDER].[ID] = [SupplyOrder].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "WHERE [SupplyOrder].[ID] IN ( " +
            "SELECT [TOTAL_VALUE_SUPPLY_ORDER].[ID] " +
            "FROM [TOTAL_VALUE_SUPPLY_ORDER] " +
            "); ",
            new { From = from, To = to }).AsList();

        if (toReturn != null)
            toReturn.AddRange(
                _connection.Query<SupplyOrderModel>(
                    ";WITH [TOTAL_VALUE_SUPPLY_ORDER] AS ( " +
                    "SELECT " +
                    "[SupplyInvoice].[ID] " +
                    ", ROUND(SUM([PackingListPackageOrderItem].[UnitPrice] * [PackingListPackageOrderItem].[Qty]), 2) AS [TotalNetPrice] " +
                    ", SUM([PackingListPackageOrderItem].[Qty]) AS [TotalQty] " +
                    ", SUM( " +
                    "CASE " +
                    "WHEN [SupplyInvoice].[IsFullyPlaced] = 1 " +
                    "THEN 1 " +
                    "ELSE 0 " +
                    "END) AS [IsPlaced] " +
                    "FROM [SupplyInvoice] " +
                    "LEFT JOIN [PackingList] " +
                    "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                    "AND [PackingList].[Deleted] = 0 " +
                    "LEFT JOIN [PackingListPackageOrderItem] " +
                    "ON [PackingListPackageOrderItem].[PackingListID] = [PackingList].[ID] " +
                    "AND [PackingListPackageOrderItem].[Deleted] = 0 " +
                    "WHERE [SupplyInvoice].[Deleted] = 0 " +
                    "AND [SupplyInvoice].[DateFrom] >= @From " +
                    "AND [SupplyInvoice].[DateFrom] <= @To " +
                    "GROUP BY [SupplyInvoice].[ID] " +
                    ") " +
                    "SELECT " +
                    "[SupplyOrder].[DateFrom] As [FromDate] " +
                    ", CASE " +
                    "WHEN [TOTAL_VALUE_SUPPLY_ORDER].[TotalNetPrice] IS NULL " +
                    "THEN 0 " +
                    "ELSE [TOTAL_VALUE_SUPPLY_ORDER].[TotalNetPrice] " +
                    "END AS [TotalPrice] " +
                    ", [SupplyInvoice].[Number] AS [InvNumber] " +
                    ", [SupplyInvoice].[DateFrom] AS [InvDate] " +
                    ", CASE " +
                    "WHEN [TOTAL_VALUE_SUPPLY_ORDER].[TotalQty] IS NULL " +
                    "THEN 0 " +
                    "ELSE [TOTAL_VALUE_SUPPLY_ORDER].[TotalQty] " +
                    "END AS [Qty] " +
                    ", '' AS [Organization] " +
                    ", CASE " +
                    "WHEN [TOTAL_VALUE_SUPPLY_ORDER].[IsPlaced] > 0 " +
                    "THEN N'Так' " +
                    "ELSE N'Ні' " +
                    "END [Placed] " +
                    "FROM [SupplyInvoice] " +
                    "LEFT JOIN [SupplyOrder] " +
                    "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                    "LEFT JOIN [TOTAL_VALUE_SUPPLY_ORDER] " +
                    "ON [TOTAL_VALUE_SUPPLY_ORDER].[ID] = [SupplyInvoice].[ID] " +
                    "WHERE [SupplyInvoice].[ID] IN ( " +
                    "SELECT [TOTAL_VALUE_SUPPLY_ORDER].[ID] " +
                    "FROM [TOTAL_VALUE_SUPPLY_ORDER] " +
                    "); ",
                    new { From = from, To = to }).AsList()
            );
        else
            toReturn = new List<SupplyOrderModel>();

        return toReturn;
    }

    public Currency GetCurrencyByInvoiceId(long invoiceId) {
        return _connection.Query<Currency>(
            "SELECT TOP 1 [Currency].* FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [SupplyInvoice].[ID] = @Id ",
            new { Id = invoiceId }).FirstOrDefault();
    }

    public SupplyInvoice GetInvoiceByPackingListPackageOrderItemId(long id) {
        return _connection.Query<SupplyInvoice>(
            "SELECT TOP 1 [SupplyInvoice].* FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "WHERE [PackingListPackageOrderItem].[ID] = @Id ",
            new { Id = id }).FirstOrDefault();
    }

    public Currency GetCurrencyByInvoice(long invoiceId) {
        return _connection.Query<Currency>(
            "SELECT [Currency].* FROM [SupplyInvoice] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [SupplyInvoice].[ID] = @Id ",
            new { Id = invoiceId }).FirstOrDefault();
    }

    private void LoadAdditionalCollections(SupplyOrder supplyOrderToReturn) {
        foreach (SupplyInvoice supplyInvoice in supplyOrderToReturn.SupplyInvoices)
            supplyInvoice.MergedSupplyInvoices =
                _connection.Query<SupplyInvoice>(
                    "SELECT * " +
                    "FROM [SupplyInvoice] " +
                    "WHERE [SupplyInvoice].RootSupplyInvoiceId = @Id",
                    new { supplyInvoice.Id }).ToList();

        string sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyInvoice].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] AS [InvoicePaymentProtocol] " +
            "ON [InvoicePaymentProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [InvoicePaymentProtocol].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [SupplyOrderPaymentDeliveryProtocolKey].ID " +
            ",[SupplyOrderPaymentDeliveryProtocolKey].[Key] " +
            "FROM [SupplyOrderPaymentDeliveryProtocolKey]" +
            ") AS [InvoicePaymentProtocolKey] " +
            "ON [InvoicePaymentProtocolKey].ID = [InvoicePaymentProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [InvoicePaymentProtocolUser] " +
            "ON [InvoicePaymentProtocolUser].ID = [InvoicePaymentProtocol].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [InvoicePaymentProtocolTask] " +
            "ON [InvoicePaymentProtocolTask].ID = [InvoicePaymentProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [InvoicePaymentProtocolTaskUser] " +
            "ON [InvoicePaymentProtocolTaskUser].ID = [InvoicePaymentProtocolTask].UserID " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [InvoiceInformationProtocol] " +
            "ON [InvoiceInformationProtocol].SupplyInvoiceID = [SupplyInvoice].ID " +
            "AND [InvoiceInformationProtocol].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKey].ID " +
            ",[SupplyInformationDeliveryProtocolKey].[Key] " +
            "FROM [SupplyInformationDeliveryProtocolKey] " +
            ") AS [InvoiceInformationProtocolKey] " +
            "ON [InvoiceInformationProtocolKey].ID = [InvoiceInformationProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKeyTranslation].ID, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].CultureCode, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].[Key], " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID " +
            "FROM [SupplyInformationDeliveryProtocolKeyTranslation] " +
            ") AS [InvoiceInformationProtocolKeyTranslation] " +
            "ON [InvoiceInformationProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID = [InvoiceInformationProtocolKey].ID " +
            "AND [InvoiceInformationProtocolKeyTranslation].CultureCode = @Culture " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [InvoiceInformationProtocolUser] " +
            "ON [InvoiceInformationProtocolUser].ID = [InvoiceInformationProtocol].UserID " +
            "LEFT JOIN [ResponsibilityDeliveryProtocol] " +
            "ON [ResponsibilityDeliveryProtocol].SupplyOrderID = [SupplyOrder].ID " +
            "AND [ResponsibilityDeliveryProtocol].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ResponsibilityProtocolUser] " +
            "ON [ResponsibilityProtocolUser].ID = [ResponsibilityDeliveryProtocol].UserID " +
            "LEFT JOIN [PackingListDocument] " +
            "ON [PackingListDocument].SupplyOrderID = [SupplyOrder].ID " +
            "AND [PackingListDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderDeliveryDocument] " +
            "ON [SupplyOrderDeliveryDocument].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyOrderDeliveryDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyDeliveryDocument] " +
            "ON [SupplyDeliveryDocument].ID = [SupplyOrderDeliveryDocument].SupplyDeliveryDocumentID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [SupplyOrderDeliveryDocumentUser] " +
            "ON [SupplyOrderDeliveryDocumentUser].ID = [SupplyOrderDeliveryDocument].UserID " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [SupplyOrderInformationProtocol] " +
            "ON [SupplyOrderInformationProtocol].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyOrderInformationProtocol].SupplyInvoiceID IS NULL " +
            "AND [SupplyOrderInformationProtocol].SupplyProFormID IS NULL " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKey].ID " +
            ",[SupplyInformationDeliveryProtocolKey].[Key] " +
            "FROM [SupplyInformationDeliveryProtocolKey]" +
            ") AS [SupplyOrderInformationProtocolKey] " +
            "ON [SupplyOrderInformationProtocolKey].ID = [SupplyOrderInformationProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKeyTranslation].ID, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].CultureCode, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].[Key], " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID " +
            "FROM [SupplyInformationDeliveryProtocolKeyTranslation] " +
            ") AS [SupplyOrderInformationProtocolKeyTranslation] " +
            "ON [SupplyOrderInformationProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID = [SupplyOrderInformationProtocolKey].ID " +
            "AND [SupplyOrderInformationProtocolKeyTranslation].CultureCode = @Culture " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [SupplyOrderInformationProtocolUser] " +
            "ON [SupplyOrderInformationProtocolUser].ID = [SupplyOrderInformationProtocol].UserID " +
            "LEFT JOIN [SupplyOrderPolandPaymentDeliveryProtocol] AS [SupplyOrderPaymentProtocol] " +
            "ON [SupplyOrderPaymentProtocol].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyOrderPaymentProtocol].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [SupplyOrderPaymentDeliveryProtocolKey].ID " +
            ",[SupplyOrderPaymentDeliveryProtocolKey].[Key] " +
            "FROM [SupplyOrderPaymentDeliveryProtocolKey] " +
            ") AS [SupplyOrderPaymentProtocolKey] " +
            "ON [SupplyOrderPaymentProtocolKey].ID = [SupplyOrderPaymentProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN ( " +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User] " +
            ") AS [SupplyOrderPaymentProtocolUser] " +
            "ON [SupplyOrderPaymentProtocolUser].ID = [SupplyOrderPaymentProtocol].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [SupplyOrderPaymentProtocolTask] " +
            "ON [SupplyOrderPaymentProtocolTask].ID = [SupplyOrderPaymentProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [SupplyOrderPaymentProtocolTaskUser] " +
            "ON [SupplyOrderPaymentProtocolTaskUser].ID = [SupplyOrderPaymentProtocolTask].UserID " +
            "LEFT JOIN [CreditNoteDocument] " +
            "ON [CreditNoteDocument].SupplyOrderID = [SupplyOrder].ID " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(SupplyInvoice),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(SupplyInformationDeliveryProtocolKeyTranslation),
            typeof(User),
            typeof(ResponsibilityDeliveryProtocol),
            typeof(User),
            typeof(PackingListDocument),
            typeof(SupplyOrderDeliveryDocument),
            typeof(SupplyDeliveryDocument),
            typeof(User),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(SupplyInformationDeliveryProtocolKeyTranslation),
            typeof(User),
            typeof(SupplyOrderPolandPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(CreditNoteDocument)
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[1];
            SupplyOrderPaymentDeliveryProtocol invoicePaymentProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[2];
            SupplyOrderPaymentDeliveryProtocolKey invoicePaymentProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[3];
            User invoicePaymentProtocolUser = (User)objects[4];
            SupplyPaymentTask invoicePaymentProtocolTask = (SupplyPaymentTask)objects[5];
            User invoicePaymentProtocolTaskUser = (User)objects[6];
            SupplyInformationDeliveryProtocol invoiceInformationProtocol = (SupplyInformationDeliveryProtocol)objects[7];
            SupplyInformationDeliveryProtocolKey invoiceInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[8];
            SupplyInformationDeliveryProtocolKeyTranslation invoiceInformationProtocolKeyTranslation = (SupplyInformationDeliveryProtocolKeyTranslation)objects[9];
            User invoiceInformationProtocolUser = (User)objects[10];
            ResponsibilityDeliveryProtocol responsibilityDeliveryProtocol = (ResponsibilityDeliveryProtocol)objects[11];
            User responsibilityDeliveryProtocolUser = (User)objects[12];
            PackingListDocument packingListDocument = (PackingListDocument)objects[13];
            SupplyOrderDeliveryDocument supplyOrderDeliveryDocument = (SupplyOrderDeliveryDocument)objects[14];
            SupplyDeliveryDocument supplyDeliveryDocument = (SupplyDeliveryDocument)objects[15];
            User supplyOrderDeliveryDocumentUser = (User)objects[16];
            SupplyInformationDeliveryProtocol supplyOrderInformationProtocol = (SupplyInformationDeliveryProtocol)objects[17];
            SupplyInformationDeliveryProtocolKey supplyOrderInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[18];
            SupplyInformationDeliveryProtocolKeyTranslation supplyOrderInformationProtocolKeyTranslation = (SupplyInformationDeliveryProtocolKeyTranslation)objects[19];
            User supplyOrderInformationProtocolUser = (User)objects[20];
            SupplyOrderPolandPaymentDeliveryProtocol supplyOrderPolandPaymentDeliveryProtocol = (SupplyOrderPolandPaymentDeliveryProtocol)objects[21];
            SupplyOrderPaymentDeliveryProtocolKey supplyOrderPaymentProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[22];
            User supplyOrderPaymentProtocolUser = (User)objects[23];
            SupplyPaymentTask supplyOrderPaymentProtocolTask = (SupplyPaymentTask)objects[24];
            User supplyOrderPaymentProtocolTaskUser = (User)objects[25];
            CreditNoteDocument creditNoteDocument = (CreditNoteDocument)objects[26];

            if (supplyOrderPolandPaymentDeliveryProtocol != null &&
                !supplyOrderToReturn.SupplyOrderPolandPaymentDeliveryProtocols.Any(p => p.Id.Equals(supplyOrderPolandPaymentDeliveryProtocol.Id))) {
                if (supplyOrderPaymentProtocolTask != null) {
                    supplyOrderPaymentProtocolTask.User = supplyOrderPaymentProtocolTaskUser;

                    supplyOrderPolandPaymentDeliveryProtocol.SupplyPaymentTask = supplyOrderPaymentProtocolTask;
                }

                supplyOrderPolandPaymentDeliveryProtocol.User = supplyOrderPaymentProtocolUser;
                supplyOrderPolandPaymentDeliveryProtocol.SupplyOrderPaymentDeliveryProtocolKey = supplyOrderPaymentProtocolKey;

                supplyOrderToReturn.SupplyOrderPolandPaymentDeliveryProtocols.Add(supplyOrderPolandPaymentDeliveryProtocol);
            }

            if (supplyOrderInformationProtocol != null && !supplyOrderToReturn.InformationDeliveryProtocols.Any(p => p.Id.Equals(supplyOrderInformationProtocol.Id))) {
                supplyOrderInformationProtocol.User = supplyOrderInformationProtocolUser;
                supplyOrderInformationProtocolKey.Key = supplyOrderInformationProtocolKeyTranslation?.Key ?? supplyOrderInformationProtocolKey.Key;
                supplyOrderInformationProtocol.SupplyInformationDeliveryProtocolKey = supplyOrderInformationProtocolKey;

                supplyOrderToReturn.InformationDeliveryProtocols.Add(supplyOrderInformationProtocol);
            }

            if (supplyOrderDeliveryDocument != null && !supplyOrderToReturn.SupplyOrderDeliveryDocuments.Any(d => d.Id.Equals(supplyOrderDeliveryDocument.Id))) {
                supplyOrderDeliveryDocument.SupplyDeliveryDocument = supplyDeliveryDocument;
                supplyOrderDeliveryDocument.User = supplyOrderDeliveryDocumentUser;

                supplyOrderToReturn.SupplyOrderDeliveryDocuments.Add(supplyOrderDeliveryDocument);
            }

            if (packingListDocument != null && !supplyOrderToReturn.PackingListDocuments.Any(d => d.Id.Equals(packingListDocument.Id)))
                supplyOrderToReturn.PackingListDocuments.Add(packingListDocument);

            if (responsibilityDeliveryProtocol != null && !supplyOrderToReturn.ResponsibilityDeliveryProtocols.Any(r => r.Id.Equals(responsibilityDeliveryProtocol.Id))) {
                responsibilityDeliveryProtocol.User = responsibilityDeliveryProtocolUser;

                supplyOrderToReturn.ResponsibilityDeliveryProtocols.Add(responsibilityDeliveryProtocol);
            }

            if (creditNoteDocument != null && !supplyOrderToReturn.CreditNoteDocuments.Any(d => d.Id.Equals(creditNoteDocument.Id)))
                supplyOrderToReturn.CreditNoteDocuments.Add(creditNoteDocument);

            if (supplyInvoice == null) return supplyOrder;

            if (!supplyOrderToReturn.SupplyInvoices.Any(i => i.Id.Equals(supplyInvoice.Id))) {
                if (invoicePaymentProtocol != null) {
                    if (invoicePaymentProtocolTask != null) {
                        invoicePaymentProtocolTask.User = invoicePaymentProtocolTaskUser;

                        invoicePaymentProtocol.SupplyPaymentTask = invoicePaymentProtocolTask;
                    }

                    invoicePaymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = invoicePaymentProtocolKey;
                    invoicePaymentProtocol.User = invoicePaymentProtocolUser;

                    supplyInvoice.PaymentDeliveryProtocols.Add(invoicePaymentProtocol);
                }

                if (invoiceInformationProtocol != null) {
                    invoiceInformationProtocol.User = invoiceInformationProtocolUser;
                    invoiceInformationProtocolKey.Key = invoiceInformationProtocolKeyTranslation?.Key ?? invoiceInformationProtocolKey.Key;
                    invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                    supplyInvoice.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
                }

                supplyOrderToReturn.SupplyInvoices.Add(supplyInvoice);
            } else {
                SupplyInvoice invoiceFromList = supplyOrderToReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

                if (invoicePaymentProtocol != null &&
                    !invoiceFromList.PaymentDeliveryProtocols.Any(p => p.Id.Equals(invoicePaymentProtocol.Id))) {
                    if (invoicePaymentProtocolTask != null) {
                        invoicePaymentProtocolTask.User = invoicePaymentProtocolTaskUser;

                        invoicePaymentProtocol.SupplyPaymentTask = invoicePaymentProtocolTask;
                    }

                    invoicePaymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = invoicePaymentProtocolKey;
                    invoicePaymentProtocol.User = invoicePaymentProtocolUser;

                    invoiceFromList.PaymentDeliveryProtocols.Add(invoicePaymentProtocol);
                }

                if (invoiceInformationProtocol == null ||
                    invoiceFromList.InformationDeliveryProtocols.Any(p => p.Id.Equals(invoiceInformationProtocol.Id))) return supplyOrder;

                invoiceInformationProtocol.User = invoiceInformationProtocolUser;
                invoiceInformationProtocolKey.Key = invoiceInformationProtocolKeyTranslation?.Key ?? invoiceInformationProtocolKey.Key;
                invoiceInformationProtocol.SupplyInformationDeliveryProtocolKey = invoiceInformationProtocolKey;

                invoiceFromList.InformationDeliveryProtocols.Add(invoiceInformationProtocol);
            }

            return supplyOrder;
        };

        var props = new { NetId = supplyOrderToReturn.NetUid, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [SupplyInvoice].ID = [InvoiceDocument].SupplyInvoiceID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderPolandPaymentDeliveryProtocol] " +
            "ON [SupplyOrderPolandPaymentDeliveryProtocol].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [InvoiceDocument] AS [PaymentProtocolInvoiceDocument] " +
            "ON [PaymentProtocolInvoiceDocument].SupplyOrderPolandPaymentDeliveryProtocolID = [SupplyOrderPolandPaymentDeliveryProtocol].ID " +
            "AND [SupplyOrderPolandPaymentDeliveryProtocol].Deleted = 0 " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(SupplyInvoice),
            typeof(InvoiceDocument),
            typeof(SupplyOrderPolandPaymentDeliveryProtocol),
            typeof(InvoiceDocument)
        };

        Func<object[], InvoiceDocument> documentsMapper = objects => {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[1];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[2];
            SupplyOrderPolandPaymentDeliveryProtocol paymentProtocol = (SupplyOrderPolandPaymentDeliveryProtocol)objects[3];
            InvoiceDocument paymentProtocolInvoiceDocument = (InvoiceDocument)objects[4];

            if (supplyInvoice != null && supplyOrderToReturn.SupplyInvoices.Any(i => i.Id.Equals(supplyInvoice.Id))) {
                SupplyInvoice invoiceFromList = supplyOrderToReturn.SupplyInvoices.First(i => i.Id.Equals(supplyInvoice.Id));

                if (invoiceDocument != null && !invoiceFromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) invoiceFromList.InvoiceDocuments.Add(invoiceDocument);
            }

            if (paymentProtocol == null ||
                !supplyOrderToReturn.SupplyOrderPolandPaymentDeliveryProtocols.Any(p => p.Id.Equals(paymentProtocol.Id)))
                return invoiceDocument;

            SupplyOrderPolandPaymentDeliveryProtocol paymentProtocolFromList =
                supplyOrderToReturn.SupplyOrderPolandPaymentDeliveryProtocols.First(p => p.Id.Equals(paymentProtocol.Id));

            if (paymentProtocolInvoiceDocument != null && !paymentProtocolFromList.InvoiceDocuments.Any(d => d.Id.Equals(paymentProtocolInvoiceDocument.Id)))
                paymentProtocolFromList.InvoiceDocuments.Add(paymentProtocolInvoiceDocument);

            return invoiceDocument;
        };

        _connection.Query(sqlExpression, types, documentsMapper, props);

        sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN ( " +
            "SELECT ID " +
            ", SupplyOrganizationAgreementID " +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID] " +
            "FROM [PortWorkService] " +
            ") AS [PortWorkService] " +
            "ON [PortWorkService].ID = [SupplyOrder].PortWorkServiceID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [PortWorkAgreement] " +
            "ON [PortWorkAgreement].ID = [PortWorkService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [PortWorkAgreementCurrency] " +
            "ON [PortWorkAgreementCurrency].ID = [PortWorkAgreement].CurrencyID " +
            "AND [PortWorkAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN ( " +
            "SELECT ID " +
            ", SupplyOrganizationAgreementID " +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID] " +
            "FROM [TransportationService] " +
            ") AS [TransportationService] " +
            "ON [TransportationService].ID = [SupplyOrder].TransportationServiceID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [TransportationAgreement] " +
            "ON [TransportationAgreement].ID = [TransportationService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [TransportationAgreementCurrency] " +
            "ON [TransportationAgreementCurrency].ID = [TransportationAgreement].CurrencyID " +
            "AND [TransportationAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN ( " +
            "SELECT ID " +
            ", SupplyOrganizationAgreementID " +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID] " +
            "FROM [CustomAgencyService] " +
            ") AS [CustomAgencyService] " +
            "ON [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [CustomAgencyAgreement] " +
            "ON [CustomAgencyAgreement].ID = [CustomAgencyService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [CustomAgencyAgreementCurrency] " +
            "ON [CustomAgencyAgreementCurrency].ID = [CustomAgencyAgreement].CurrencyID " +
            "AND [CustomAgencyAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN ( " +
            "SELECT ID " +
            ", SupplyOrganizationAgreementID " +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID] " +
            "FROM [PortCustomAgencyService] " +
            ") AS [PortCustomAgencyService] " +
            "ON [PortCustomAgencyService].ID = [SupplyOrder].PortCustomAgencyServiceID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [PortCustomAgencyAgreement] " +
            "ON [PortCustomAgencyAgreement].ID = [PortCustomAgencyService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [PortCustomAgencyAgreementCurrency] " +
            "ON [PortCustomAgencyAgreementCurrency].ID = [PortCustomAgencyAgreement].CurrencyID " +
            "AND [PortCustomAgencyAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN ( " +
            "SELECT ID " +
            ", SupplyOrganizationAgreementID " +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID] " +
            "FROM [PlaneDeliveryService] " +
            ") AS [PlaneDeliveryService] " +
            "ON [PlaneDeliveryService].ID = [SupplyOrder].PlaneDeliveryServiceID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [PlaneDeliveryAgreement] " +
            "ON [PlaneDeliveryAgreement].ID = [PlaneDeliveryService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [PlaneDeliveryAgreementCurrency] " +
            "ON [PlaneDeliveryAgreementCurrency].ID = [PlaneDeliveryAgreement].CurrencyID " +
            "AND [PlaneDeliveryAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN ( " +
            "SELECT ID " +
            ", SupplyOrganizationAgreementID " +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID] " +
            "FROM [VehicleDeliveryService] " +
            ") AS [VehicleDeliveryService] " +
            "ON [VehicleDeliveryService].ID = [SupplyOrder].VehicleDeliveryServiceID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [VehicleDeliveryAgreement] " +
            "ON [VehicleDeliveryAgreement].ID = [VehicleDeliveryService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [VehicleDeliveryAgreementCurrency] " +
            "ON [VehicleDeliveryAgreementCurrency].ID = [VehicleDeliveryAgreement].CurrencyID " +
            "AND [VehicleDeliveryAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN ( " +
            "SELECT ID " +
            ", SupplyOrderID " +
            ", SupplyOrganizationAgreementID " +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID] " +
            "FROM [CustomService] " +
            ") AS [CustomService] " +
            "ON [CustomService].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [CustomAgreement] " +
            "ON [CustomAgreement].ID = [CustomService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [CustomAgreementCurrency] " +
            "ON [CustomAgreementCurrency].ID = [CustomAgreement].CurrencyID " +
            "AND [CustomAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyOrderContainerService] " +
            "ON [SupplyOrderContainerService].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyOrderContainerService].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT ID " +
            ", SupplyOrganizationAgreementID " +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID] " +
            "FROM [ContainerService] " +
            ") AS [ContainerService] " +
            "ON [ContainerService].ID = [SupplyOrderContainerService].ContainerServiceID " +
            "LEFT JOIN [SupplyOrganizationAgreement] AS [ContainerAgreement] " +
            "ON [ContainerAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [ContainerAgreementCurrency] " +
            "ON [ContainerAgreementCurrency].ID = [ContainerAgreement].CurrencyID " +
            "AND [ContainerAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentPortWorkService] " +
            "ON [ActProvidingServiceDocumentPortWorkService].[ID] = [PortWorkService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentPortWorkService] " +
            "ON [SupplyServiceAccountDocumentPortWorkService].[ID] = [PortWorkService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentTransportationService] " +
            "ON [ActProvidingServiceDocumentTransportationService].[ID] = [TransportationService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentTransportationService] " +
            "ON [SupplyServiceAccountDocumentTransportationService].[ID] = [TransportationService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentCustomAgencyService] " +
            "ON [ActProvidingServiceDocumentCustomAgencyService].[ID] = [CustomAgencyService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentCustomAgencyService] " +
            "ON [SupplyServiceAccountDocumentCustomAgencyService].[ID] = [CustomAgencyService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentPortCustomAgencyService] " +
            "ON [ActProvidingServiceDocumentPortCustomAgencyService].[ID] = [PortCustomAgencyService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentPortCustomAgencyService] " +
            "ON [SupplyServiceAccountDocumentPortCustomAgencyService].[ID] = [PortCustomAgencyService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentPlaneDeliveryService] " +
            "ON [ActProvidingServiceDocumentPlaneDeliveryService].[ID] = [PlaneDeliveryService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentPlaneDeliveryService] " +
            "ON [SupplyServiceAccountDocumentPlaneDeliveryService].[ID] = [PlaneDeliveryService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentVehicleDeliveryService] " +
            "ON [ActProvidingServiceDocumentVehicleDeliveryService].[ID] = [VehicleDeliveryService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentVehicleDeliveryService] " +
            "ON [SupplyServiceAccountDocumentVehicleDeliveryService].[ID] = [VehicleDeliveryService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentCustomService] " +
            "ON [ActProvidingServiceDocumentCustomService].[ID] = [CustomService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentCustomService] " +
            "ON [SupplyServiceAccountDocumentCustomService].[ID] = [CustomService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentContainerService] " +
            "ON [ActProvidingServiceDocumentContainerService].[ID] = [ContainerService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentContainerService] " +
            "ON [SupplyServiceAccountDocumentContainerService].[ID] = [ContainerService].[SupplyServiceAccountDocumentID] " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(PortWorkService),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(TransportationService),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(CustomAgencyService),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(PortCustomAgencyService),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(PlaneDeliveryService),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(VehicleDeliveryService),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(CustomService),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(SupplyOrderContainerService),
            typeof(ContainerService),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], SupplyOrder> supplyOrganizationAgreementsMapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyOrganizationAgreement portWorkAgreement = (SupplyOrganizationAgreement)objects[2];
            Currency portWorkAgreementCurrency = (Currency)objects[3];
            SupplyOrganizationAgreement transportationAgreement = (SupplyOrganizationAgreement)objects[5];
            Currency transportationAgreementCurrency = (Currency)objects[6];
            SupplyOrganizationAgreement customAgencyAgreement = (SupplyOrganizationAgreement)objects[8];
            Currency customAgencyAgreementCurrency = (Currency)objects[9];
            SupplyOrganizationAgreement portCustomAgencyAgreement = (SupplyOrganizationAgreement)objects[11];
            Currency portCustomAgencyAgreementCurrency = (Currency)objects[12];
            SupplyOrganizationAgreement planeDeliveryAgreement = (SupplyOrganizationAgreement)objects[14];
            Currency planeDeliveryAgreementCurrency = (Currency)objects[15];
            SupplyOrganizationAgreement vehicleDeliveryAgreement = (SupplyOrganizationAgreement)objects[17];
            Currency vehicleDeliveryAgreementCurrency = (Currency)objects[18];
            CustomService customService = (CustomService)objects[19];
            SupplyOrganizationAgreement customAgreement = (SupplyOrganizationAgreement)objects[20];
            Currency customAgreementCurrency = (Currency)objects[21];
            SupplyOrderContainerService supplyOrderContainerService = (SupplyOrderContainerService)objects[22];
            SupplyOrganizationAgreement containerAgreement = (SupplyOrganizationAgreement)objects[24];
            Currency containerAgreementCurrency = (Currency)objects[25];
            ActProvidingServiceDocument actProvidingServiceDocumentPortWorkService = (ActProvidingServiceDocument)objects[26];
            SupplyServiceAccountDocument supplyServiceAccountDocumentPortWorkService = (SupplyServiceAccountDocument)objects[27];
            ActProvidingServiceDocument actProvidingServiceDocumentTransportationService = (ActProvidingServiceDocument)objects[28];
            SupplyServiceAccountDocument supplyServiceAccountDocumentTransportationService = (SupplyServiceAccountDocument)objects[29];
            ActProvidingServiceDocument actProvidingServiceDocumentCustomAgencyService = (ActProvidingServiceDocument)objects[30];
            SupplyServiceAccountDocument supplyServiceAccountDocumentCustomAgencyService = (SupplyServiceAccountDocument)objects[31];
            ActProvidingServiceDocument actProvidingServiceDocumentPortCustomAgencyService = (ActProvidingServiceDocument)objects[32];
            SupplyServiceAccountDocument supplyServiceAccountDocumentPortCustomAgencyService = (SupplyServiceAccountDocument)objects[33];
            ActProvidingServiceDocument actProvidingServiceDocumentPlaneDeliveryService = (ActProvidingServiceDocument)objects[34];
            SupplyServiceAccountDocument supplyServiceAccountDocumentPlaneDeliveryService = (SupplyServiceAccountDocument)objects[35];
            ActProvidingServiceDocument actProvidingServiceDocumentVehicleDeliveryService = (ActProvidingServiceDocument)objects[36];
            SupplyServiceAccountDocument supplyServiceAccountDocumentVehicleDeliveryService = (SupplyServiceAccountDocument)objects[37];
            ActProvidingServiceDocument actProvidingServiceDocumentCustomService = (ActProvidingServiceDocument)objects[38];
            SupplyServiceAccountDocument supplyServiceAccountDocumentCustomService = (SupplyServiceAccountDocument)objects[39];
            ActProvidingServiceDocument actProvidingServiceDocumentContainerService = (ActProvidingServiceDocument)objects[40];
            SupplyServiceAccountDocument supplyServiceAccountDocumentContainerService = (SupplyServiceAccountDocument)objects[41];

            if (portWorkAgreement != null) {
                portWorkAgreement.Currency = portWorkAgreementCurrency;
                supplyOrderToReturn.PortWorkService.ActProvidingServiceDocument = actProvidingServiceDocumentPortWorkService;
                supplyOrderToReturn.PortWorkService.SupplyServiceAccountDocument = supplyServiceAccountDocumentPortWorkService;
                supplyOrderToReturn.PortWorkService.SupplyOrganizationAgreement = portWorkAgreement;
            }

            if (transportationAgreement != null) {
                transportationAgreement.Currency = transportationAgreementCurrency;
                supplyOrderToReturn.TransportationService.ActProvidingServiceDocument = actProvidingServiceDocumentTransportationService;
                supplyOrderToReturn.TransportationService.SupplyServiceAccountDocument = supplyServiceAccountDocumentTransportationService;
                supplyOrderToReturn.TransportationService.SupplyOrganizationAgreement = transportationAgreement;
            }

            if (customAgencyAgreement != null) {
                customAgencyAgreement.Currency = customAgencyAgreementCurrency;
                supplyOrderToReturn.CustomAgencyService.ActProvidingServiceDocument = actProvidingServiceDocumentCustomAgencyService;
                supplyOrderToReturn.CustomAgencyService.SupplyServiceAccountDocument = supplyServiceAccountDocumentCustomAgencyService;
                supplyOrderToReturn.CustomAgencyService.SupplyOrganizationAgreement = customAgencyAgreement;
            }

            if (portCustomAgencyAgreement != null) {
                portCustomAgencyAgreement.Currency = portCustomAgencyAgreementCurrency;
                supplyOrderToReturn.PortCustomAgencyService.ActProvidingServiceDocument = actProvidingServiceDocumentPortCustomAgencyService;
                supplyOrderToReturn.PortCustomAgencyService.SupplyServiceAccountDocument = supplyServiceAccountDocumentPortCustomAgencyService;
                supplyOrderToReturn.PortCustomAgencyService.SupplyOrganizationAgreement = portCustomAgencyAgreement;
            }

            if (planeDeliveryAgreement != null) {
                planeDeliveryAgreement.Currency = planeDeliveryAgreementCurrency;
                supplyOrderToReturn.PlaneDeliveryService.ActProvidingServiceDocument = actProvidingServiceDocumentPlaneDeliveryService;
                supplyOrderToReturn.PlaneDeliveryService.SupplyServiceAccountDocument = supplyServiceAccountDocumentPlaneDeliveryService;
                supplyOrderToReturn.PlaneDeliveryService.SupplyOrganizationAgreement = planeDeliveryAgreement;
            }

            if (vehicleDeliveryAgreement != null) {
                vehicleDeliveryAgreement.Currency = vehicleDeliveryAgreementCurrency;
                supplyOrderToReturn.VehicleDeliveryService.ActProvidingServiceDocument = actProvidingServiceDocumentVehicleDeliveryService;
                supplyOrderToReturn.VehicleDeliveryService.SupplyServiceAccountDocument = supplyServiceAccountDocumentVehicleDeliveryService;
                supplyOrderToReturn.VehicleDeliveryService.SupplyOrganizationAgreement = vehicleDeliveryAgreement;
            }

            if (customService != null && customAgreement != null) {
                customAgreement.Currency = customAgreementCurrency;
                supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id)).ActProvidingServiceDocument = actProvidingServiceDocumentCustomService;
                supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id)).SupplyServiceAccountDocument = supplyServiceAccountDocumentCustomService;
                supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id)).SupplyOrganizationAgreement = customAgreement;
            }

            if (supplyOrderContainerService == null || containerAgreement == null) return supplyOrder;

            containerAgreement.Currency = containerAgreementCurrency;

            supplyOrderToReturn
                .SupplyOrderContainerServices
                .First(s => s.Id.Equals(supplyOrderContainerService.Id))
                .ContainerService
                .ActProvidingServiceDocument = actProvidingServiceDocumentContainerService;

            supplyOrderToReturn
                .SupplyOrderContainerServices
                .First(s => s.Id.Equals(supplyOrderContainerService.Id))
                .ContainerService
                .SupplyServiceAccountDocument = supplyServiceAccountDocumentContainerService;

            supplyOrderToReturn
                .SupplyOrderContainerServices
                .First(s => s.Id.Equals(supplyOrderContainerService.Id))
                .ContainerService
                .SupplyOrganizationAgreement = containerAgreement;

            return supplyOrder;
        };

        _connection.Query(sqlExpression, types, supplyOrganizationAgreementsMapper, props);

        _connection.Query<SupplyOrder, ClientAgreement, Agreement, Organization, ProviderPricing, Currency, Pricing, SupplyOrder>(
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
            "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
            "AND [AgreementOrganization].CultureCode = @Culture " +
            "LEFT JOIN [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "WHERE [SupplyOrder].ID = @Id",
            (order, clientAgreement, agreement, agreementOrganization, providerPricing, currency, pricing) => {
                if (providerPricing != null) providerPricing.Pricing = pricing;

                agreement.Organization = agreementOrganization;
                agreement.ProviderPricing = providerPricing;
                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                supplyOrderToReturn.ClientAgreement = clientAgreement;

                return order;
            },
            new { supplyOrderToReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        Type[] mergerServiceTypes = {
            typeof(MergedService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(Currency),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], MergedService> mergerServiceMapper = objects => {
            MergedService service = (MergedService)objects[0];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[1];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[2];
            User user = (User)objects[3];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[4];
            User paymentTaskUser = (User)objects[5];
            SupplyPaymentTask accountingPaymentTask = (SupplyPaymentTask)objects[6];
            User accountingPaymentTaskUser = (User)objects[7];
            Currency currency = (Currency)objects[8];
            ActProvidingServiceDocument actProvidingServiceDocument = (ActProvidingServiceDocument)objects[9];
            SupplyServiceAccountDocument supplyServiceAccountDocument = (SupplyServiceAccountDocument)objects[10];

            if (supplyPaymentTask != null) supplyPaymentTask.User = paymentTaskUser;

            if (accountingPaymentTask != null) accountingPaymentTask.User = accountingPaymentTaskUser;

            supplyOrganizationAgreement.Currency = currency;

            service.User = user;
            service.SupplyPaymentTask = supplyPaymentTask;
            service.AccountingPaymentTask = accountingPaymentTask;
            service.SupplyOrganization = supplyOrganization;
            service.SupplyOrganizationAgreement = supplyOrganizationAgreement;

            service.ActProvidingServiceDocument = actProvidingServiceDocument;
            service.SupplyServiceAccountDocument = supplyServiceAccountDocument;

            supplyOrderToReturn.MergedServices.Add(service);

            return service;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [MergedService] " +
            "LEFT JOIN [SupplyOrganization] " +
            "ON [SupplyOrganization].ID = [MergedService].SupplyOrganizationID " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [MergedService].SupplyOrganizationAgreementID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [MergedService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [MergedService].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [PaymentTaskUser] " +
            "ON [PaymentTaskUser].ID = [SupplyPaymentTask].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask] " +
            "ON [AccountingPaymentTask].ID = [MergedService].AccountingPaymentTaskID " +
            "LEFT JOIN [User] AS [AccountingPaymentTaskUser] " +
            "ON [AccountingPaymentTaskUser].ID = [AccountingPaymentTask].UserID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [ActProvidingServiceDocument] " +
            "ON [ActProvidingServiceDocument].[ID] = [MergedService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] " +
            "ON [SupplyServiceAccountDocument].[ID] = [MergedService].[SupplyServiceAccountDocumentID] " +
            "WHERE [MergedService].Deleted = 0 " +
            "AND [MergedService].SupplyOrderID = @Id",
            mergerServiceTypes,
            mergerServiceMapper,
            new { supplyOrderToReturn.Id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (!supplyOrderToReturn.MergedServices.Any()) return;

        _connection.Query<InvoiceDocument, MergedService, InvoiceDocument>(
            "SELECT * " +
            "FROM [InvoiceDocument] " +
            "LEFT JOIN [MergedService] " +
            "ON [MergedService].ID = [InvoiceDocument].MergedServiceID " +
            "WHERE [InvoiceDocument].Deleted = 0 " +
            "AND [MergedService].ID IN @Ids",
            (document, service) => {
                supplyOrderToReturn
                    .MergedServices
                    .First(s => s.Id.Equals(service.Id))
                    .InvoiceDocuments
                    .Add(document);

                return document;
            },
            new { Ids = supplyOrderToReturn.MergedServices.Select(s => s.Id) }
        );

        _connection.Query<ServiceDetailItem, ServiceDetailItemKey, ServiceDetailItem>(
            "SELECT * " +
            "FROM [ServiceDetailItem] " +
            "LEFT JOIN [ServiceDetailItemKey] " +
            "ON [ServiceDetailItemKey].ID = [ServiceDetailItem].ServiceDetailItemKeyID " +
            "WHERE [ServiceDetailItem].Deleted = 0 " +
            "AND [ServiceDetailItem].MergedServiceID IN @Ids",
            (item, itemKey) => {
                item.ServiceDetailItemKey = itemKey;

                supplyOrderToReturn
                    .MergedServices
                    .First(s => s.Id.Equals(item.MergedServiceId))
                    .ServiceDetailItems
                    .Add(item);

                return item;
            },
            new { Ids = supplyOrderToReturn.MergedServices.Select(s => s.Id) }
        );
    }

    private SupplyOrder GetSupplyOrderWithPlaneServicesByNetId(Guid netId) {
        SupplyOrder supplyOrderToReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN (" +
            "SELECT [ClientAgreement].ID " +
            ",[ClientAgreement].AgreementID " +
            ",[ClientAgreement].ClientID " +
            "FROM ClientAgreement " +
            ") AS [ClientAgreement] " +
            "ON [ClientAgreement].ID = (" +
            "SELECT TOP(1) [ClientAgreement].ID " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [ClientAgreement].ClientID = [Client].ID " +
            "AND [Agreement].IsActive = 1" +
            ") " +
            "LEFT JOIN (" +
            "SELECT [Agreement].ID " +
            ",[Agreement].AmountDebt " +
            ",[Agreement].CurrencyID " +
            ",[Agreement].NumberDaysDebt " +
            ",[Agreement].IsAccounting " +
            ",[Agreement].IsControlAmountDebt " +
            ",[Agreement].IsControlNumberDaysDebt " +
            ",[Agreement].IsManagementAccounting " +
            ",[Agreement].WithVATAccounting " +
            ",[Agreement].IsActive " +
            ",[Agreement].Name " +
            ",[Agreement].DeferredPayment " +
            ",[Agreement].TermsOfPayment " +
            ",[Agreement].IsPrePaymentFull " +
            ",[Agreement].PrePaymentPercentages " +
            ",[Agreement].IsPrePayment " +
            "FROM [Agreement]" +
            ") AS [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN (" +
            "SELECT [SupplyOrderNumber].ID " +
            ",[SupplyOrderNumber].Number " +
            "FROM [SupplyOrderNumber]" +
            ") AS [SupplyOrderNumber] " +
            "ON SupplyOrderNumber.ID = SupplyOrder.SupplyOrderNumberID " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyProForm].ID = [SupplyOrder].SupplyProFormID " +
            "LEFT JOIN [ProFormDocument] " +
            "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
            "AND [ProFormDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormInformationProtocol] " +
            "ON [ProFormInformationProtocol].SupplyProFormID = [SupplyProForm].ID " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKey].ID " +
            ",[SupplyInformationDeliveryProtocolKey].[Key] " +
            "FROM [SupplyInformationDeliveryProtocolKey]" +
            ") AS [ProFormInformationProtocolKey] " +
            "ON [ProFormInformationProtocolKey].ID = [ProFormInformationProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKeyTranslation].ID, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].CultureCode, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].[Key], " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID " +
            "FROM [SupplyInformationDeliveryProtocolKeyTranslation] " +
            ") AS [ProFormInformationProtocolKeyTranslation] " +
            "ON [ProFormInformationProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID = [ProFormInformationProtocolKey].ID " +
            "AND [ProFormInformationProtocolKeyTranslation].CultureCode = @Culture " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormInformationProtocolUser] " +
            "ON [ProFormInformationProtocolUser].ID = [ProFormInformationProtocol].UserID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] AS [ProFormPaymentProtocol] " +
            "ON [ProFormPaymentProtocol].SupplyProFormID = [SupplyProForm].ID " +
            "AND [ProFormPaymentProtocol].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [SupplyOrderPaymentDeliveryProtocolKey].ID " +
            ",[SupplyOrderPaymentDeliveryProtocolKey].[Key] " +
            "FROM [SupplyOrderPaymentDeliveryProtocolKey] " +
            ") AS [ProFormPaymentProtocolKey] " +
            "ON [ProFormPaymentProtocolKey].ID = [ProFormPaymentProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormPaymentProtocolUser] " +
            "ON [ProFormPaymentProtocolUser].ID = [ProFormPaymentProtocol].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [ProFormPaymentProtocolTask] " +
            "ON [ProFormPaymentProtocolTask].ID = [ProFormPaymentProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormPaymentProtocolTaskUser] " +
            "ON [ProFormPaymentProtocolTaskUser].ID = [ProFormPaymentProtocolTask].UserID " +
            "LEFT JOIN [CustomService] " +
            "ON [CustomService].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
            "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
            "ON [CustomOrganization].ID = [CustomService].CustomOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServiceUser] " +
            "ON [CustomServiceUser].ID = [CustomService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomServicePaymentTask] " +
            "ON [CustomServicePaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomServiceAccountingPaymentTask] " +
            "ON [CustomServiceAccountingPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServicePaymentTaskUser] " +
            "ON [CustomServicePaymentTaskUser].ID = [CustomServicePaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServiceAccountingPaymentTaskUser] " +
            "ON [CustomServiceAccountingPaymentTaskUser].ID = [CustomServiceAccountingPaymentTask].UserID " +
            "LEFT JOIN [PlaneDeliveryService] " +
            "ON [PlaneDeliveryService].ID = [SupplyOrder].PlaneDeliveryServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [PlaneDeliveryOrganization]  " +
            "ON [PlaneDeliveryOrganization].ID = [PlaneDeliveryService].PlaneDeliveryOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [PlaneDeliveryServiceUser] " +
            "ON [PlaneDeliveryServiceUser].ID = [PlaneDeliveryService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PlaneDeliveryPaymentTask] " +
            "ON [PlaneDeliveryPaymentTask].ID = [PlaneDeliveryService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PlaneDeliveryAccountingPaymentTask] " +
            "ON [PlaneDeliveryAccountingPaymentTask].ID = [PlaneDeliveryService].AccountingPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [PlaneDeliveryPaymentTaskUser] " +
            "ON [PlaneDeliveryPaymentTaskUser].ID = [PlaneDeliveryPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [PlaneDeliveryAccountingPaymentTaskUser] " +
            "ON [PlaneDeliveryAccountingPaymentTaskUser].ID = [PlaneDeliveryAccountingPaymentTask].UserID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
            "ON [CustomAgencyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomAgencyServiceUser] " +
            "ON [CustomAgencyServiceUser].ID = [CustomAgencyService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomAgencyPaymentTask] " +
            "ON [CustomAgencyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomAgencyAccountingPaymentTask] " +
            "ON [CustomAgencyAccountingPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomAgencyPaymentTaskUser] " +
            "ON [CustomAgencyPaymentTaskUser].ID = [CustomAgencyPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomAgencyAccountingPaymentTaskUser] " +
            "ON [CustomAgencyAccountingPaymentTaskUser].ID = [CustomAgencyAccountingPaymentTask].UserID " +
            "LEFT JOIN [views].[CurrencyView] AS [AdditionalPaymentCurrency] " +
            "ON [AdditionalPaymentCurrency].ID = [SupplyOrder].AdditionalPaymentCurrencyID " +
            "AND [AdditionalPaymentCurrency].CultureCode = @Culture " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentCustomService] " +
            "ON [ActProvidingServiceDocumentCustomService].[ID] = [CustomService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentCustomService] " +
            "ON [SupplyServiceAccountDocumentCustomService].[ID] = [CustomService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentPlaneDeliveryService] " +
            "ON [ActProvidingServiceDocumentPlaneDeliveryService].[ID] = [PlaneDeliveryService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentPlaneDeliveryService] " +
            "ON [SupplyServiceAccountDocumentPlaneDeliveryService].[ID] = [PlaneDeliveryService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentCustomAgencyService] " +
            "ON [ActProvidingServiceDocumentCustomAgencyService].[ID] = [CustomAgencyService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentCustomAgencyService] " +
            "ON [SupplyServiceAccountDocumentCustomAgencyService].[ID]  = [CustomAgencyService].[SupplyServiceAccountDocumentID] " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization),
            typeof(SupplyOrderNumber),
            typeof(SupplyProForm),
            typeof(ProFormDocument),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(SupplyInformationDeliveryProtocolKeyTranslation),
            typeof(User),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(CustomService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(PlaneDeliveryService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(CustomAgencyService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(Currency),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            Client client = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Currency currency = (Currency)objects[4];
            Organization organization = (Organization)objects[5];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[6];
            SupplyProForm supplyProForm = (SupplyProForm)objects[7];
            ProFormDocument proFormDocument = (ProFormDocument)objects[8];
            SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[9];
            SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[10];
            SupplyInformationDeliveryProtocolKeyTranslation proFormInformationProtocolKeyTranslation = (SupplyInformationDeliveryProtocolKeyTranslation)objects[11];
            User proFormInformationProtocolUser = (User)objects[12];
            SupplyOrderPaymentDeliveryProtocol proFormPaymentProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[13];
            SupplyOrderPaymentDeliveryProtocolKey proFormPaymentProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[14];
            User proFormPaymentProtocolUser = (User)objects[15];
            SupplyPaymentTask proFormPaymentProtocolTask = (SupplyPaymentTask)objects[16];
            User proFormPaymentProtocolTaskUser = (User)objects[17];
            CustomService customService = (CustomService)objects[18];
            SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[19];
            SupplyOrganization customOrganization = (SupplyOrganization)objects[20];
            User customServiceUser = (User)objects[21];
            SupplyPaymentTask customServicePaymentTask = (SupplyPaymentTask)objects[22];
            SupplyPaymentTask customServiceAccountingPaymentTask = (SupplyPaymentTask)objects[23];
            User customServicePaymentTaskUser = (User)objects[24];
            User customServiceAccountingPaymentTaskUser = (User)objects[25];
            PlaneDeliveryService planeDeliveryService = (PlaneDeliveryService)objects[26];
            SupplyOrganization planeDeliveryOrganization = (SupplyOrganization)objects[27];
            User planeDeliveryServiceUser = (User)objects[28];
            SupplyPaymentTask planeDeliveryPaymentTask = (SupplyPaymentTask)objects[29];
            SupplyPaymentTask planeDeliveryAccountingPaymentTask = (SupplyPaymentTask)objects[30];
            User planeDeliveryPaymentTaskUser = (User)objects[31];
            User planeDeliveryAccountingPaymentTaskUser = (User)objects[32];
            CustomAgencyService customAgencyService = (CustomAgencyService)objects[33];
            SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[34];
            User customAgencyServiceUser = (User)objects[35];
            SupplyPaymentTask customAgencyPaymentTask = (SupplyPaymentTask)objects[36];
            SupplyPaymentTask customAgencyAccountingPaymentTask = (SupplyPaymentTask)objects[37];
            User customAgencyPaymentTaskUser = (User)objects[38];
            User customAgencyAccountingPaymentTaskUser = (User)objects[39];
            Currency additionalPaymentCurrency = (Currency)objects[40];

            ActProvidingServiceDocument actProvidingServiceDocumentCustomService = (ActProvidingServiceDocument)objects[41];
            SupplyServiceAccountDocument supplyServiceAccountDocumentCustomService = (SupplyServiceAccountDocument)objects[42];
            ActProvidingServiceDocument actProvidingServiceDocumentPlaneDeliveryService = (ActProvidingServiceDocument)objects[43];
            SupplyServiceAccountDocument supplyServiceAccountDocumentPlaneDeliveryService = (SupplyServiceAccountDocument)objects[44];
            ActProvidingServiceDocument actProvidingServiceDocumentCustomAgencyService = (ActProvidingServiceDocument)objects[45];
            SupplyServiceAccountDocument supplyServiceAccountDocumentCustomAgencyService = (SupplyServiceAccountDocument)objects[46];

            if (supplyOrderToReturn != null) {
                if (supplyProForm != null) {
                    if (proFormDocument != null && !supplyOrderToReturn.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                        supplyOrderToReturn.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null &&
                        !supplyOrderToReturn.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                        proFormInformationProtocolKey.Key = proFormInformationProtocolKeyTranslation?.Key ?? proFormInformationProtocolKey.Key;
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;
                        proFormInformationProtocol.User = proFormInformationProtocolUser;

                        supplyOrderToReturn.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormPaymentProtocol != null && !supplyOrderToReturn.SupplyProForm.PaymentDeliveryProtocols.Any(p => p.Id.Equals(proFormPaymentProtocol.Id))) {
                        if (proFormPaymentProtocolTask != null) {
                            proFormPaymentProtocolTask.User = proFormPaymentProtocolTaskUser;

                            proFormPaymentProtocol.SupplyPaymentTask = proFormPaymentProtocolTask;
                        }

                        proFormPaymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = proFormPaymentProtocolKey;
                        proFormPaymentProtocol.User = proFormPaymentProtocolUser;

                        supplyOrderToReturn.SupplyProForm.PaymentDeliveryProtocols.Add(proFormPaymentProtocol);
                    }
                }

                if (customService == null || supplyOrderToReturn.CustomServices.Any(s => s.Id.Equals(customService.Id))) return supplyOrder;

                if (customServicePaymentTask != null) {
                    customServicePaymentTask.User = customServicePaymentTaskUser;

                    customService.SupplyPaymentTask = customServicePaymentTask;
                }

                if (customServiceAccountingPaymentTask != null) {
                    customServiceAccountingPaymentTask.User = customServiceAccountingPaymentTaskUser;

                    customService.AccountingPaymentTask = customServiceAccountingPaymentTask;
                }

                customService.CustomOrganization = customOrganization;
                customService.ExciseDutyOrganization = exciseDutyOrganization;
                customService.User = customServiceUser;

                supplyOrderToReturn.CustomServices.Add(customService);
            } else {
                if (client != null) {
                    if (clientAgreement != null) {
                        if (agreement != null) {
                            agreement.Currency = currency;

                            clientAgreement.Agreement = agreement;
                        }

                        client.ClientAgreements.Add(clientAgreement);
                    }

                    supplyOrder.Client = client;
                }

                if (supplyProForm != null) {
                    if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null) {
                        proFormInformationProtocolKey.Key = proFormInformationProtocolKeyTranslation?.Key ?? proFormInformationProtocolKey.Key;
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;
                        proFormInformationProtocol.User = proFormInformationProtocolUser;

                        supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormPaymentProtocol != null) {
                        if (proFormPaymentProtocolTask != null) {
                            proFormPaymentProtocolTask.User = proFormPaymentProtocolTaskUser;

                            proFormPaymentProtocol.SupplyPaymentTask = proFormPaymentProtocolTask;
                        }

                        proFormPaymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = proFormPaymentProtocolKey;
                        proFormPaymentProtocol.User = proFormPaymentProtocolUser;

                        supplyProForm.PaymentDeliveryProtocols.Add(proFormPaymentProtocol);
                    }

                    supplyOrder.SupplyProForm = supplyProForm;
                }

                if (customService != null) {
                    if (customServicePaymentTask != null) {
                        customServicePaymentTask.User = customServicePaymentTaskUser;

                        customService.SupplyPaymentTask = customServicePaymentTask;
                    }

                    if (customServiceAccountingPaymentTask != null) {
                        customServiceAccountingPaymentTask.User = customServiceAccountingPaymentTaskUser;

                        customService.AccountingPaymentTask = customServiceAccountingPaymentTask;
                    }

                    customService.ActProvidingServiceDocument = actProvidingServiceDocumentCustomService;
                    customService.SupplyServiceAccountDocument = supplyServiceAccountDocumentCustomService;
                    customService.CustomOrganization = customOrganization;
                    customService.ExciseDutyOrganization = exciseDutyOrganization;
                    customService.User = customServiceUser;

                    supplyOrder.CustomServices.Add(customService);
                }

                if (customAgencyService != null) {
                    if (customAgencyPaymentTask != null) {
                        customAgencyPaymentTask.User = customAgencyPaymentTaskUser;

                        customAgencyService.SupplyPaymentTask = customAgencyPaymentTask;
                    }

                    if (customAgencyAccountingPaymentTask != null) {
                        customAgencyAccountingPaymentTask.User = customAgencyAccountingPaymentTaskUser;

                        customAgencyService.AccountingPaymentTask = customAgencyAccountingPaymentTask;
                    }

                    customAgencyService.ActProvidingServiceDocument = actProvidingServiceDocumentPlaneDeliveryService;
                    customAgencyService.SupplyServiceAccountDocument = supplyServiceAccountDocumentPlaneDeliveryService;
                    customAgencyService.CustomAgencyOrganization = customAgencyOrganization;
                    customAgencyService.User = customAgencyServiceUser;

                    supplyOrder.CustomAgencyService = customAgencyService;
                }

                if (planeDeliveryService != null) {
                    if (planeDeliveryPaymentTask != null) {
                        planeDeliveryPaymentTask.User = planeDeliveryPaymentTaskUser;

                        planeDeliveryService.SupplyPaymentTask = planeDeliveryPaymentTask;
                    }

                    if (planeDeliveryAccountingPaymentTask != null) {
                        planeDeliveryAccountingPaymentTask.User = planeDeliveryAccountingPaymentTaskUser;

                        planeDeliveryService.AccountingPaymentTask = planeDeliveryAccountingPaymentTask;
                    }

                    planeDeliveryService.ActProvidingServiceDocument = actProvidingServiceDocumentCustomAgencyService;
                    planeDeliveryService.SupplyServiceAccountDocument = supplyServiceAccountDocumentCustomAgencyService;
                    planeDeliveryService.PlaneDeliveryOrganization = planeDeliveryOrganization;
                    planeDeliveryService.User = planeDeliveryServiceUser;

                    supplyOrder.PlaneDeliveryService = planeDeliveryService;
                }

                supplyOrder.AdditionalPaymentCurrency = additionalPaymentCurrency;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                supplyOrderToReturn = supplyOrder;
            }

            return supplyOrder;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (supplyOrderToReturn == null) return supplyOrderToReturn;

        sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [PlaneDeliveryService] " +
            "ON [PlaneDeliveryService].ID = [SupplyOrder].PlaneDeliveryServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [PlaneDeliveryDocument] " +
            "ON [PlaneDeliveryService].ID = [PlaneDeliveryDocument].PlaneDeliveryServiceID " +
            "AND [PlaneDeliveryDocument].Deleted = 0 " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [CustomAgencyDocument] " +
            "ON [CustomAgencyDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "AND [CustomAgencyDocument].Deleted = 0 " +
            "LEFT JOIN [CustomService] " +
            "ON [CustomService].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [InvoiceDocument] AS [CustomServiceDocument] " +
            "ON [CustomServiceDocument].CustomServiceID = [CustomService].ID " +
            "AND [CustomServiceDocument].Deleted = 0 " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(PlaneDeliveryService),
            typeof(InvoiceDocument),
            typeof(CustomAgencyService),
            typeof(InvoiceDocument),
            typeof(CustomService),
            typeof(InvoiceDocument)
        };

        Func<object[], InvoiceDocument> documentsMapper = objects => {
            InvoiceDocument planeDeliveryDocument = (InvoiceDocument)objects[2];
            InvoiceDocument customAgencyDocument = (InvoiceDocument)objects[4];
            CustomService customService = (CustomService)objects[5];
            InvoiceDocument customServiceDocument = (InvoiceDocument)objects[6];

            if (planeDeliveryDocument != null && !supplyOrderToReturn.PlaneDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(planeDeliveryDocument.Id)))
                supplyOrderToReturn.PlaneDeliveryService.InvoiceDocuments.Add(planeDeliveryDocument);

            if (customAgencyDocument != null && !supplyOrderToReturn.CustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(customAgencyDocument.Id)))
                supplyOrderToReturn.CustomAgencyService.InvoiceDocuments.Add(customAgencyDocument);

            if (customService == null || customServiceDocument == null) return planeDeliveryDocument;

            CustomService customServiceFromList = supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id));

            if (!customServiceFromList.InvoiceDocuments.Any(d => d.Id.Equals(customServiceDocument.Id))) customServiceFromList.InvoiceDocuments.Add(customServiceDocument);

            return planeDeliveryDocument;
        };

        _connection.Query(sqlExpression, types, documentsMapper, props);

        sqlExpression =
            "SELECT " +
            "[SupplyOrder].* " +
            ",[CustomService].* " +
            ",[CustomServiceDetailItem].* " +
            ",[CustomServiceDetailItemKey].* " +
            ",[PlaneDeliveryServiceDetailItem].* " +
            ",[PlaneDeliveryServiceDetailItemKey].* " +
            ",[CustomAgencyServiceDetailItem].* " +
            ",[CustomAgencyServiceDetailItemKey].* " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [CustomService] " +
            "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
            "LEFT JOIN [ServiceDetailItem] AS [CustomServiceDetailItem] " +
            "ON [CustomServiceDetailItem].CustomServiceID = [CustomService].ID " +
            "AND [CustomServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [CustomServiceDetailItemKey] " +
            "ON [CustomServiceDetailItemKey].ID = [CustomServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [PlaneDeliveryService] " +
            "ON [SupplyOrder].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [PlaneDeliveryServiceDetailItem] " +
            "ON [PlaneDeliveryServiceDetailItem].PlaneDeliveryServiceID = [PlaneDeliveryService].ID " +
            "AND [PlaneDeliveryServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [PlaneDeliveryServiceDetailItemKey] " +
            "ON [PlaneDeliveryServiceDetailItemKey].ID = [PlaneDeliveryServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [CustomAgencyServiceDetailItem] " +
            "ON [CustomAgencyServiceDetailItem].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "AND [CustomAgencyServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [CustomAgencyServiceDetailItemKey] " +
            "ON [CustomAgencyServiceDetailItemKey].ID = [CustomAgencyServiceDetailItem].ServiceDetailItemKeyID " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(CustomService),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey)
        };

        Func<object[], SupplyOrder> detailItemsMapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            CustomService customService = (CustomService)objects[1];
            ServiceDetailItem customServiceDetailItem = (ServiceDetailItem)objects[2];
            ServiceDetailItemKey customServiceDetailItemKey = (ServiceDetailItemKey)objects[3];
            ServiceDetailItem planeDeliveryServiceDetailItem = (ServiceDetailItem)objects[4];
            ServiceDetailItemKey planeDeliveryServiceDetailItemKey = (ServiceDetailItemKey)objects[5];
            ServiceDetailItem customAgencyServiceDetailItem = (ServiceDetailItem)objects[6];
            ServiceDetailItemKey customAgencyServiceDetailItemKey = (ServiceDetailItemKey)objects[7];

            if (customService != null && customServiceDetailItem != null) {
                CustomService fromList = supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id));

                if (!fromList.ServiceDetailItems.Any(i => i.Id.Equals(customServiceDetailItem.Id))) {
                    customServiceDetailItem.ServiceDetailItemKey = customServiceDetailItemKey;

                    fromList.ServiceDetailItems.Add(customServiceDetailItem);
                }
            }

            if (planeDeliveryServiceDetailItem != null &&
                !supplyOrderToReturn.PlaneDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(planeDeliveryServiceDetailItem.Id))) {
                planeDeliveryServiceDetailItem.ServiceDetailItemKey = planeDeliveryServiceDetailItemKey;

                supplyOrderToReturn.PlaneDeliveryService.ServiceDetailItems.Add(planeDeliveryServiceDetailItem);
            }

            if (customAgencyServiceDetailItem == null ||
                supplyOrderToReturn.CustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(customAgencyServiceDetailItem.Id))) return supplyOrder;
            customAgencyServiceDetailItem.ServiceDetailItemKey = customAgencyServiceDetailItemKey;

            supplyOrderToReturn.CustomAgencyService.ServiceDetailItems.Add(customAgencyServiceDetailItem);

            return supplyOrder;
        };

        _connection.Query(sqlExpression, types, detailItemsMapper, props);

        types = new[] {
            typeof(SupplyOrder),
            typeof(TransportationService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], SupplyOrder> additionalServicesMapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            TransportationService transportationService = (TransportationService)objects[1];
            SupplyOrganization transportationOrganization = (SupplyOrganization)objects[2];
            User transportationServiceUser = (User)objects[3];
            SupplyPaymentTask transportationPaymentTask = (SupplyPaymentTask)objects[4];
            User transportationPaymentTaskUser = (User)objects[5];
            SupplyPaymentTask transportationAccountingPaymentTask = (SupplyPaymentTask)objects[6];
            User transportationAccountingPaymentTaskUser = (User)objects[7];
            ActProvidingServiceDocument actProvidingServiceDocument = (ActProvidingServiceDocument)objects[8];
            SupplyServiceAccountDocument supplyServiceAccountDocument = (SupplyServiceAccountDocument)objects[9];

            if (transportationService == null) return supplyOrder;

            if (transportationPaymentTask != null) {
                transportationPaymentTask.User = transportationPaymentTaskUser;

                transportationService.SupplyPaymentTask = transportationPaymentTask;
            }

            if (transportationAccountingPaymentTask != null) {
                transportationAccountingPaymentTask.User = transportationAccountingPaymentTaskUser;

                transportationService.AccountingPaymentTask = transportationAccountingPaymentTask;
            }

            transportationService.TransportationOrganization = transportationOrganization;
            transportationService.User = transportationServiceUser;
            transportationService.ActProvidingServiceDocument = actProvidingServiceDocument;
            transportationService.SupplyServiceAccountDocument = supplyServiceAccountDocument;

            supplyOrderToReturn.TransportationService = transportationService;

            return supplyOrder;
        };

        _connection.Query(
            "SELECT * FROM [SupplyOrder] " +
            "LEFT JOIN [TransportationService] " +
            "ON [TransportationService].ID = [SupplyOrder].TransportationServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [TransportationOrganization] " +
            "ON [TransportationOrganization].ID = [TransportationService].TransportationOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [TransportationServiceUser] " +
            "ON [TransportationServiceUser].ID = [TransportationService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [TransportationPaymentTask] " +
            "ON [TransportationPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [TransportationPaymentTaskUser] " +
            "ON [TransportationPaymentTaskUser].ID = [TransportationPaymentTask].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [TransportationAccountingPaymentTask] " +
            "ON [TransportationAccountingPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [TransportationAccountingPaymentTaskUser] " +
            "ON [TransportationAccountingPaymentTaskUser].ID = [TransportationAccountingPaymentTask].UserID " +
            "LEFT JOIN [ActProvidingServiceDocument] " +
            "ON [ActProvidingServiceDocument].[ID] = [TransportationService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] " +
            "ON [SupplyServiceAccountDocument].[ID] = [TransportationService].[SupplyServiceAccountDocumentID] " +
            "WHERE [SupplyOrder].NetUID = @NetId",
            types,
            additionalServicesMapper,
            props
        );

        _connection.Query<SupplyOrder, TransportationService, InvoiceDocument, SupplyOrder>(
            "SELECT * FROM [SupplyOrder] " +
            "LEFT JOIN [TransportationService] " +
            "ON [TransportationService].ID = [SupplyOrder].TransportationServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [TransportationDocument] " +
            "ON [TransportationDocument].TransportationServiceID = [TransportationService].ID " +
            "AND [TransportationDocument].Deleted = 0 " +
            "WHERE [SupplyOrder].NetUID = @NetId",
            (order, service, document) => {
                if (document != null && !supplyOrderToReturn.TransportationService.InvoiceDocuments.Any(d => d.Id.Equals(document.Id)))
                    supplyOrderToReturn.TransportationService.InvoiceDocuments.Add(document);

                return order;
            },
            props
        );

        _connection.Query<SupplyOrder, TransportationService, ServiceDetailItem, ServiceDetailItemKey, SupplyOrder>(
            "SELECT * FROM [SupplyOrder] " +
            "LEFT JOIN [TransportationService] " +
            "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [TransportationServiceDetailItem] " +
            "ON [TransportationServiceDetailItem].TransportationServiceID = [TransportationService].ID " +
            "AND [TransportationServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [TransportationServiceDetailItemKey] " +
            "ON [TransportationServiceDetailItemKey].ID = [TransportationServiceDetailItem].ServiceDetailItemKeyID " +
            "WHERE [SupplyOrder].NetUID = @NetId",
            (order, service, transportationServiceDetailItem, transportationServiceDetailItemKey) => {
                if (transportationServiceDetailItem == null ||
                    supplyOrderToReturn.TransportationService.ServiceDetailItems.Any(i => i.Id.Equals(transportationServiceDetailItem.Id))) return order;

                transportationServiceDetailItem.ServiceDetailItemKey = transportationServiceDetailItemKey;

                supplyOrderToReturn.TransportationService.ServiceDetailItems.Add(transportationServiceDetailItem);

                return order;
            },
            props
        );

        return supplyOrderToReturn;
    }

    private SupplyOrder GetSupplyOrderWithShipServicesByNetId(Guid netId) {
        SupplyOrder supplyOrderToReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN (" +
            "SELECT [ClientAgreement].ID " +
            ",[ClientAgreement].AgreementID " +
            ",[ClientAgreement].ClientID " +
            "FROM ClientAgreement " +
            ") AS [ClientAgreement] " +
            "ON [ClientAgreement].ID = (" +
            "SELECT TOP(1) [ClientAgreement].ID " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [ClientAgreement].ClientID = [Client].ID " +
            "AND [Agreement].IsActive = 1" +
            ") " +
            "LEFT JOIN (" +
            "SELECT [Agreement].ID " +
            ",[Agreement].AmountDebt " +
            ",[Agreement].CurrencyID " +
            ",[Agreement].NumberDaysDebt " +
            ",[Agreement].IsAccounting " +
            ",[Agreement].IsControlAmountDebt " +
            ",[Agreement].IsControlNumberDaysDebt " +
            ",[Agreement].IsManagementAccounting " +
            ",[Agreement].WithVATAccounting " +
            ",[Agreement].IsActive " +
            ",[Agreement].Name " +
            ",[Agreement].DeferredPayment " +
            ",[Agreement].TermsOfPayment " +
            ",[Agreement].IsPrePaymentFull " +
            ",[Agreement].PrePaymentPercentages " +
            ",[Agreement].IsPrePayment " +
            "FROM [Agreement]" +
            ") AS [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN (" +
            "SELECT [SupplyOrderNumber].ID " +
            ",[SupplyOrderNumber].Number " +
            "FROM [SupplyOrderNumber]" +
            ") AS [SupplyOrderNumber] " +
            "ON SupplyOrderNumber.ID = SupplyOrder.SupplyOrderNumberID " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyProForm].ID = [SupplyOrder].SupplyProFormID " +
            "LEFT JOIN [ProFormDocument] " +
            "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
            "AND [ProFormDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormInformationProtocol] " +
            "ON [ProFormInformationProtocol].SupplyProFormID = [SupplyProForm].ID " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKey].ID " +
            ",[SupplyInformationDeliveryProtocolKey].[Key] " +
            "FROM [SupplyInformationDeliveryProtocolKey]" +
            ") AS [ProFormInformationProtocolKey] " +
            "ON [ProFormInformationProtocolKey].ID = [ProFormInformationProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKeyTranslation].ID, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].CultureCode, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].[Key], " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID " +
            "FROM [SupplyInformationDeliveryProtocolKeyTranslation] " +
            ") AS [ProFormInformationProtocolKeyTranslation] " +
            "ON [ProFormInformationProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID = [ProFormInformationProtocolKey].ID " +
            "AND [ProFormInformationProtocolKeyTranslation].CultureCode = @Culture " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormInformationProtocolUser] " +
            "ON [ProFormInformationProtocolUser].ID = [ProFormInformationProtocol].UserID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] AS [ProFormPaymentProtocol] " +
            "ON [ProFormPaymentProtocol].SupplyProFormID = [SupplyProForm].ID " +
            "AND [ProFormPaymentProtocol].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [SupplyOrderPaymentDeliveryProtocolKey].ID " +
            ",[SupplyOrderPaymentDeliveryProtocolKey].[Key] " +
            "FROM [SupplyOrderPaymentDeliveryProtocolKey] " +
            ") AS [ProFormPaymentProtocolKey] " +
            "ON [ProFormPaymentProtocolKey].ID = [ProFormPaymentProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormPaymentProtocolUser] " +
            "ON [ProFormPaymentProtocolUser].ID = [ProFormPaymentProtocol].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [ProFormPaymentProtocolTask] " +
            "ON [ProFormPaymentProtocolTask].ID = [ProFormPaymentProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormPaymentProtocolTaskUser] " +
            "ON [ProFormPaymentProtocolTaskUser].ID = [ProFormPaymentProtocolTask].UserID " +
            "LEFT JOIN [CustomService] " +
            "ON [CustomService].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
            "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
            "ON [CustomOrganization].ID = [CustomService].CustomOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServiceUser] " +
            "ON [CustomServiceUser].ID = [CustomService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomServicePaymentTask] " +
            "ON [CustomServicePaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomServiceAccountingPaymentTask] " +
            "ON [CustomServiceAccountingPaymentTask].ID = [CustomService].AccountingPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServicePaymentTaskUser] " +
            "ON [CustomServicePaymentTaskUser].ID = [CustomServicePaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServiceAccountingPaymentTaskUser] " +
            "ON [CustomServiceAccountingPaymentTaskUser].ID = [CustomServiceAccountingPaymentTask].UserID " +
            "LEFT JOIN [PortWorkService] " +
            "ON [PortWorkService].ID = [SupplyOrder].PortWorkServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [PortWorkOrganization] " +
            "ON [PortWorkOrganization].ID = [PortWorkService].PortWorkOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [PortWorkServiceUser] " +
            "ON [PortWorkServiceUser].ID = [PortWorkService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PortWorkPaymentTask] " +
            "ON [PortWorkPaymentTask].ID = [PortWorkService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PortWorkAccountingPaymentTask] " +
            "ON [PortWorkAccountingPaymentTask].ID = [PortWorkService].AccountingPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [PortWorkPaymentTaskUser] " +
            "ON [PortWorkPaymentTaskUser].ID = [PortWorkPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [PortWorkAccountingPaymentTaskUser] " +
            "ON [PortWorkAccountingPaymentTaskUser].ID = [PortWorkAccountingPaymentTask].UserID " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [PortCustomAgencyService].ID = [SupplyOrder].PortCustomAgencyServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
            "ON [PortCustomAgencyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [PortCustomAgencyServiceUser] " +
            "ON [PortCustomAgencyServiceUser].ID = [PortCustomAgencyService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PortCustomAgencyPaymentTask] " +
            "ON [PortCustomAgencyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PortCustomAgencyAccountingPaymentTask] " +
            "ON [PortCustomAgencyAccountingPaymentTask].ID = [PortCustomAgencyService].[AccountingPaymentTaskID] " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User] " +
            ") AS [PortCustomAgencyPaymentTaskUser] " +
            "ON [PortCustomAgencyPaymentTaskUser].ID = [PortCustomAgencyPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User] " +
            ") AS [PortCustomAgencyAccountingPaymentTaskUser] " +
            "ON [PortCustomAgencyAccountingPaymentTaskUser].ID = [PortCustomAgencyAccountingPaymentTask].UserID " +
            "LEFT JOIN [views].[CurrencyView] AS [AdditionalPaymentCurrency] " +
            "ON [AdditionalPaymentCurrency].ID = [SupplyOrder].AdditionalPaymentCurrencyID " +
            "AND [AdditionalPaymentCurrency].CultureCode = @Culture " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentCustomService] " +
            "ON [ActProvidingServiceDocumentCustomService].[ID] = [CustomService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentCustomService] " +
            "ON [SupplyServiceAccountDocumentCustomService].[ID] = [CustomService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentPortWorkService] " +
            "ON [ActProvidingServiceDocumentPortWorkService].[ID] = [PortWorkService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentPortWorkService] " +
            "ON [SupplyServiceAccountDocumentPortWorkService].[ID] = [PortWorkService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentPortCustomAgencyService] " +
            "ON [ActProvidingServiceDocumentPortCustomAgencyService].[ID] = [PortCustomAgencyService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentPortCustomAgencyService] " +
            "ON [SupplyServiceAccountDocumentPortCustomAgencyService].[ID] = [PortCustomAgencyService].[SupplyServiceAccountDocumentID] " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization),
            typeof(SupplyOrderNumber),
            typeof(SupplyProForm),
            typeof(ProFormDocument),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(SupplyInformationDeliveryProtocolKeyTranslation),
            typeof(User),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(CustomService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(PortWorkService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(PortCustomAgencyService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(Currency),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            Client client = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Currency currency = (Currency)objects[4];
            Organization organization = (Organization)objects[5];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[6];
            SupplyProForm supplyProForm = (SupplyProForm)objects[7];
            ProFormDocument proFormDocument = (ProFormDocument)objects[8];
            SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[9];
            SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[10];
            SupplyInformationDeliveryProtocolKeyTranslation proFormInformationProtocolKeyTranslation = (SupplyInformationDeliveryProtocolKeyTranslation)objects[11];
            User proFormInformationProtocolUser = (User)objects[12];
            SupplyOrderPaymentDeliveryProtocol proFormPaymentProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[13];
            SupplyOrderPaymentDeliveryProtocolKey proFormPaymentProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[14];
            User proFormPaymentProtocolUser = (User)objects[15];
            SupplyPaymentTask proFormPaymentProtocolTask = (SupplyPaymentTask)objects[16];
            User proFormPaymentProtocolTaskUser = (User)objects[17];
            CustomService customService = (CustomService)objects[18];
            SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[19];
            SupplyOrganization customOrganization = (SupplyOrganization)objects[20];
            User customServiceUser = (User)objects[21];
            SupplyPaymentTask customServicePaymentTask = (SupplyPaymentTask)objects[22];
            SupplyPaymentTask customServiceAccountingPaymentTask = (SupplyPaymentTask)objects[23];
            User customServicePaymentTaskUser = (User)objects[24];
            User customServiceAccountingPaymentTaskUser = (User)objects[25];
            PortWorkService portWorkService = (PortWorkService)objects[26];
            SupplyOrganization portWorkOrganization = (SupplyOrganization)objects[27];
            User portWorkServiceUser = (User)objects[28];
            SupplyPaymentTask portWorkPaymentTask = (SupplyPaymentTask)objects[29];
            SupplyPaymentTask portWorkAccountingPaymentTask = (SupplyPaymentTask)objects[30];
            User portWorkPaymentTaskUser = (User)objects[31];
            User portWorkAccountingPaymentTaskUser = (User)objects[32];
            PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[33];
            SupplyOrganization portCustomAgencyOrganization = (SupplyOrganization)objects[34];
            User portCustomAgencyServiceUser = (User)objects[35];
            SupplyPaymentTask portCustomAgencyPaymentTask = (SupplyPaymentTask)objects[36];
            SupplyPaymentTask portCustomAgencyAccountingPaymentTask = (SupplyPaymentTask)objects[37];
            User portCustomAgencyPaymentTaskUser = (User)objects[38];
            User portCustomAgencyAccountingPaymentTaskUser = (User)objects[39];
            Currency additionalPaymentCurrency = (Currency)objects[40];
            ActProvidingServiceDocument actProvidingServiceDocumentCustomService = (ActProvidingServiceDocument)objects[41];
            SupplyServiceAccountDocument supplyServiceAccountDocumentCustomService = (SupplyServiceAccountDocument)objects[42];
            ActProvidingServiceDocument actProvidingServiceDocumentPortWorkService = (ActProvidingServiceDocument)objects[43];
            SupplyServiceAccountDocument supplyServiceAccountDocumentPortWorkService = (SupplyServiceAccountDocument)objects[44];
            ActProvidingServiceDocument actProvidingServiceDocumentPortCustomAgencyService = (ActProvidingServiceDocument)objects[45];
            SupplyServiceAccountDocument supplyServiceAccountDocumentPortCustomAgencyService = (SupplyServiceAccountDocument)objects[46];

            if (supplyOrderToReturn != null) {
                if (supplyProForm == null) return supplyOrder;

                if (proFormDocument != null && !supplyOrderToReturn.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                    supplyOrderToReturn.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                if (proFormInformationProtocol != null &&
                    !supplyOrderToReturn.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                    proFormInformationProtocolKey.Key = proFormInformationProtocolKeyTranslation?.Key ?? proFormInformationProtocolKey.Key;
                    proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;
                    proFormInformationProtocol.User = proFormInformationProtocolUser;

                    supplyOrderToReturn.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                }

                if (proFormPaymentProtocol != null && !supplyOrderToReturn.SupplyProForm.PaymentDeliveryProtocols.Any(p => p.Id.Equals(proFormPaymentProtocol.Id))) {
                    if (proFormPaymentProtocolTask != null) {
                        proFormPaymentProtocolTask.User = proFormPaymentProtocolTaskUser;

                        proFormPaymentProtocol.SupplyPaymentTask = proFormPaymentProtocolTask;
                    }

                    proFormPaymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = proFormPaymentProtocolKey;
                    proFormPaymentProtocol.User = proFormPaymentProtocolUser;

                    supplyOrderToReturn.SupplyProForm.PaymentDeliveryProtocols.Add(proFormPaymentProtocol);
                }

                if (customService == null || supplyOrderToReturn.CustomServices.Any(s => s.Id.Equals(customService.Id))) return supplyOrder;

                if (customServicePaymentTask != null) {
                    customServicePaymentTask.User = customServicePaymentTaskUser;

                    customService.SupplyPaymentTask = customServicePaymentTask;
                }

                if (customServiceAccountingPaymentTask != null) {
                    customServiceAccountingPaymentTask.User = customServiceAccountingPaymentTaskUser;

                    customService.AccountingPaymentTask = customServiceAccountingPaymentTask;
                }

                customService.CustomOrganization = customOrganization;
                customService.ExciseDutyOrganization = exciseDutyOrganization;
                customService.User = customServiceUser;

                supplyOrderToReturn.CustomServices.Add(customService);
            } else {
                if (client != null) {
                    if (clientAgreement != null) {
                        if (agreement != null) {
                            agreement.Currency = currency;

                            clientAgreement.Agreement = agreement;
                        }

                        client.ClientAgreements.Add(clientAgreement);
                    }

                    supplyOrder.Client = client;
                }

                if (supplyProForm != null) {
                    if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null) {
                        proFormInformationProtocolKey.Key = proFormInformationProtocolKeyTranslation?.Key ?? proFormInformationProtocolKey.Key;
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;
                        proFormInformationProtocol.User = proFormInformationProtocolUser;

                        supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormPaymentProtocol != null) {
                        if (proFormPaymentProtocolTask != null) {
                            proFormPaymentProtocolTask.User = proFormPaymentProtocolTaskUser;

                            proFormPaymentProtocol.SupplyPaymentTask = proFormPaymentProtocolTask;
                        }

                        proFormPaymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = proFormPaymentProtocolKey;
                        proFormPaymentProtocol.User = proFormPaymentProtocolUser;

                        supplyProForm.PaymentDeliveryProtocols.Add(proFormPaymentProtocol);
                    }

                    supplyOrder.SupplyProForm = supplyProForm;
                }

                if (customService != null) {
                    if (customServicePaymentTask != null) {
                        customServicePaymentTask.User = customServicePaymentTaskUser;

                        customService.SupplyPaymentTask = customServicePaymentTask;
                    }

                    if (customServiceAccountingPaymentTask != null) {
                        customServiceAccountingPaymentTask.User = customServiceAccountingPaymentTaskUser;

                        customService.AccountingPaymentTask = customServiceAccountingPaymentTask;
                    }

                    customService.ActProvidingServiceDocument = actProvidingServiceDocumentCustomService;
                    customService.SupplyServiceAccountDocument = supplyServiceAccountDocumentCustomService;
                    customService.CustomOrganization = customOrganization;
                    customService.ExciseDutyOrganization = exciseDutyOrganization;
                    customService.User = customServiceUser;

                    supplyOrder.CustomServices.Add(customService);
                }

                if (portWorkService != null) {
                    if (portWorkPaymentTask != null) {
                        portWorkPaymentTask.User = portWorkPaymentTaskUser;

                        portWorkService.SupplyPaymentTask = portWorkPaymentTask;
                    }

                    if (portWorkAccountingPaymentTask != null) {
                        portWorkAccountingPaymentTask.User = portWorkAccountingPaymentTaskUser;

                        portWorkService.AccountingPaymentTask = portWorkAccountingPaymentTask;
                    }

                    portWorkService.ActProvidingServiceDocument = actProvidingServiceDocumentPortWorkService;
                    portWorkService.SupplyServiceAccountDocument = supplyServiceAccountDocumentPortWorkService;
                    portWorkService.PortWorkOrganization = portWorkOrganization;
                    portWorkService.User = portWorkServiceUser;

                    supplyOrder.PortWorkService = portWorkService;
                }

                if (portCustomAgencyService != null) {
                    if (portCustomAgencyPaymentTask != null) {
                        portCustomAgencyPaymentTask.User = portCustomAgencyPaymentTaskUser;

                        portCustomAgencyService.SupplyPaymentTask = portCustomAgencyPaymentTask;
                    }

                    if (portCustomAgencyAccountingPaymentTask != null) {
                        portCustomAgencyAccountingPaymentTask.User = portCustomAgencyAccountingPaymentTaskUser;

                        portCustomAgencyService.AccountingPaymentTask = portCustomAgencyAccountingPaymentTask;
                    }


                    portCustomAgencyService.ActProvidingServiceDocument = actProvidingServiceDocumentPortCustomAgencyService;
                    portCustomAgencyService.SupplyServiceAccountDocument = supplyServiceAccountDocumentPortCustomAgencyService;
                    portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;
                    portCustomAgencyService.User = portCustomAgencyServiceUser;

                    supplyOrder.PortCustomAgencyService = portCustomAgencyService;
                }

                supplyOrder.AdditionalPaymentCurrency = additionalPaymentCurrency;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                supplyOrderToReturn = supplyOrder;
            }

            return supplyOrder;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        if (supplyOrderToReturn == null) return supplyOrderToReturn;

        sqlExpression =
            "SELECT * FROM [SupplyOrder] " +
            "LEFT JOIN [TransportationService] " +
            "ON [TransportationService].ID = [SupplyOrder].TransportationServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [TransportationOrganization] " +
            "ON [TransportationOrganization].ID = [TransportationService].TransportationOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [TransportationServiceUser] " +
            "ON [TransportationServiceUser].ID = [TransportationService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [TransportationPaymentTask] " +
            "ON [TransportationPaymentTask].ID = [TransportationService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [TransportationAccountingPaymentTask] " +
            "ON [TransportationAccountingPaymentTask].ID = [TransportationService].AccountingPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [TransportationPaymentTaskUser] " +
            "ON [TransportationPaymentTaskUser].ID = [TransportationPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [TransportationAccountingPaymentTaskUser] " +
            "ON [TransportationAccountingPaymentTaskUser].ID = [TransportationAccountingPaymentTask].UserID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
            "ON [CustomAgencyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomAgencyServiceUser] " +
            "ON [CustomAgencyServiceUser].ID = [CustomAgencyService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomAgencyPaymentTask] " +
            "ON [CustomAgencyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomAgencyAccountingPaymentTask] " +
            "ON [CustomAgencyAccountingPaymentTask].ID = [CustomAgencyService].AccountingPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User] " +
            ") AS [CustomAgencyPaymentTaskUser] " +
            "ON [CustomAgencyPaymentTaskUser].ID = [CustomAgencyPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User] " +
            ") AS [CustomAgencyAccountingPaymentTaskUser] " +
            "ON [CustomAgencyAccountingPaymentTaskUser].ID = [CustomAgencyAccountingPaymentTask].UserID " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentTransportationService] " +
            "ON [ActProvidingServiceDocumentTransportationService].[ID] = [TransportationService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentTransportationService] " +
            "ON [SupplyServiceAccountDocumentTransportationService].[ID] = [TransportationService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentCustomAgencyService] " +
            "ON [ActProvidingServiceDocumentCustomAgencyService].[ID] = [CustomAgencyService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentCustomAgencyService] " +
            "ON [SupplyServiceAccountDocumentCustomAgencyService].[ID] = [CustomAgencyService].[SupplyServiceAccountDocumentID] " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(TransportationService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(CustomAgencyService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], SupplyOrder> additionalServicesMapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            TransportationService transportationService = (TransportationService)objects[1];
            SupplyOrganization transportationOrganization = (SupplyOrganization)objects[2];
            User transportationServiceUser = (User)objects[3];
            SupplyPaymentTask transportationPaymentTask = (SupplyPaymentTask)objects[4];
            SupplyPaymentTask transportationAccountingPaymentTask = (SupplyPaymentTask)objects[5];
            User transportationPaymentTaskUser = (User)objects[6];
            User transportationAccountingPaymentTaskUser = (User)objects[7];
            CustomAgencyService customAgencyService = (CustomAgencyService)objects[8];
            SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[9];
            User customAgencyServiceUser = (User)objects[10];
            SupplyPaymentTask customAgencyPaymentTask = (SupplyPaymentTask)objects[11];
            SupplyPaymentTask customAgencyAccountingPaymentTask = (SupplyPaymentTask)objects[12];
            User customAgencyPaymentTaskUser = (User)objects[13];
            User customAgencyAccountingPaymentTaskUser = (User)objects[14];
            ActProvidingServiceDocument actProvidingServiceDocumentTransportationService = (ActProvidingServiceDocument)objects[15];
            SupplyServiceAccountDocument supplyServiceAccountDocumentTransportationService = (SupplyServiceAccountDocument)objects[16];
            ActProvidingServiceDocument actProvidingServiceDocumentCustomAgencyService = (ActProvidingServiceDocument)objects[17];
            SupplyServiceAccountDocument supplyServiceAccountDocumentCustomAgencyService = (SupplyServiceAccountDocument)objects[18];

            if (transportationService != null) {
                if (transportationPaymentTask != null) {
                    transportationPaymentTask.User = transportationPaymentTaskUser;

                    transportationService.SupplyPaymentTask = transportationPaymentTask;
                }

                if (transportationAccountingPaymentTask != null) {
                    transportationAccountingPaymentTask.User = transportationAccountingPaymentTaskUser;

                    transportationService.AccountingPaymentTask = transportationAccountingPaymentTask;
                }

                transportationService.ActProvidingServiceDocument = actProvidingServiceDocumentTransportationService;
                transportationService.SupplyServiceAccountDocument = supplyServiceAccountDocumentTransportationService;
                transportationService.TransportationOrganization = transportationOrganization;
                transportationService.User = transportationServiceUser;

                supplyOrderToReturn.TransportationService = transportationService;
            }

            if (customAgencyService == null) return supplyOrder;

            if (customAgencyPaymentTask != null) {
                customAgencyPaymentTask.User = customAgencyPaymentTaskUser;

                customAgencyService.SupplyPaymentTask = customAgencyPaymentTask;
            }

            if (customAgencyAccountingPaymentTask != null) {
                customAgencyAccountingPaymentTask.User = customAgencyAccountingPaymentTaskUser;

                customAgencyService.AccountingPaymentTask = customAgencyAccountingPaymentTask;
            }

            customAgencyService.ActProvidingServiceDocument = actProvidingServiceDocumentCustomAgencyService;
            customAgencyService.SupplyServiceAccountDocument = supplyServiceAccountDocumentCustomAgencyService;
            customAgencyService.CustomAgencyOrganization = customAgencyOrganization;
            customAgencyService.User = customAgencyServiceUser;

            supplyOrderToReturn.CustomAgencyService = customAgencyService;

            return supplyOrder;
        };

        _connection.Query(sqlExpression, types, additionalServicesMapper, props);

        sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [PortWorkService] " +
            "ON [PortWorkService].ID = [SupplyOrder].PortWorkServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [PortWorkDocument] " +
            "ON [PortWorkDocument].PortWorkServiceID = [PortWorkService].ID " +
            "AND [PortWorkDocument].Deleted = 0 " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [PortCustomAgencyService].ID = [SupplyOrder].PortCustomAgencyServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [PortCustomAgencyDocument] " +
            "ON [PortCustomAgencyDocument].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "AND [PortCustomAgencyDocument].Deleted = 0 " +
            "LEFT JOIN [TransportationService] " +
            "ON [TransportationService].ID = [SupplyOrder].TransportationServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [TransportationDocument] " +
            "ON [TransportationDocument].TransportationServiceID = [TransportationService].ID " +
            "AND [TransportationDocument].Deleted = 0 " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [CustomAgencyDocument] " +
            "ON [CustomAgencyDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "AND [CustomAgencyDocument].Deleted = 0 " +
            "LEFT JOIN [CustomService] " +
            "ON [CustomService].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [InvoiceDocument] AS [CustomServiceDocument] " +
            "ON [CustomServiceDocument].CustomServiceID = [CustomService].ID " +
            "AND [CustomServiceDocument].Deleted = 0 " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(PortWorkService),
            typeof(InvoiceDocument),
            typeof(PortCustomAgencyService),
            typeof(InvoiceDocument),
            typeof(TransportationService),
            typeof(InvoiceDocument),
            typeof(CustomAgencyService),
            typeof(InvoiceDocument),
            typeof(CustomService),
            typeof(InvoiceDocument)
        };

        Func<object[], InvoiceDocument> documentsMapper = objects => {
            InvoiceDocument portWorkDocument = (InvoiceDocument)objects[2];
            InvoiceDocument portCustomAgencyDocument = (InvoiceDocument)objects[4];
            InvoiceDocument transportationDocument = (InvoiceDocument)objects[6];
            InvoiceDocument customAgencyDocument = (InvoiceDocument)objects[8];
            CustomService customService = (CustomService)objects[9];
            InvoiceDocument customServiceDocument = (InvoiceDocument)objects[10];

            if (portWorkDocument != null && !supplyOrderToReturn.PortWorkService.InvoiceDocuments.Any(d => d.Id.Equals(portWorkDocument.Id)))
                supplyOrderToReturn.PortWorkService.InvoiceDocuments.Add(portWorkDocument);

            if (portCustomAgencyDocument != null && !supplyOrderToReturn.PortCustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(portCustomAgencyDocument.Id)))
                supplyOrderToReturn.PortCustomAgencyService.InvoiceDocuments.Add(portCustomAgencyDocument);

            if (transportationDocument != null && !supplyOrderToReturn.TransportationService.InvoiceDocuments.Any(d => d.Id.Equals(transportationDocument.Id)))
                supplyOrderToReturn.TransportationService.InvoiceDocuments.Add(transportationDocument);

            if (customAgencyDocument != null && !supplyOrderToReturn.CustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(customAgencyDocument.Id)))
                supplyOrderToReturn.CustomAgencyService.InvoiceDocuments.Add(customAgencyDocument);

            if (customService == null || customServiceDocument == null) return portWorkDocument;

            CustomService customServiceFromList = supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id));

            if (!customServiceFromList.InvoiceDocuments.Any(d => d.Id.Equals(customServiceDocument.Id))) customServiceFromList.InvoiceDocuments.Add(customServiceDocument);

            return portWorkDocument;
        };

        _connection.Query(sqlExpression, types, documentsMapper, props);

        sqlExpression =
            "SELECT " +
            "[SupplyOrder].* " +
            ",[CustomService].* " +
            ",[CustomServiceDetailItem].* " +
            ",[CustomServiceDetailItemKey].* " +
            ",[PortCustomAgencyServiceDetailItem].* " +
            ",[PortCustomAgencyServiceDetailItemKey].* " +
            ",[PortWorkServiceDetailItem].* " +
            ",[PortWorkServiceDetailItemKey].* " +
            ",[CustomAgencyServiceDetailItem].* " +
            ",[CustomAgencyServiceDetailItemKey].* " +
            ",[TransportationServiceDetailItem].* " +
            ",[TransportationServiceDetailItemKey].* " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [CustomService] " +
            "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
            "LEFT JOIN [ServiceDetailItem] AS [CustomServiceDetailItem] " +
            "ON [CustomServiceDetailItem].CustomServiceID = [CustomService].ID " +
            "AND [CustomServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [CustomServiceDetailItemKey] " +
            "ON [CustomServiceDetailItemKey].ID = [CustomServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [PortCustomAgencyServiceDetailItem] " +
            "ON [PortCustomAgencyServiceDetailItem].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "AND [PortCustomAgencyServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [PortCustomAgencyServiceDetailItemKey] " +
            "ON [PortCustomAgencyServiceDetailItemKey].ID = [PortCustomAgencyServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [PortWorkService] " +
            "ON [SupplyOrder].PortWorkServiceID = [PortWorkService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [PortWorkServiceDetailItem] " +
            "ON [PortWorkServiceDetailItem].PortWorkServiceID = [PortWorkService].ID " +
            "AND [PortWorkServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [PortWorkServiceDetailItemKey] " +
            "ON [PortWorkServiceDetailItemKey].ID = [PortWorkServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [CustomAgencyServiceDetailItem] " +
            "ON [CustomAgencyServiceDetailItem].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "AND [CustomAgencyServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [CustomAgencyServiceDetailItemKey] " +
            "ON [CustomAgencyServiceDetailItemKey].ID = [CustomAgencyServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [TransportationService] " +
            "ON [SupplyOrder].TransportationServiceID = [TransportationService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [TransportationServiceDetailItem] " +
            "ON [TransportationServiceDetailItem].TransportationServiceID = [TransportationService].ID " +
            "AND [TransportationServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [TransportationServiceDetailItemKey] " +
            "ON [TransportationServiceDetailItemKey].ID = [TransportationServiceDetailItem].ServiceDetailItemKeyID " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(CustomService),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey)
        };

        Func<object[], SupplyOrder> detailItemsMapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            CustomService customService = (CustomService)objects[1];
            ServiceDetailItem customServiceDetailItem = (ServiceDetailItem)objects[2];
            ServiceDetailItemKey customServiceDetailItemKey = (ServiceDetailItemKey)objects[3];
            ServiceDetailItem portCustomAgencyServiceDetailItem = (ServiceDetailItem)objects[4];
            ServiceDetailItemKey portCustomAgencyServiceDetailItemKey = (ServiceDetailItemKey)objects[5];
            ServiceDetailItem portWorkServiceDetailItem = (ServiceDetailItem)objects[6];
            ServiceDetailItemKey portWorkServiceDetailItemKey = (ServiceDetailItemKey)objects[7];
            ServiceDetailItem customAgencyServiceDetailItem = (ServiceDetailItem)objects[8];
            ServiceDetailItemKey customAgencyServiceDetailItemKey = (ServiceDetailItemKey)objects[9];
            ServiceDetailItem transportationServiceDetailItem = (ServiceDetailItem)objects[10];
            ServiceDetailItemKey transportationServiceDetailItemKey = (ServiceDetailItemKey)objects[11];

            if (customService != null && customServiceDetailItem != null) {
                CustomService fromList = supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id));

                if (!fromList.ServiceDetailItems.Any(i => i.Id.Equals(customServiceDetailItem.Id))) {
                    customServiceDetailItem.ServiceDetailItemKey = customServiceDetailItemKey;

                    fromList.ServiceDetailItems.Add(customServiceDetailItem);
                }
            }

            if (portCustomAgencyServiceDetailItem != null &&
                !supplyOrderToReturn.PortCustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(portCustomAgencyServiceDetailItem.Id))) {
                portCustomAgencyServiceDetailItem.ServiceDetailItemKey = portCustomAgencyServiceDetailItemKey;

                supplyOrderToReturn.PortCustomAgencyService.ServiceDetailItems.Add(portCustomAgencyServiceDetailItem);
            }

            if (portWorkServiceDetailItem != null && !supplyOrderToReturn.PortWorkService.ServiceDetailItems.Any(i => i.Id.Equals(portWorkServiceDetailItem.Id))) {
                portWorkServiceDetailItem.ServiceDetailItemKey = portWorkServiceDetailItemKey;

                supplyOrderToReturn.PortWorkService.ServiceDetailItems.Add(portWorkServiceDetailItem);
            }

            if (transportationServiceDetailItem != null &&
                !supplyOrderToReturn.TransportationService.ServiceDetailItems.Any(i => i.Id.Equals(transportationServiceDetailItem.Id))) {
                transportationServiceDetailItem.ServiceDetailItemKey = transportationServiceDetailItemKey;

                supplyOrderToReturn.TransportationService.ServiceDetailItems.Add(transportationServiceDetailItem);
            }

            if (customAgencyServiceDetailItem == null ||
                supplyOrderToReturn.CustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(customAgencyServiceDetailItem.Id))) return supplyOrder;

            customAgencyServiceDetailItem.ServiceDetailItemKey = customAgencyServiceDetailItemKey;

            supplyOrderToReturn.CustomAgencyService.ServiceDetailItems.Add(customAgencyServiceDetailItem);

            return supplyOrder;
        };

        _connection.Query(sqlExpression, types, detailItemsMapper, props);

        sqlExpression =
            "SELECT [SupplyOrder].ID " +
            ",[SupplyOrderContainerService].* " +
            ",[ContainerService].* " +
            ",[BillOfLadingDocument].* " +
            ",[ContainerServiceUser].* " +
            ",[ContainerServiceTask].* " +
            ",[ContainerServiceTaskUser].* " +
            ",[ContainerServiceAccountingPaymentTask].* " +
            ",[ContainerServiceAccountingPaymentTaskUser].* " +
            ",[SupplyInformationTask].* " +
            ",[SupplyInformationTaskUser].* " +
            ",[ActProvidingServiceDocument].* " +
            ",[SupplyServiceAccountDocument].* " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderContainerService] " +
            "ON [SupplyOrderContainerService].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyOrderContainerService].Deleted = 0 " +
            "LEFT JOIN [ContainerService] " +
            "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
            "LEFT JOIN [BillOfLadingDocument] " +
            "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
            "LEFT JOIN [User] AS [ContainerServiceUser] " +
            "ON [ContainerServiceUser].ID = [ContainerService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [ContainerServiceTask] " +
            "ON [ContainerServiceTask].ID = [ContainerService].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [ContainerServiceTaskUser] " +
            "ON [ContainerServiceTaskUser].ID = [ContainerServiceTask].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [ContainerServiceAccountingPaymentTask] " +
            "ON [ContainerServiceAccountingPaymentTask].ID = [ContainerService].AccountingPaymentTaskID " +
            "LEFT JOIN [User] AS [ContainerServiceAccountingPaymentTaskUser] " +
            "ON [ContainerServiceAccountingPaymentTaskUser].ID = [ContainerServiceAccountingPaymentTask].UserID " +
            "LEFT JOIN [SupplyInformationTask] " +
            "ON [SupplyInformationTask].[ID] = [ContainerService].[SupplyInformationTaskID] " +
            "AND [SupplyInformationTask].[Deleted] = 0 " +
            "LEFT JOIN [User] AS [SupplyInformationTaskUser] " +
            "ON [SupplyInformationTaskUser].[ID] = [SupplyInformationTask].[UserID] " +
            "LEFT JOIN [ActProvidingServiceDocument] " +
            "ON [ActProvidingServiceDocument].[ID] = [ContainerService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] " +
            "ON [SupplyServiceAccountDocument].[ID] = [ContainerService].[SupplyServiceAccountDocumentID] " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(SupplyOrderContainerService),
            typeof(ContainerService),
            typeof(BillOfLadingDocument),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationTask),
            typeof(User),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], SupplyOrder> containerServicesMapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyOrderContainerService supplyOrderContainerService = (SupplyOrderContainerService)objects[1];
            ContainerService containerService = (ContainerService)objects[2];
            BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[3];
            User containerServiceUser = (User)objects[4];
            SupplyPaymentTask containerServiceTask = (SupplyPaymentTask)objects[5];
            User containerServiceTaskUser = (User)objects[6];
            SupplyPaymentTask containerServiceAccountingPaymentTask = (SupplyPaymentTask)objects[7];
            User containerServiceAccountingPaymentTaskUser = (User)objects[8];
            SupplyInformationTask supplyInformationTask = (SupplyInformationTask)objects[9];
            User supplyInformationTaskUser = (User)objects[10];
            ActProvidingServiceDocument actProvidingServiceDocument = (ActProvidingServiceDocument)objects[11];
            SupplyServiceAccountDocument supplyServiceAccountDocument = (SupplyServiceAccountDocument)objects[12];

            if (supplyOrderContainerService == null ||
                supplyOrderToReturn.SupplyOrderContainerServices.Any(s => s.Id.Equals(supplyOrderContainerService.Id)))
                return supplyOrder;

            if (containerServiceTask != null) {
                containerServiceTask.User = containerServiceTaskUser;

                containerService.SupplyPaymentTask = containerServiceTask;
            }

            if (containerServiceAccountingPaymentTask != null) {
                containerServiceAccountingPaymentTask.User = containerServiceAccountingPaymentTaskUser;

                containerService.AccountingPaymentTask = containerServiceAccountingPaymentTask;
            }

            if (supplyInformationTask != null) {
                supplyInformationTask.User = supplyInformationTaskUser;

                containerService.SupplyInformationTask = supplyInformationTask;
            }

            containerService.ActProvidingServiceDocument = actProvidingServiceDocument;
            containerService.SupplyServiceAccountDocument = supplyServiceAccountDocument;
            containerService.BillOfLadingDocument = billOfLadingDocument;
            containerService.User = containerServiceUser;

            supplyOrderContainerService.ContainerService = containerService;

            supplyOrderToReturn.SupplyOrderContainerServices.Add(supplyOrderContainerService);

            return supplyOrder;
        };

        _connection.Query(sqlExpression, types, containerServicesMapper, props);

        if (!supplyOrderToReturn.SupplyOrderContainerServices.Any()) return supplyOrderToReturn;

        sqlExpression =
            "SELECT " +
            "[SupplyOrderContainerService].ID " +
            ",[ContainerService].ID " +
            ",[ContainerOrganization].* " +
            "FROM [SupplyOrderContainerService] " +
            "LEFT JOIN [ContainerService] " +
            "ON [ContainerService].ID = [SupplyOrderContainerService].ContainerServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
            "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
            "WHERE [SupplyOrderContainerService].ID IN @Ids";

        types = new[] {
            typeof(SupplyOrderContainerService),
            typeof(ContainerService),
            typeof(SupplyOrganization)
        };

        Func<object[], SupplyOrderContainerService> containerServiceOrganizationsMapper = objects => {
            SupplyOrderContainerService supplyOrderContainerService = (SupplyOrderContainerService)objects[0];
            SupplyOrganization containerOrganization = (SupplyOrganization)objects[2];

            if (containerOrganization != null)
                supplyOrderToReturn.SupplyOrderContainerServices
                    .First(s => s.Id.Equals(supplyOrderContainerService.Id)).ContainerService.ContainerOrganization = containerOrganization;

            return supplyOrderContainerService;
        };

        var containerServiceProps = new {
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            Ids = supplyOrderToReturn.SupplyOrderContainerServices.Select(s => s.Id)
        };

        _connection.Query(
            sqlExpression,
            types,
            containerServiceOrganizationsMapper,
            containerServiceProps
        );

        _connection.Query<SupplyOrderContainerService, ContainerService, InvoiceDocument, SupplyOrderContainerService>(
            "SELECT " +
            "[SupplyOrderContainerService].ID " +
            ",[ContainerService].ID " +
            ",[InvoiceDocument].* " +
            "FROM [SupplyOrderContainerService] " +
            "LEFT JOIN [ContainerService] " +
            "ON [ContainerService].ID = [SupplyOrderContainerService].ContainerServiceID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].ContainerServiceID = [ContainerService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "WHERE [SupplyOrderContainerService].ID IN @Ids",
            (junction, service, document) => {
                if (document == null) return junction;

                ContainerService fromList = supplyOrderToReturn.SupplyOrderContainerServices.First(s => s.Id.Equals(junction.Id)).ContainerService;

                if (!fromList.InvoiceDocuments.Any(d => d.Id.Equals(document.Id))) fromList.InvoiceDocuments.Add(document);

                return junction;
            },
            containerServiceProps
        );

        _connection.Query<PackingList, SupplyInvoice, SupplyOrder, Client, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "WHERE [PackingList].ContainerServiceID IN @Ids " +
            "AND [PackingList].Deleted = 0",
            (packingList, invoice, supplyOrder, client) => {
                supplyOrder.Client = client;

                invoice.SupplyOrder = supplyOrder;

                packingList.SupplyInvoice = invoice;

                supplyOrderToReturn.SupplyOrderContainerServices
                    .First(s => packingList.ContainerServiceId != null && s.ContainerServiceId.Equals(packingList.ContainerServiceId.Value))
                    .ContainerService.PackingLists.Add(packingList);

                return packingList;
            },
            new { Ids = supplyOrderToReturn.SupplyOrderContainerServices.Select(s => s.ContainerServiceId) }
        );

        return supplyOrderToReturn;
    }

    private SupplyOrder GetSupplyOrderWithVehicleServicesByNetId(Guid netId) {
        SupplyOrder supplyOrderToReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN (" +
            "SELECT [ClientAgreement].ID " +
            ",[ClientAgreement].AgreementID " +
            ",[ClientAgreement].ClientID " +
            "FROM ClientAgreement " +
            ") AS [ClientAgreement] " +
            "ON [ClientAgreement].ID = (" +
            "SELECT TOP(1) [ClientAgreement].ID " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [ClientAgreement].ClientID = [Client].ID " +
            "AND [Agreement].IsActive = 1" +
            ") " +
            "LEFT JOIN (" +
            "SELECT [Agreement].ID " +
            ",[Agreement].AmountDebt " +
            ",[Agreement].CurrencyID " +
            ",[Agreement].NumberDaysDebt " +
            ",[Agreement].IsAccounting " +
            ",[Agreement].IsControlAmountDebt " +
            ",[Agreement].IsControlNumberDaysDebt " +
            ",[Agreement].IsManagementAccounting " +
            ",[Agreement].WithVATAccounting " +
            ",[Agreement].IsActive " +
            ",[Agreement].Name " +
            ",[Agreement].DeferredPayment " +
            ",[Agreement].TermsOfPayment " +
            ",[Agreement].IsPrePaymentFull " +
            ",[Agreement].PrePaymentPercentages " +
            ",[Agreement].IsPrePayment " +
            "FROM [Agreement]" +
            ") AS [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN (" +
            "SELECT [SupplyOrderNumber].ID " +
            ",[SupplyOrderNumber].Number " +
            "FROM [SupplyOrderNumber]" +
            ") AS [SupplyOrderNumber] " +
            "ON SupplyOrderNumber.ID = SupplyOrder.SupplyOrderNumberID " +
            "LEFT JOIN [SupplyProForm] " +
            "ON [SupplyProForm].ID = [SupplyOrder].SupplyProFormID " +
            "LEFT JOIN [ProFormDocument] " +
            "ON [ProFormDocument].SupplyProFormID = [SupplyProForm].ID " +
            "AND [ProFormDocument].Deleted = 0 " +
            "LEFT JOIN [SupplyInformationDeliveryProtocol] AS [ProFormInformationProtocol] " +
            "ON [ProFormInformationProtocol].SupplyProFormID = [SupplyProForm].ID " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKey].ID " +
            ",[SupplyInformationDeliveryProtocolKey].[Key] " +
            "FROM [SupplyInformationDeliveryProtocolKey]" +
            ") AS [ProFormInformationProtocolKey] " +
            "ON [ProFormInformationProtocolKey].ID = [ProFormInformationProtocol].SupplyInformationDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [SupplyInformationDeliveryProtocolKeyTranslation].ID, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].CultureCode, " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].[Key], " +
            "[SupplyInformationDeliveryProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID " +
            "FROM [SupplyInformationDeliveryProtocolKeyTranslation] " +
            ") AS [ProFormInformationProtocolKeyTranslation] " +
            "ON [ProFormInformationProtocolKeyTranslation].SupplyInformationDeliveryProtocolKeyID = [ProFormInformationProtocolKey].ID " +
            "AND [ProFormInformationProtocolKeyTranslation].CultureCode = @Culture " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormInformationProtocolUser] " +
            "ON [ProFormInformationProtocolUser].ID = [ProFormInformationProtocol].UserID " +
            "LEFT JOIN [SupplyOrderPaymentDeliveryProtocol] AS [ProFormPaymentProtocol] " +
            "ON [ProFormPaymentProtocol].SupplyProFormID = [SupplyProForm].ID " +
            "AND [ProFormPaymentProtocol].Deleted = 0 " +
            "LEFT JOIN (" +
            "SELECT [SupplyOrderPaymentDeliveryProtocolKey].ID " +
            ",[SupplyOrderPaymentDeliveryProtocolKey].[Key] " +
            "FROM [SupplyOrderPaymentDeliveryProtocolKey] " +
            ") AS [ProFormPaymentProtocolKey] " +
            "ON [ProFormPaymentProtocolKey].ID = [ProFormPaymentProtocol].SupplyOrderPaymentDeliveryProtocolKeyID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormPaymentProtocolUser] " +
            "ON [ProFormPaymentProtocolUser].ID = [ProFormPaymentProtocol].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [ProFormPaymentProtocolTask] " +
            "ON [ProFormPaymentProtocolTask].ID = [ProFormPaymentProtocol].SupplyPaymentTaskID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [ProFormPaymentProtocolTaskUser] " +
            "ON [ProFormPaymentProtocolTaskUser].ID = [ProFormPaymentProtocolTask].UserID " +
            "LEFT JOIN [CustomService] " +
            "ON [CustomService].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [SupplyOrganization] AS [ExciseDutyOrganization] " +
            "ON [ExciseDutyOrganization].ID = [CustomService].ExciseDutyOrganizationID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomOrganization] " +
            "ON [CustomOrganization].ID = [CustomService].CustomOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServiceUser] " +
            "ON [CustomServiceUser].ID = [CustomService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomServicePaymentTask] " +
            "ON [CustomServicePaymentTask].ID = [CustomService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomServiceAccountingPaymentTask] " +
            "ON [CustomServiceAccountingPaymentTask].[ID] = [CustomService].[AccountingPaymentTaskID] " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServicePaymentTaskUser] " +
            "ON [CustomServicePaymentTaskUser].ID = [CustomServicePaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomServiceAccountingPaymentTaskUser] " +
            "ON [CustomServiceAccountingPaymentTaskUser].ID = [CustomServiceAccountingPaymentTask].UserID " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [PortCustomAgencyService].ID = [SupplyOrder].PortCustomAgencyServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [PortCustomAgencyOrganization] " +
            "ON [PortCustomAgencyOrganization].ID = [PortCustomAgencyService].PortCustomAgencyOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [PortCustomAgencyServiceUser] " +
            "ON [PortCustomAgencyServiceUser].ID = [PortCustomAgencyService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PortCustomAgencyPaymentTask] " +
            "ON [PortCustomAgencyPaymentTask].ID = [PortCustomAgencyService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [PortCustomAgencyAccountingPaymentTask] " +
            "ON [PortCustomAgencyAccountingPaymentTask].[ID] = [PortCustomAgencyService].[AccountingPaymentTaskID] " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User] " +
            ") AS [PortCustomAgencyPaymentTaskUser] " +
            "ON [PortCustomAgencyPaymentTaskUser].ID = [PortCustomAgencyPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User] " +
            ") AS [PortCustomAgencyAccountingPaymentTaskUser] " +
            "ON [PortCustomAgencyAccountingPaymentTaskUser].ID = [PortCustomAgencyAccountingPaymentTask].UserID " +
            "LEFT JOIN [VehicleDeliveryService] " +
            "ON [VehicleDeliveryService].ID = [SupplyOrder].VehicleDeliveryServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [VehicleDeliveryOrganization] " +
            "ON [VehicleDeliveryOrganization].ID = [VehicleDeliveryService].VehicleDeliveryOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [VehicleDeliveryServiceUser] " +
            "ON [VehicleDeliveryServiceUser].ID = [VehicleDeliveryService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [VehicleDeliveryPaymentTask] " +
            "ON [VehicleDeliveryPaymentTask].ID = [VehicleDeliveryService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [VehicleDeliveryAccountingPaymentTask] " +
            "ON [VehicleDeliveryAccountingPaymentTask].[ID] = [VehicleDeliveryService].[AccountingPaymentTaskID] " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [VehicleDeliveryPaymentTaskUser] " +
            "ON [VehicleDeliveryPaymentTaskUser].ID = [VehicleDeliveryPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [VehicleDeliveryAccountingPaymentTaskUser] " +
            "ON [VehicleDeliveryAccountingPaymentTaskUser].ID = [VehicleDeliveryAccountingPaymentTask].UserID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
            "LEFT JOIN [SupplyOrganization] AS [CustomAgencyOrganization] " +
            "ON [CustomAgencyOrganization].ID = [CustomAgencyService].CustomAgencyOrganizationID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomAgencyServiceUser] " +
            "ON [CustomAgencyServiceUser].ID = [CustomAgencyService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomAgencyPaymentTask] " +
            "ON [CustomAgencyPaymentTask].ID = [CustomAgencyService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [CustomAgencyServiceAccountingPaymentTask] " +
            "ON [CustomAgencyServiceAccountingPaymentTask].[ID] = [CustomAgencyService].[AccountingPaymentTaskID] " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomAgencyPaymentTaskUser] " +
            "ON [CustomAgencyPaymentTaskUser].ID = [CustomAgencyPaymentTask].UserID " +
            "LEFT JOIN (" +
            "SELECT [User].ID " +
            ",[User].FirstName " +
            ",[User].MiddleName " +
            ",[User].LastName " +
            "FROM [User]" +
            ") AS [CustomAgencyAccountingPaymentTaskUser] " +
            "ON [CustomAgencyAccountingPaymentTaskUser].ID = [CustomAgencyServiceAccountingPaymentTask].UserID " +
            "LEFT JOIN [views].[CurrencyView] AS [AdditionalPaymentCurrency] " +
            "ON [AdditionalPaymentCurrency].ID = [SupplyOrder].AdditionalPaymentCurrencyID " +
            "AND [AdditionalPaymentCurrency].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInformationTask] AS [CustomServiceSupplyInformationTask] " +
            "ON [CustomServiceSupplyInformationTask].[ID] = [CustomService].[SupplyInformationTaskID] " +
            "AND [CustomServiceSupplyInformationTask].[Deleted] = 0 " +
            "LEFT JOIN [SupplyInformationTask] AS [PortCustomAgencyServiceSupplyInformationTask] " +
            "ON [PortCustomAgencyServiceSupplyInformationTask].[ID] = [PortCustomAgencyService].[SupplyInformationTaskID] " +
            "AND [PortCustomAgencyServiceSupplyInformationTask].[Deleted] = 0 " +
            "LEFT JOIN [SupplyInformationTask] AS [VehicleDeliveryServiceSupplyInformationTask] " +
            "ON [VehicleDeliveryServiceSupplyInformationTask].[ID] = [VehicleDeliveryService].[SupplyInformationTaskID] " +
            "AND [VehicleDeliveryServiceSupplyInformationTask].[Deleted] = 0 " +
            "LEFT JOIN [SupplyInformationTask] AS [CustomAgencyServiceSupplyInformationTask] " +
            "ON [CustomAgencyServiceSupplyInformationTask].[ID] = [CustomAgencyService].[SupplyInformationTaskID] " +
            "AND [CustomAgencyServiceSupplyInformationTask].[Deleted] = 0 " +
            "LEFT JOIN [User] AS [UserCustomServiceSupplyInformationTask] " +
            "ON [UserCustomServiceSupplyInformationTask].[ID] = [CustomServiceSupplyInformationTask].[UserID] " +
            "LEFT JOIN [User] AS [UserPortCustomAgencyServiceSupplyInformationTask] " +
            "ON [UserPortCustomAgencyServiceSupplyInformationTask].[ID] = [PortCustomAgencyServiceSupplyInformationTask].[UserID] " +
            "LEFT JOIN [User] AS [UserVehicleDeliveryServiceSupplyInformationTask] " +
            "ON [UserVehicleDeliveryServiceSupplyInformationTask].[ID] = [VehicleDeliveryServiceSupplyInformationTask].[UserID] " +
            "LEFT JOIN [User] AS [UserCustomAgencyServiceSupplyInformationTask] " +
            "ON [UserCustomAgencyServiceSupplyInformationTask].[ID] = [CustomAgencyServiceSupplyInformationTask].[UserID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentCustomService] " +
            "ON [ActProvidingServiceDocumentCustomService].[ID] = [CustomService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentCustomService] " +
            "ON [SupplyServiceAccountDocumentCustomService].[ID] = [CustomService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentPortCustomAgencyService] " +
            "ON [ActProvidingServiceDocumentPortCustomAgencyService].[ID] = [PortCustomAgencyService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentPortCustomAgencyService] " +
            "ON [SupplyServiceAccountDocumentPortCustomAgencyService].[ID] = [PortCustomAgencyService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentVehicleDeliveryService] " +
            "ON [ActProvidingServiceDocumentVehicleDeliveryService].[ID] = [VehicleDeliveryService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentVehicleDeliveryService] " +
            "ON [SupplyServiceAccountDocumentVehicleDeliveryService].[ID] = [VehicleDeliveryService].[SupplyServiceAccountDocumentID] " +
            "LEFT JOIN [ActProvidingServiceDocument] AS [ActProvidingServiceDocumentCustomAgencyService] " +
            "ON [ActProvidingServiceDocumentCustomAgencyService].[ID] = [CustomAgencyService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] AS [SupplyServiceAccountDocumentCustomAgencyService] " +
            "ON [SupplyServiceAccountDocumentCustomAgencyService].[ID] = [CustomAgencyService].[SupplyServiceAccountDocumentID] " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        Type[] types = {
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization),
            typeof(SupplyOrderNumber),
            typeof(SupplyProForm),
            typeof(ProFormDocument),
            typeof(SupplyInformationDeliveryProtocol),
            typeof(SupplyInformationDeliveryProtocolKey),
            typeof(SupplyInformationDeliveryProtocolKeyTranslation),
            typeof(User),
            typeof(SupplyOrderPaymentDeliveryProtocol),
            typeof(SupplyOrderPaymentDeliveryProtocolKey),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(CustomService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(PortCustomAgencyService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(VehicleDeliveryService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(CustomAgencyService),
            typeof(SupplyOrganization),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(User),
            typeof(Currency),
            typeof(SupplyInformationTask),
            typeof(SupplyInformationTask),
            typeof(SupplyInformationTask),
            typeof(SupplyInformationTask),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(User),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], SupplyOrder> mapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            Client client = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Currency currency = (Currency)objects[4];
            Organization organization = (Organization)objects[5];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[6];
            SupplyProForm supplyProForm = (SupplyProForm)objects[7];
            ProFormDocument proFormDocument = (ProFormDocument)objects[8];
            SupplyInformationDeliveryProtocol proFormInformationProtocol = (SupplyInformationDeliveryProtocol)objects[9];
            SupplyInformationDeliveryProtocolKey proFormInformationProtocolKey = (SupplyInformationDeliveryProtocolKey)objects[10];
            SupplyInformationDeliveryProtocolKeyTranslation proFormInformationProtocolKeyTranslation = (SupplyInformationDeliveryProtocolKeyTranslation)objects[11];
            User proFormInformationProtocolUser = (User)objects[12];
            SupplyOrderPaymentDeliveryProtocol proFormPaymentProtocol = (SupplyOrderPaymentDeliveryProtocol)objects[13];
            SupplyOrderPaymentDeliveryProtocolKey proFormPaymentProtocolKey = (SupplyOrderPaymentDeliveryProtocolKey)objects[14];
            User proFormPaymentProtocolUser = (User)objects[15];
            SupplyPaymentTask proFormPaymentProtocolTask = (SupplyPaymentTask)objects[16];
            User proFormPaymentProtocolTaskUser = (User)objects[17];
            CustomService customService = (CustomService)objects[18];
            SupplyOrganization exciseDutyOrganization = (SupplyOrganization)objects[19];
            SupplyOrganization customOrganization = (SupplyOrganization)objects[20];
            User customServiceUser = (User)objects[21];
            SupplyPaymentTask customServicePaymentTask = (SupplyPaymentTask)objects[22];
            SupplyPaymentTask customServiceAccountingPaymentTask = (SupplyPaymentTask)objects[23];
            User customServicePaymentTaskUser = (User)objects[24];
            User customServiceAccountingPaymentTaskUser = (User)objects[25];
            PortCustomAgencyService portCustomAgencyService = (PortCustomAgencyService)objects[26];
            SupplyOrganization portCustomAgencyOrganization = (SupplyOrganization)objects[27];
            User portCustomAgencyServiceUser = (User)objects[28];
            SupplyPaymentTask portCustomAgencyPaymentTask = (SupplyPaymentTask)objects[29];
            SupplyPaymentTask portCustomAgencyAccountingPaymentTask = (SupplyPaymentTask)objects[30];
            User portCustomAgencyPaymentTaskUser = (User)objects[31];
            User portCustomAgencyAccountingPaymentTaskUser = (User)objects[32];
            VehicleDeliveryService vehicleDeliveryService = (VehicleDeliveryService)objects[33];
            SupplyOrganization vehicleDeliveryOrganization = (SupplyOrganization)objects[34];
            User vehicleDeliveryServiceUser = (User)objects[35];
            SupplyPaymentTask vehicleDeliveryPaymentTask = (SupplyPaymentTask)objects[36];
            SupplyPaymentTask vehicleDeliveryAccountingPaymentTask = (SupplyPaymentTask)objects[37];
            User vehicleDeliveryPaymentTaskUser = (User)objects[38];
            User vehicleDeliveryAccountingPaymentTaskUser = (User)objects[39];
            CustomAgencyService customAgencyService = (CustomAgencyService)objects[40];
            SupplyOrganization customAgencyOrganization = (SupplyOrganization)objects[41];
            User customAgencyServiceUser = (User)objects[42];
            SupplyPaymentTask customAgencyPaymentTask = (SupplyPaymentTask)objects[43];
            SupplyPaymentTask customAgencyAccountingPaymentTask = (SupplyPaymentTask)objects[44];
            User customAgencyPaymentTaskUser = (User)objects[45];
            User customAgencyAccountingPaymentTaskUser = (User)objects[46];
            Currency additionalPaymentCurrency = (Currency)objects[47];
            SupplyInformationTask customServiceInformationTask = (SupplyInformationTask)objects[48];
            SupplyInformationTask portCustomAgencyInformationTask = (SupplyInformationTask)objects[49];
            SupplyInformationTask vehicleDeliveryServiceInformationTask = (SupplyInformationTask)objects[50];
            SupplyInformationTask customAgencyInformationTask = (SupplyInformationTask)objects[51];
            User userCustomServiceInformationTask = (User)objects[52];
            User userPortCustomAgencyInformationTask = (User)objects[53];
            User userVehicleDeliveryServiceInformationTask = (User)objects[54];
            User userCustomAgencyInformationTask = (User)objects[55];
            ActProvidingServiceDocument actProvidingServiceDocumentCustomService = (ActProvidingServiceDocument)objects[56];
            SupplyServiceAccountDocument supplyServiceAccountDocumentCustomService = (SupplyServiceAccountDocument)objects[57];
            ActProvidingServiceDocument actProvidingServiceDocumentPortCustomAgencyService = (ActProvidingServiceDocument)objects[58];
            SupplyServiceAccountDocument supplyServiceAccountDocumentPortCustomAgencyService = (SupplyServiceAccountDocument)objects[59];
            ActProvidingServiceDocument actProvidingServiceDocumentVehicleDeliveryService = (ActProvidingServiceDocument)objects[60];
            SupplyServiceAccountDocument supplyServiceAccountDocumentVehicleDeliveryService = (SupplyServiceAccountDocument)objects[61];
            ActProvidingServiceDocument actProvidingServiceDocumentCustomAgencyService = (ActProvidingServiceDocument)objects[62];
            SupplyServiceAccountDocument supplyServiceAccountDocumentCustomAgencyService = (SupplyServiceAccountDocument)objects[63];

            if (supplyOrderToReturn != null) {
                if (supplyProForm == null) return supplyOrder;

                if (proFormDocument != null && !supplyOrderToReturn.SupplyProForm.ProFormDocuments.Any(d => d.Id.Equals(proFormDocument.Id)))
                    supplyOrderToReturn.SupplyProForm.ProFormDocuments.Add(proFormDocument);

                if (proFormInformationProtocol != null &&
                    !supplyOrderToReturn.SupplyProForm.InformationDeliveryProtocols.Any(p => p.Id.Equals(proFormInformationProtocol.Id))) {
                    proFormInformationProtocolKey.Key = proFormInformationProtocolKeyTranslation?.Key ?? proFormInformationProtocolKey.Key;
                    proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;
                    proFormInformationProtocol.User = proFormInformationProtocolUser;

                    supplyOrderToReturn.SupplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                }

                if (proFormPaymentProtocol != null && !supplyOrderToReturn.SupplyProForm.PaymentDeliveryProtocols.Any(p => p.Id.Equals(proFormPaymentProtocol.Id))) {
                    if (proFormPaymentProtocolTask != null) {
                        proFormPaymentProtocolTask.User = proFormPaymentProtocolTaskUser;

                        proFormPaymentProtocol.SupplyPaymentTask = proFormPaymentProtocolTask;
                    }

                    proFormPaymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = proFormPaymentProtocolKey;
                    proFormPaymentProtocol.User = proFormPaymentProtocolUser;

                    supplyOrderToReturn.SupplyProForm.PaymentDeliveryProtocols.Add(proFormPaymentProtocol);
                }

                if (customService == null || supplyOrderToReturn.CustomServices.Any(s => s.Id.Equals(customService.Id))) return supplyOrder;


                if (customServicePaymentTask != null) {
                    customServicePaymentTask.User = customServicePaymentTaskUser;

                    customService.SupplyPaymentTask = customServicePaymentTask;

                    customService.SupplyInformationTask = customServiceInformationTask;
                }

                if (customServiceAccountingPaymentTask != null) {
                    customServiceAccountingPaymentTask.User = customServiceAccountingPaymentTaskUser;

                    customService.AccountingPaymentTask = customServiceAccountingPaymentTask;
                }

                if (customServiceInformationTask != null) {
                    customServiceInformationTask.User = userCustomServiceInformationTask;
                    customService.SupplyInformationTask = customServiceInformationTask;
                }

                customService.ActProvidingServiceDocument = actProvidingServiceDocumentCustomService;
                customService.SupplyServiceAccountDocument = supplyServiceAccountDocumentCustomService;
                customService.CustomOrganization = customOrganization;
                customService.ExciseDutyOrganization = exciseDutyOrganization;
                customService.User = customServiceUser;

                supplyOrderToReturn.CustomServices.Add(customService);
            } else {
                if (client != null) {
                    if (clientAgreement != null) {
                        if (agreement != null) {
                            agreement.Currency = currency;

                            clientAgreement.Agreement = agreement;
                        }

                        client.ClientAgreements.Add(clientAgreement);
                    }

                    supplyOrder.Client = client;
                }

                if (supplyProForm != null) {
                    if (proFormDocument != null) supplyProForm.ProFormDocuments.Add(proFormDocument);

                    if (proFormInformationProtocol != null) {
                        proFormInformationProtocolKey.Key = proFormInformationProtocolKeyTranslation?.Key ?? proFormInformationProtocolKey.Key;
                        proFormInformationProtocol.SupplyInformationDeliveryProtocolKey = proFormInformationProtocolKey;
                        proFormInformationProtocol.User = proFormInformationProtocolUser;

                        supplyProForm.InformationDeliveryProtocols.Add(proFormInformationProtocol);
                    }

                    if (proFormPaymentProtocol != null) {
                        if (proFormPaymentProtocolTask != null) {
                            proFormPaymentProtocolTask.User = proFormPaymentProtocolTaskUser;

                            proFormPaymentProtocol.SupplyPaymentTask = proFormPaymentProtocolTask;
                        }

                        proFormPaymentProtocol.SupplyOrderPaymentDeliveryProtocolKey = proFormPaymentProtocolKey;
                        proFormPaymentProtocol.User = proFormPaymentProtocolUser;

                        supplyProForm.PaymentDeliveryProtocols.Add(proFormPaymentProtocol);
                    }

                    supplyOrder.SupplyProForm = supplyProForm;
                }

                if (customService != null) {
                    if (customServicePaymentTask != null) {
                        customServicePaymentTask.User = customServicePaymentTaskUser;

                        customService.SupplyPaymentTask = customServicePaymentTask;
                    }

                    if (customServiceAccountingPaymentTask != null) {
                        customServiceAccountingPaymentTask.User = customServiceAccountingPaymentTaskUser;

                        customService.AccountingPaymentTask = customServiceAccountingPaymentTask;
                    }

                    if (customServiceInformationTask != null) {
                        customServiceInformationTask.User = userCustomServiceInformationTask;
                        customService.SupplyInformationTask = customServiceInformationTask;
                    }

                    customService.CustomOrganization = customOrganization;
                    customService.ExciseDutyOrganization = exciseDutyOrganization;
                    customService.User = customServiceUser;

                    supplyOrder.CustomServices.Add(customService);
                }

                if (portCustomAgencyService != null) {
                    if (portCustomAgencyPaymentTask != null) {
                        portCustomAgencyPaymentTask.User = portCustomAgencyPaymentTaskUser;

                        portCustomAgencyService.SupplyPaymentTask = portCustomAgencyPaymentTask;
                    }

                    if (portCustomAgencyAccountingPaymentTask != null) {
                        portCustomAgencyAccountingPaymentTask.User = portCustomAgencyAccountingPaymentTaskUser;

                        portCustomAgencyService.AccountingPaymentTask = portCustomAgencyAccountingPaymentTask;
                    }

                    if (portCustomAgencyInformationTask != null) {
                        portCustomAgencyInformationTask.User = userPortCustomAgencyInformationTask;
                        portCustomAgencyService.SupplyInformationTask = portCustomAgencyInformationTask;
                    }

                    portCustomAgencyService.ActProvidingServiceDocument = actProvidingServiceDocumentPortCustomAgencyService;
                    portCustomAgencyService.SupplyServiceAccountDocument = supplyServiceAccountDocumentPortCustomAgencyService;
                    portCustomAgencyService.PortCustomAgencyOrganization = portCustomAgencyOrganization;
                    portCustomAgencyService.User = portCustomAgencyServiceUser;

                    supplyOrder.PortCustomAgencyService = portCustomAgencyService;
                }

                if (vehicleDeliveryService != null) {
                    if (vehicleDeliveryPaymentTask != null) {
                        vehicleDeliveryPaymentTask.User = vehicleDeliveryPaymentTaskUser;

                        vehicleDeliveryService.SupplyPaymentTask = vehicleDeliveryPaymentTask;
                    }

                    if (vehicleDeliveryAccountingPaymentTask != null) {
                        vehicleDeliveryAccountingPaymentTask.User = vehicleDeliveryAccountingPaymentTaskUser;

                        vehicleDeliveryService.AccountingPaymentTask = vehicleDeliveryAccountingPaymentTask;
                    }

                    if (vehicleDeliveryServiceInformationTask != null) {
                        vehicleDeliveryServiceInformationTask.User = userVehicleDeliveryServiceInformationTask;
                        vehicleDeliveryService.SupplyInformationTask = vehicleDeliveryServiceInformationTask;
                    }

                    vehicleDeliveryService.ActProvidingServiceDocument = actProvidingServiceDocumentVehicleDeliveryService;
                    vehicleDeliveryService.SupplyServiceAccountDocument = supplyServiceAccountDocumentVehicleDeliveryService;
                    vehicleDeliveryService.VehicleDeliveryOrganization = vehicleDeliveryOrganization;
                    vehicleDeliveryService.User = vehicleDeliveryServiceUser;

                    supplyOrder.VehicleDeliveryService = vehicleDeliveryService;
                }

                if (customAgencyService != null) {
                    if (customAgencyPaymentTask != null) {
                        customAgencyPaymentTask.User = customAgencyPaymentTaskUser;

                        customAgencyService.SupplyPaymentTask = customAgencyPaymentTask;
                    }

                    if (customAgencyAccountingPaymentTask != null) {
                        customAgencyAccountingPaymentTask.User = customAgencyAccountingPaymentTaskUser;

                        customAgencyService.AccountingPaymentTask = customAgencyAccountingPaymentTask;
                    }

                    if (customAgencyInformationTask != null) {
                        customAgencyInformationTask.User = userCustomAgencyInformationTask;
                        customAgencyService.SupplyInformationTask = customAgencyInformationTask;
                    }

                    customAgencyService.ActProvidingServiceDocument = actProvidingServiceDocumentCustomAgencyService;
                    customAgencyService.SupplyServiceAccountDocument = supplyServiceAccountDocumentCustomAgencyService;
                    customAgencyService.CustomAgencyOrganization = customAgencyOrganization;
                    customAgencyService.User = customAgencyServiceUser;

                    supplyOrder.CustomAgencyService = customAgencyService;
                }

                supplyOrder.AdditionalPaymentCurrency = additionalPaymentCurrency;
                supplyOrder.Organization = organization;
                supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                supplyOrderToReturn = supplyOrder;
            }

            return supplyOrder;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props, commandTimeout: 360);

        if (supplyOrderToReturn == null) return supplyOrderToReturn;

        sqlExpression =
            "SELECT * " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [VehicleDeliveryService] " +
            "ON [VehicleDeliveryService].ID = [SupplyOrder].VehicleDeliveryServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [VehicleDeliveryDocument] " +
            "ON [VehicleDeliveryService].ID = [VehicleDeliveryDocument].VehicleDeliveryServiceID " +
            "AND [VehicleDeliveryDocument].Deleted = 0 " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [PortCustomAgencyService].ID = [SupplyOrder].ID " +
            "LEFT JOIN [InvoiceDocument] AS [PortCustomAgencyDocument] " +
            "ON [PortCustomAgencyDocument].ID = [PortCustomAgencyDocument].PortCustomAgencyServiceID " +
            "AND [PortCustomAgencyDocument].Deleted = 0 " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [CustomAgencyService].ID = [SupplyOrder].CustomAgencyServiceID " +
            "LEFT JOIN [InvoiceDocument] AS [CustomAgencyDocument] " +
            "ON [CustomAgencyDocument].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "AND [CustomAgencyDocument].Deleted = 0 " +
            "LEFT JOIN [CustomService] " +
            "ON [CustomService].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [InvoiceDocument] AS [CustomServiceDocument] " +
            "ON [CustomServiceDocument].CustomServiceID = [CustomService].ID " +
            "AND [CustomServiceDocument].Deleted = 0 " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(VehicleDeliveryService),
            typeof(InvoiceDocument),
            typeof(PortCustomAgencyService),
            typeof(InvoiceDocument),
            typeof(CustomAgencyService),
            typeof(InvoiceDocument),
            typeof(CustomService),
            typeof(InvoiceDocument)
        };

        Func<object[], InvoiceDocument> documentsMapper = objects => {
            InvoiceDocument vehicleDeliveryDocument = (InvoiceDocument)objects[2];
            InvoiceDocument portCustomAgencyDocument = (InvoiceDocument)objects[4];
            InvoiceDocument customAgencyDocument = (InvoiceDocument)objects[6];
            CustomService customService = (CustomService)objects[7];
            InvoiceDocument customServiceDocument = (InvoiceDocument)objects[8];

            if (vehicleDeliveryDocument != null && !supplyOrderToReturn.VehicleDeliveryService.InvoiceDocuments.Any(d => d.Id.Equals(vehicleDeliveryDocument.Id)))
                supplyOrderToReturn.VehicleDeliveryService.InvoiceDocuments.Add(vehicleDeliveryDocument);

            if (portCustomAgencyDocument != null && !supplyOrderToReturn.PortCustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(portCustomAgencyDocument.Id)))
                supplyOrderToReturn.PortCustomAgencyService.InvoiceDocuments.Add(portCustomAgencyDocument);

            if (customAgencyDocument != null && !supplyOrderToReturn.CustomAgencyService.InvoiceDocuments.Any(d => d.Id.Equals(customAgencyDocument.Id)))
                supplyOrderToReturn.CustomAgencyService.InvoiceDocuments.Add(customAgencyDocument);

            if (customService == null || customServiceDocument == null) return vehicleDeliveryDocument;

            CustomService customServiceFromList = supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id));

            if (!customServiceFromList.InvoiceDocuments.Any(d => d.Id.Equals(customServiceDocument.Id))) customServiceFromList.InvoiceDocuments.Add(customServiceDocument);

            return vehicleDeliveryDocument;
        };

        _connection.Query(sqlExpression, types, documentsMapper, props);

        sqlExpression =
            "SELECT " +
            "[SupplyOrder].* " +
            ",[CustomService].* " +
            ",[CustomServiceDetailItem].* " +
            ",[CustomServiceDetailItemKey].* " +
            ",[PortCustomAgencyServiceDetailItem].* " +
            ",[PortCustomAgencyServiceDetailItemKey].* " +
            ",[VehicleDeliveryServiceDetailItem].* " +
            ",[VehicleDeliveryServiceDetailItemKey].* " +
            ",[CustomAgencyServiceDetailItem].* " +
            ",[CustomAgencyServiceDetailItemKey].* " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [CustomService] " +
            "ON [SupplyOrder].ID = [CustomService].SupplyOrderID " +
            "LEFT JOIN [ServiceDetailItem] AS [CustomServiceDetailItem] " +
            "ON [CustomServiceDetailItem].CustomServiceID = [CustomService].ID " +
            "AND [CustomServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [CustomServiceDetailItemKey] " +
            "ON [CustomServiceDetailItemKey].ID = [CustomServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [PortCustomAgencyService] " +
            "ON [SupplyOrder].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [PortCustomAgencyServiceDetailItem] " +
            "ON [PortCustomAgencyServiceDetailItem].PortCustomAgencyServiceID = [PortCustomAgencyService].ID " +
            "AND [PortCustomAgencyServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [PortCustomAgencyServiceDetailItemKey] " +
            "ON [PortCustomAgencyServiceDetailItemKey].ID = [PortCustomAgencyServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [VehicleDeliveryService] " +
            "ON [SupplyOrder].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [VehicleDeliveryServiceDetailItem] " +
            "ON [VehicleDeliveryServiceDetailItem].VehicleDeliveryServiceID = [VehicleDeliveryService].ID " +
            "AND [VehicleDeliveryServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [VehicleDeliveryServiceDetailItemKey] " +
            "ON [VehicleDeliveryServiceDetailItemKey].ID = [VehicleDeliveryServiceDetailItem].ServiceDetailItemKeyID " +
            "LEFT JOIN [CustomAgencyService] " +
            "ON [SupplyOrder].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "LEFT JOIN [ServiceDetailItem] AS [CustomAgencyServiceDetailItem] " +
            "ON [CustomAgencyServiceDetailItem].CustomAgencyServiceID = [CustomAgencyService].ID " +
            "AND [CustomAgencyServiceDetailItem].Deleted = 0 " +
            "LEFT JOIN [ServiceDetailItemKey] AS [CustomAgencyServiceDetailItemKey] " +
            "ON [CustomAgencyServiceDetailItemKey].ID = [CustomAgencyServiceDetailItem].ServiceDetailItemKeyID " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(CustomService),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey),
            typeof(ServiceDetailItem),
            typeof(ServiceDetailItemKey)
        };

        Func<object[], SupplyOrder> detailItemsMapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            CustomService customService = (CustomService)objects[1];
            ServiceDetailItem customServiceDetailItem = (ServiceDetailItem)objects[2];
            ServiceDetailItemKey customServiceDetailItemKey = (ServiceDetailItemKey)objects[3];
            ServiceDetailItem portCustomAgencyServiceDetailItem = (ServiceDetailItem)objects[4];
            ServiceDetailItemKey portCustomAgencyServiceDetailItemKey = (ServiceDetailItemKey)objects[5];
            ServiceDetailItem vehicleDeliveryServiceDetailItem = (ServiceDetailItem)objects[6];
            ServiceDetailItemKey vehicleDeliveryServiceDetailItemKey = (ServiceDetailItemKey)objects[7];
            ServiceDetailItem customAgencyServiceDetailItem = (ServiceDetailItem)objects[8];
            ServiceDetailItemKey customAgencyServiceDetailItemKey = (ServiceDetailItemKey)objects[9];

            if (customService != null && customServiceDetailItem != null) {
                CustomService fromList = supplyOrderToReturn.CustomServices.First(s => s.Id.Equals(customService.Id));

                if (!fromList.ServiceDetailItems.Any(i => i.Id.Equals(customServiceDetailItem.Id))) {
                    customServiceDetailItem.ServiceDetailItemKey = customServiceDetailItemKey;

                    fromList.ServiceDetailItems.Add(customServiceDetailItem);
                }
            }

            if (portCustomAgencyServiceDetailItem != null &&
                !supplyOrderToReturn.PortCustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(portCustomAgencyServiceDetailItem.Id))) {
                portCustomAgencyServiceDetailItem.ServiceDetailItemKey = portCustomAgencyServiceDetailItemKey;

                supplyOrderToReturn.PortCustomAgencyService.ServiceDetailItems.Add(portCustomAgencyServiceDetailItem);
            }

            if (vehicleDeliveryServiceDetailItem != null &&
                !supplyOrderToReturn.VehicleDeliveryService.ServiceDetailItems.Any(i => i.Id.Equals(vehicleDeliveryServiceDetailItem.Id))) {
                vehicleDeliveryServiceDetailItem.ServiceDetailItemKey = vehicleDeliveryServiceDetailItemKey;

                supplyOrderToReturn.VehicleDeliveryService.ServiceDetailItems.Add(vehicleDeliveryServiceDetailItem);
            }

            if (customAgencyServiceDetailItem == null ||
                supplyOrderToReturn.CustomAgencyService.ServiceDetailItems.Any(i => i.Id.Equals(customAgencyServiceDetailItem.Id))) return supplyOrder;

            customAgencyServiceDetailItem.ServiceDetailItemKey = customAgencyServiceDetailItemKey;

            supplyOrderToReturn.CustomAgencyService.ServiceDetailItems.Add(customAgencyServiceDetailItem);

            return supplyOrder;
        };

        _connection.Query(sqlExpression, types, detailItemsMapper, props);

        sqlExpression =
            "SELECT [SupplyOrder].ID " +
            ",[SupplyOrderVehicleService].* " +
            ",[VehicleService].* " +
            ",[BillOfLadingDocument].* " +
            ",[InvoiceDocument].* " +
            ",[VehicleServiceUser].* " +
            ",[VehicleServiceTask].* " +
            ",[VehicleServiceTaskUser].* " +
            ",[VehicleServiceAccountingPaymentTask].* " +
            ",[VehicleServiceAccountingPaymentTaskUser].* " +
            ",[SupplyInformationTask].* " +
            ",[SupplyInformationTaskUser].* " +
            ",[ActProvidingServiceDocument].* " +
            ",[SupplyServiceAccountDocument].* " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderVehicleService] " +
            "ON [SupplyOrderVehicleService].SupplyOrderID = [SupplyOrder].ID " +
            "AND [SupplyOrderVehicleService].Deleted = 0 " +
            "LEFT JOIN [VehicleService] " +
            "ON [SupplyOrderVehicleService].VehicleServiceID = [VehicleService].ID " +
            "LEFT JOIN [BillOfLadingDocument] " +
            "ON [BillOfLadingDocument].ID = [VehicleService].BillOfLadingDocumentID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].ID = [VehicleService].ID " +
            "LEFT JOIN [User] AS [VehicleServiceUser] " +
            "ON [VehicleServiceUser].ID = [VehicleService].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [VehicleServiceTask] " +
            "ON [VehicleServiceTask].ID = [VehicleService].SupplyPaymentTaskID " +
            "LEFT JOIN [User] AS [VehicleServiceTaskUser] " +
            "ON [VehicleServiceTaskUser].ID = [VehicleServiceTask].UserID " +
            "LEFT JOIN [SupplyPaymentTask] AS [VehicleServiceAccountingPaymentTask] " +
            "ON [VehicleServiceAccountingPaymentTask].[ID] = [VehicleService].[AccountingPaymentTaskID] " +
            "LEFT JOIN [User] AS [VehicleServiceAccountingPaymentTaskUser] " +
            "ON [VehicleServiceAccountingPaymentTaskUser].ID = [VehicleServiceAccountingPaymentTask].UserID " +
            "LEFT JOIN [SupplyInformationTask] " +
            "ON [SupplyInformationTask].[ID] = [VehicleService].[SupplyInformationTaskID] " +
            "AND [SupplyInformationTask].[Deleted] = 0 " +
            "LEFT JOIN [User] AS [SupplyInformationTaskUser] " +
            "ON [SupplyInformationTaskUser].[ID] = [SupplyInformationTask].[UserID] " +
            "LEFT JOIN [ActProvidingServiceDocument] " +
            "ON [ActProvidingServiceDocument].[ID] = [VehicleService].[ActProvidingServiceDocumentID] " +
            "LEFT JOIN [SupplyServiceAccountDocument] " +
            "ON [SupplyServiceAccountDocument].[ID] = [VehicleService].[SupplyServiceAccountDocumentID] " +
            "WHERE [SupplyOrder].NetUID = @NetId";

        types = new[] {
            typeof(SupplyOrder),
            typeof(SupplyOrderVehicleService),
            typeof(VehicleService),
            typeof(BillOfLadingDocument),
            typeof(InvoiceDocument),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(SupplyInformationTask),
            typeof(User),
            typeof(ActProvidingServiceDocument),
            typeof(SupplyServiceAccountDocument)
        };

        Func<object[], SupplyOrder> vehicleServicesMapper = objects => {
            SupplyOrder supplyOrder = (SupplyOrder)objects[0];
            SupplyOrderVehicleService supplyOrderVehicleService = (SupplyOrderVehicleService)objects[1];
            VehicleService vehicleService = (VehicleService)objects[2];
            BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[3];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[4];
            User vehicleServiceUser = (User)objects[5];
            SupplyPaymentTask vehicleServiceTask = (SupplyPaymentTask)objects[6];
            User vehicleServiceTaskUser = (User)objects[7];
            SupplyPaymentTask vehicleServiceAccountingPaymentTask = (SupplyPaymentTask)objects[8];
            User vehicleServiceAccountingPaymentTaskUser = (User)objects[9];
            SupplyInformationTask supplyInformationTask = (SupplyInformationTask)objects[10];
            User supplyInformationTaskUser = (User)objects[11];
            ActProvidingServiceDocument actProvidingServiceDocument = (ActProvidingServiceDocument)objects[12];
            SupplyServiceAccountDocument supplyServiceAccountDocument = (SupplyServiceAccountDocument)objects[13];

            if (supplyOrderVehicleService == null ||
                supplyOrderToReturn.SupplyOrderVehicleServices.Any(s => s.Id.Equals(supplyOrderVehicleService.Id)))
                return supplyOrder;

            if (vehicleService != null) {
                if (vehicleServiceTask != null) {
                    vehicleServiceTask.User = vehicleServiceTaskUser;

                    vehicleService.SupplyPaymentTask = vehicleServiceTask;
                }

                if (supplyInformationTask != null) {
                    supplyInformationTask.User = supplyInformationTaskUser;

                    vehicleService.SupplyInformationTask = supplyInformationTask;
                }

                if (vehicleServiceAccountingPaymentTask != null) {
                    vehicleServiceAccountingPaymentTask.User = vehicleServiceAccountingPaymentTaskUser;

                    vehicleService.AccountingPaymentTask = vehicleServiceAccountingPaymentTask;
                }

                if (invoiceDocument != null) {
                    if (!vehicleService.InvoiceDocuments.Any(x => x.Id.Equals(invoiceDocument.Id)))
                        vehicleService.InvoiceDocuments.Add(invoiceDocument);
                    else
                        invoiceDocument = vehicleService.InvoiceDocuments.FirstOrDefault(x => x.Id.Equals(invoiceDocument.Id));
                }

                if (billOfLadingDocument != null)
                    vehicleService.BillOfLadingDocument = billOfLadingDocument;

                vehicleService.ActProvidingServiceDocument = actProvidingServiceDocument;
                vehicleService.SupplyServiceAccountDocument = supplyServiceAccountDocument;
                vehicleService.User = vehicleServiceUser;

                supplyOrderVehicleService.VehicleService = vehicleService;
            }

            supplyOrderToReturn.SupplyOrderVehicleServices.Add(supplyOrderVehicleService);

            return supplyOrder;
        };

        _connection.Query(sqlExpression, types, vehicleServicesMapper, props);

        if (!supplyOrderToReturn.SupplyOrderVehicleServices.Any()) return supplyOrderToReturn;

        sqlExpression =
            "SELECT " +
            "[SupplyOrderVehicleService].[ID] " +
            ",[VehicleService].[ID] " +
            ",[VehicleOrganization].* " +
            ",[SupplyOrganizationAgreement].* " +
            ",[Currency].* " +
            "FROM [SupplyOrderVehicleService] " +
            "LEFT JOIN [VehicleService] " +
            "ON [VehicleService].ID = [SupplyOrderVehicleService].[VehicleServiceID] " +
            "LEFT JOIN [SupplyOrganization] AS [VehicleOrganization] " +
            "ON [VehicleOrganization].ID = [VehicleService].[VehicleOrganizationID] " +
            "LEFT JOIN [SupplyOrganizationAgreement] " +
            "ON [SupplyOrganizationAgreement].ID = [VehicleService].[SupplyOrganizationAgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [SupplyOrganizationAgreement].[CurrencyID] " +
            "WHERE [SupplyOrderVehicleService].[ID] IN @Ids";

        types = new[] {
            typeof(SupplyOrderVehicleService),
            typeof(VehicleService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganizationAgreement),
            typeof(Currency)
        };

        Func<object[], SupplyOrderVehicleService> vehicleServiceOrganizationsMapper = objects => {
            SupplyOrderVehicleService supplyOrderVehicleService = (SupplyOrderVehicleService)objects[0];
            SupplyOrganization vehicleOrganization = (SupplyOrganization)objects[2];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[3];
            Currency currency = (Currency)objects[4];

            if (vehicleOrganization != null) {
                supplyOrganizationAgreement.Currency = currency;

                supplyOrderToReturn.SupplyOrderVehicleServices
                        .First(s => s.Id.Equals(supplyOrderVehicleService.Id)).VehicleService.SupplyOrganizationAgreement =
                    supplyOrganizationAgreement;

                supplyOrderToReturn.SupplyOrderVehicleServices
                    .First(s => s.Id.Equals(supplyOrderVehicleService.Id)).VehicleService.VehicleOrganization = vehicleOrganization;
            }

            return supplyOrderVehicleService;
        };

        var vehicleServiceProps = new {
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            Ids = supplyOrderToReturn.SupplyOrderVehicleServices.Select(s => s.Id)
        };

        _connection.Query(
            sqlExpression,
            types,
            vehicleServiceOrganizationsMapper,
            vehicleServiceProps
        );

        _connection.Query<SupplyOrderVehicleService, VehicleService, InvoiceDocument, SupplyOrderVehicleService>(
            "SELECT " +
            "[SupplyOrderVehicleService].ID " +
            ",[VehicleService].ID " +
            ",[InvoiceDocument].* " +
            "FROM [SupplyOrderVehicleService] " +
            "LEFT JOIN [VehicleService] " +
            "ON [VehicleService].ID = [SupplyOrderVehicleService].VehicleServiceID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [InvoiceDocument].VehicleServiceID = [VehicleService].ID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "WHERE [SupplyOrderVehicleService].ID IN @Ids",
            (junction, service, document) => {
                if (document == null) return junction;

                VehicleService fromList = supplyOrderToReturn.SupplyOrderVehicleServices.First(s => s.Id.Equals(junction.Id)).VehicleService;

                if (!fromList.InvoiceDocuments.Any(d => d.Id.Equals(document.Id))) fromList.InvoiceDocuments.Add(document);

                return junction;
            },
            vehicleServiceProps
        );

        _connection.Query<PackingList, SupplyInvoice, SupplyOrder, Client, PackingList>(
            "SELECT * " +
            "FROM [PackingList] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON  [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
            "WHERE [PackingList].VehicleServiceID IN @Ids " +
            "AND [PackingList].Deleted = 0",
            (packingList, invoice, supplyOrder, client) => {
                supplyOrder.Client = client;

                invoice.SupplyOrder = supplyOrder;

                packingList.SupplyInvoice = invoice;

                supplyOrderToReturn.SupplyOrderVehicleServices
                    .First(s => packingList.VehicleServiceId != null && s.VehicleServiceId.Equals(packingList.VehicleServiceId.Value))
                    .VehicleService.PackingLists.Add(packingList);

                return packingList;
            },
            new { Ids = supplyOrderToReturn.SupplyOrderVehicleServices.Select(s => s.VehicleServiceId) }
        );


        return supplyOrderToReturn;
    }
}