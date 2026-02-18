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
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.PackingLists;

public sealed class PackingListRepository : IPackingListRepository {
    private readonly IDbConnection _connection;

    public PackingListRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<PackingList> GetAllUnshipped(SupplyTransportationType transportationType, string culture) {
        return _connection.Query<PackingList, ContainerService, VehicleService, SupplyInvoice, SupplyOrder, SupplyOrderNumber, Client, PackingList>(
                ";WITH [TotalPrices] AS( " +
                "SELECT " +
                " PackingList.ID " +
                ", SUM([PackingListPackageOrderItem].[UnitPriceEur]) AS [TotalNetPriceEur] " +
                ", SUM([PackingListPackageOrderItem].[GrossUnitPriceEur]) AS [TotalGrossPriceEur] " +
                ", SUM([PackingListPackageOrderItem].[UnitPrice]) AS [TotalNetPrice] " +
                "FROM PackingList " +
                "LEFT JOIN PackingListPackageOrderItem " +
                "ON PackingListPackageOrderItem.PackingListID = PackingList.ID " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                "WHERE PackingList.Deleted = 0 " +
                "AND PackingListPackageOrderItem.Deleted = 0 " +
                "AND [SupplyOrder].IsOrderShipped = 0 " +
                "AND [SupplyOrder].[TransportationType] = 0 " +
                "AND [Organization].[Culture] = @Culture " +
                "GROUP BY PackingList.ID " +
                ") " +
                "SELECT " +
                "[PackingList].* " +
                ", [TotalPrices].TotalGrossPriceEur " +
                ", [TotalPrices].TotalNetPrice " +
                ", [TotalPrices].TotalNetPriceEur " +
                ", [ContainerService].* " +
                ", [VehicleService].* " +
                ", [SupplyInvoice].* " +
                ", [SupplyOrder].* " +
                ", [SupplyOrderNumber].* " +
                ", [Client].* " +
                "FROM [PackingList] " +
                "LEFT JOIN [ContainerService] " +
                "ON [ContainerService].ID = [PackingList].ContainerServiceID " +
                "LEFT JOIN [VehicleService] " +
                "ON [VehicleService].[ID] = [PackingList].[VehicleServiceID] " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [SupplyOrderNumber] " +
                "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [SupplyOrder].ClientID " +
                "LEFT JOIN [TotalPrices] " +
                "ON [TotalPrices].ID = PackingList.ID " +
                "WHERE [TotalPrices].ID IS NOT NULL; ",
                (list, container, vehicle, invoice, order, orderNumber, client) => {
                    order.SupplyOrderNumber = orderNumber;

                    order.Client = client;

                    invoice.SupplyOrder = order;

                    list.SupplyInvoice = invoice;
                    list.ContainerService = container;

                    list.VehicleService = vehicle;

                    return list;
                },
                new { TransportationType = transportationType, Culture = culture }
            )
            .ToList();
    }

    public void UnassignAllByVehicleServiceIdExceptProvided(long vehicleServiceId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET VehicleServiceId = NULL " +
            "WHERE [PackingList].VehicleServiceID = @VehicleServiceId AND [PackingList].ID NOT IN @Ids",
            new { VehicleServiceId = vehicleServiceId, Ids = ids }
        );
    }

    public void AssignProvidedToVehicleService(long vehicleServiceId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET VehicleServiceId = @VehicleServiceId " +
            "WHERE [PackingList].ID IN @Ids",
            new { VehicleServiceId = vehicleServiceId, Ids = ids }
        );
    }

    public void UnassignAllByVehicleServiceId(long vehicleServiceId) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET VehicleServiceId = NULL " +
            "WHERE [PackingList].VehicleServiceID = @VehicleServiceId",
            new { VehicleServiceId = vehicleServiceId }
        );
    }

    public List<PackingList> GetAllAssignedToVehicleByVehicleNetId(Guid netId) {
        return _connection.Query<PackingList>(
                "SELECT " +
                "[PackingList].ID " +
                ",[PackingList].NetUID " +
                ",( " +
                "SELECT " +
                "CASE " +
                "WHEN COUNT(PackingListPackage.ID) > 0 " +
                "THEN ROUND(SUM([PackingListPackage].CBM), 2) " +
                "ELSE 0 " +
                "END " +
                "FROM [PackingListPackage] " +
                "WHERE [PackingListPackage].Deleted = 0 AND [PackingListPackage].PackingListID = [PackingList].ID " +
                ") AS [TotalCBM] " +
                ",ROUND( " +
                "SUM([PackingListPackageOrderItem].UnitPrice * [PackingListPackageOrderItem].Qty) " +
                ", 2) AS [TotalPrice] " +
                ",ROUND( " +
                "SUM([SupplyOrderItem].NetWeight) " +
                ", 2) AS [TotalNetWeight] " +
                ",ROUND( " +
                "SUM([SupplyOrderItem].GrossWeight) " +
                ", 2) AS [TotalGrossWeight] " +
                "FROM [VehicleService] " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].VehicleServiceID = [VehicleService].ID " +
                "AND [PackingList].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
                "AND [PackingListPackageOrderItem].Deleted = 0 " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].ID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyInvoiceOrderItem].SupplyOrderItemID = [SupplyOrderItem].ID " +
                "WHERE [VehicleService].ID = (SELECT [SupplyOrderVehicleService].VehicleServiceID FROM [SupplyOrderVehicleService] WHERE [SupplyOrderVehicleService].NetUID = @NetId) " +
                "GROUP BY [PackingList].ID, [PackingList].NetUID",
                new { NetId = netId }
            )
            .ToList();
    }

    public List<PackingList> GetAllAssignedToContainerByContainerNetId(Guid netId) {
        return _connection.Query<PackingList>(
                "SELECT " +
                "[PackingList].ID " +
                ",[PackingList].NetUID " +
                ",( " +
                "SELECT " +
                "CASE " +
                "WHEN COUNT(PackingListPackage.ID) > 0 " +
                "THEN ROUND(SUM([PackingListPackage].CBM), 2) " +
                "ELSE 0 " +
                "END " +
                "FROM [PackingListPackage] " +
                "WHERE [PackingListPackage].Deleted = 0 AND [PackingListPackage].PackingListID = [PackingList].ID " +
                ") AS [TotalCBM] " +
                ",ROUND( " +
                "SUM([PackingListPackageOrderItem].UnitPrice * [PackingListPackageOrderItem].Qty) " +
                ", 2) AS [TotalPrice] " +
                ",ROUND( " +
                "SUM([SupplyOrderItem].NetWeight) " +
                ", 2) AS [TotalNetWeight] " +
                ",ROUND( " +
                "SUM([SupplyOrderItem].GrossWeight) " +
                ", 2) AS [TotalGrossWeight] " +
                "FROM [ContainerService] " +
                "LEFT JOIN [PackingList] " +
                "ON [PackingList].ContainerServiceID = [ContainerService].ID " +
                "AND [PackingList].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
                "AND [PackingListPackageOrderItem].Deleted = 0 " +
                "LEFT JOIN [SupplyInvoiceOrderItem] " +
                "ON [PackingListPackageOrderItem].SupplyInvoiceOrderItemID = [SupplyInvoiceOrderItem].ID " +
                "LEFT JOIN [SupplyOrderItem] " +
                "ON [SupplyInvoiceOrderItem].SupplyOrderItemID = [SupplyOrderItem].ID " +
                "WHERE [ContainerService].ID = (SELECT [SupplyOrderContainerService].ContainerServiceID FROM [SupplyOrderContainerService] WHERE [SupplyOrderContainerService].NetUID = @NetId) " +
                "GROUP BY [PackingList].ID, [PackingList].NetUID",
                new { NetId = netId }
            )
            .ToList();
    }

    public PackingList GetByNetIdWithProductSpecification(
        Guid netId,
        decimal govExhangeRateUahToEur) {
        PackingList toReturn = null;

        Type[] types = {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductSpecification),
            typeof(User)
        };

        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        Func<object[], PackingList> mapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductSpecification productSpecification = (ProductSpecification)objects[6];
            User user = (User)objects[7];

            if (toReturn == null)
                toReturn = packingList;

            if (packingListPackageOrderItem == null) return packingList;

            if (!toReturn.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id)))
                toReturn.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
            else
                packingListPackageOrderItem = toReturn.PackingListPackageOrderItems.First(i => i.Id.Equals(packingListPackageOrderItem.Id));

            product.MeasureUnit = measureUnit;

            product.Name =
                isPlCulture
                    ? product.NameUA
                    : product.NameUA;
            if (supplyOrderItem != null)
                supplyOrderItem.Product = product;
            supplyInvoiceOrderItem.Product = product;
            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

            packingListPackageOrderItem.TotalNetWeight = packingListPackageOrderItem.Qty * packingListPackageOrderItem.NetWeight;
            packingListPackageOrderItem.TotalGrossWeight = packingListPackageOrderItem.Qty * packingListPackageOrderItem.GrossWeight;
            packingListPackageOrderItem.UnitPrice = packingListPackageOrderItem.UnitPrice;
            packingListPackageOrderItem.UnitPriceEur = packingListPackageOrderItem.UnitPriceEur;
            packingListPackageOrderItem.TotalNetPrice = Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.UnitPrice;
            packingListPackageOrderItem.TotalNetPriceEur = Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.UnitPriceEur;

            toReturn.TotalQuantity += packingListPackageOrderItem.Qty;

            if (productSpecification != null) {
                productSpecification.AddedBy = user;
                product.ProductSpecifications.Add(productSpecification);

                toReturn.TotalCustomValue += productSpecification.CustomsValue;
                toReturn.TotalVatAmount += productSpecification.VATValue;
                toReturn.TotalDuty += productSpecification.Duty;
            }

            packingListPackageOrderItem.AccountingTotalGrossPriceEur =
                Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.AccountingGrossUnitPriceEur;

            packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur =
                Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.AccountingGeneralGrossUnitPriceEur;

            packingListPackageOrderItem.TotalGrossPriceEur = Convert.ToDecimal(packingListPackageOrderItem.Qty) * packingListPackageOrderItem.GrossUnitPriceEur;

            packingListPackageOrderItem.TotalGrossPriceEur =
                packingListPackageOrderItem.TotalGrossPriceEur +
                packingListPackageOrderItem.AccountingTotalGrossPriceEur +
                packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur;

            if (govExhangeRateUahToEur.Equals(0)) {
                packingListPackageOrderItem.AccountingTotalGrossPrice =
                    packingListPackageOrderItem.AccountingTotalGrossPriceEur *
                    govExhangeRateUahToEur +
                    0 * govExhangeRateUahToEur;

                packingListPackageOrderItem.AccountingGeneralTotalGrossPrice =
                    packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur * govExhangeRateUahToEur;

                packingListPackageOrderItem.TotalGrossPrice =
                    packingListPackageOrderItem.TotalGrossPriceEur *
                    govExhangeRateUahToEur;
            } else if (govExhangeRateUahToEur.Equals(1m)) {
                packingListPackageOrderItem.AccountingTotalGrossPrice =
                    packingListPackageOrderItem.AccountingTotalGrossPriceEur;

                packingListPackageOrderItem.AccountingGeneralTotalGrossPrice =
                    packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur;

                packingListPackageOrderItem.TotalGrossPrice = packingListPackageOrderItem.TotalGrossPriceEur;
            } else {
                packingListPackageOrderItem.AccountingTotalGrossPrice =
                    (govExhangeRateUahToEur > 0
                        ? packingListPackageOrderItem.AccountingTotalGrossPriceEur * govExhangeRateUahToEur
                        : packingListPackageOrderItem.AccountingTotalGrossPriceEur / govExhangeRateUahToEur) +
                    0
                    * govExhangeRateUahToEur;

                packingListPackageOrderItem.AccountingGeneralTotalGrossPrice =
                    govExhangeRateUahToEur > 0
                        ? packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur * govExhangeRateUahToEur
                        : packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur / govExhangeRateUahToEur;

                packingListPackageOrderItem.TotalGrossPrice =
                    govExhangeRateUahToEur > 0
                        ? packingListPackageOrderItem.TotalGrossPriceEur * govExhangeRateUahToEur
                        : packingListPackageOrderItem.TotalGrossPriceEur / govExhangeRateUahToEur;
            }

            toReturn.TotalNetPrice += packingListPackageOrderItem.TotalNetPrice;
            toReturn.TotalGrossPrice += packingListPackageOrderItem.TotalGrossPrice;
            toReturn.AccountingTotalGrossPrice += packingListPackageOrderItem.AccountingTotalGrossPrice;
            toReturn.AccountingTotalGrossPriceEur += packingListPackageOrderItem.AccountingTotalGrossPriceEur;
            toReturn.TotalGrossPriceEur += packingListPackageOrderItem.TotalGrossPriceEur;

            toReturn.TotalNetWeight += packingListPackageOrderItem.TotalNetWeight;
            toReturn.TotalGrossWeight += packingListPackageOrderItem.TotalGrossWeight;

            toReturn.TotalNetWeight = Math.Round(toReturn.TotalNetWeight, 3);

            toReturn.TotalGrossWeight = Math.Round(toReturn.TotalGrossWeight, 3);

            return packingList;
        };

        _connection.Query(
            ";WITH [Specification_CTE] AS ( " +
            "SELECT " +
            "[Product].[ID] AS [ProductID] " +
            ", MAX([ProductSpecification].[ID]) AS [SpecificationID] " +
            ", [ProductSpecification].[VATValue] " +
            ", [OrderProductSpecification].Qty " +
            ", [OrderProductSpecification].UnitPrice " +
            "FROM [OrderProductSpecification] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [OrderProductSpecification].[SupplyInvoiceId] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [OrderProductSpecification].[ProductSpecificationId] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ProductSpecification].[ProductID] " +
            "WHERE [PackingList].[NetUID] = @NetId " +
            "GROUP BY [Product].[ID], [ProductSpecification].[VATValue], [OrderProductSpecification].Qty, [OrderProductSpecification].UnitPrice " +
            ") " +
            "SELECT " +
            "[PackingList].* " +
            ", [PackingListPackageOrderItem].* " +
            ", [dbo].GetGovExchangedToEuroValue( " +
            "[PackingListPackageOrderItem].[DeliveryPerItem] * [PackingListPackageOrderItem].[Qty], " +
            "[Agreement].[CurrencyID], " +
            "CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END) AS DeliveryAmountEur " +
            ", [dbo].GetGovExchangedToUahValue( " +
            "[PackingListPackageOrderItem].[DeliveryPerItem] * [PackingListPackageOrderItem].[Qty], " +
            "[Agreement].[CurrencyID], " +
            "CASE " +
            "WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL " +
            "THEN [SupplyInvoice].[DateCustomDeclaration] " +
            "ELSE [SupplyInvoice].[Created] " +
            "END) AS DeliveryAmountUah " +
            ", [SupplyInvoiceOrderItem].* " +
            ", [SupplyOrderItem].* " +
            ", [Product].* " +
            ", [MeasureUnit].* " +
            ", [ProductSpecification].* " +
            ", [User].* " +
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
            "LEFT JOIN [Specification_CTE] " +
            "ON [Specification_CTE].[ProductId] = [Product].[ID] " +
            "AND [Specification_CTE].Qty = [PackingListPackageOrderItem].Qty " +
            "AND [Specification_CTE].UnitPrice = [PackingListPackageOrderItem].UnitPrice " +
            //"AND ROUND([Specification_CTE].[VATValue], 2) = ROUND([PackingListPackageOrderItem].[VatAmount], 2) " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [Specification_CTE].[SpecificationID] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductSpecification].AddedByID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [PackingList].NetUID = @NetId " +
            "AND [PackingList].Deleted = 0 " +
            "ORDER BY [SupplyInvoiceOrderItem].[RowNumber] ",
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower() }
        );

        Type[] typeWithSupplyService = {
            typeof(PackingListPackageOrderItem),
            typeof(PackingListPackageOrderItemSupplyService),
            typeof(MergedService),
            typeof(BillOfLadingService),
            typeof(ContainerService),
            typeof(VehicleService),
            typeof(SupplyOrganization),
            typeof(SupplyOrganization),
            typeof(SupplyOrganization),
            typeof(SupplyOrganization),
            typeof(ConsumableProduct)
        };

        Func<object[], PackingListPackageOrderItem> mapperWithSupplyService = objects => {
            PackingListPackageOrderItem packingListItem = (PackingListPackageOrderItem)objects[0];
            PackingListPackageOrderItemSupplyService packingListItemSupplyService = (PackingListPackageOrderItemSupplyService)objects[1];
            MergedService mergedService = (MergedService)objects[2];
            BillOfLadingService billOfLadingService = (BillOfLadingService)objects[3];
            ContainerService containerService = (ContainerService)objects[4];
            VehicleService vehicleService = (VehicleService)objects[5];
            SupplyOrganization supplyOrganizationMergedService = (SupplyOrganization)objects[6];
            SupplyOrganization supplyOrganizationBillOfLadingService = (SupplyOrganization)objects[7];
            SupplyOrganization supplyOrganizationContainerService = (SupplyOrganization)objects[8];
            SupplyOrganization supplyOrganizationVehicleService = (SupplyOrganization)objects[9];
            ConsumableProduct consumableProduct = (ConsumableProduct)objects[10];

            if (packingListItemSupplyService == null) return packingListItem;

            PackingListPackageOrderItem existPackingListItem =
                toReturn.PackingListPackageOrderItems.FirstOrDefault(x => x.Id.Equals(packingListItem.Id));

            if (existPackingListItem == null) return packingListItem;

            if (!existPackingListItem.PackingListPackageOrderItemSupplyServices
                    .Any(x => x.Id.Equals(packingListItemSupplyService.Id)))
                existPackingListItem.PackingListPackageOrderItemSupplyServices.Add(packingListItemSupplyService);
            else
                packingListItemSupplyService = existPackingListItem.PackingListPackageOrderItemSupplyServices
                    .First(x => x.Id.Equals(packingListItemSupplyService.Id));

            if (packingListItemSupplyService.MergedServiceId.HasValue) {
                mergedService.ConsumableProduct = consumableProduct;
                mergedService.SupplyOrganization = supplyOrganizationMergedService;
                packingListItemSupplyService.MergedService = mergedService;

                PackingListPackageOrderItemSupplyService firstEl = toReturn.PackingListPackageOrderItems.First(x => x.Id == packingListItem.Id)
                    .PackingListPackageOrderItemSupplyServices
                    .First(x => x.MergedServiceId.Equals(mergedService.Id));

                firstEl.TotalGeneralPriceForServiceEur += packingListItemSupplyService.GeneralValueEur;
                firstEl.TotalGeneralPriceForServiceUah += packingListItemSupplyService.GeneralValueUah;
                firstEl.TotalNetPriceForServiceEur += packingListItemSupplyService.NetValueEur;
                firstEl.TotalNetPriceForServiceUah += packingListItemSupplyService.NetValueUah;
                firstEl.TotalManagementPriceForServiceEur += packingListItemSupplyService.ManagementValueEur;
                firstEl.TotalManagementPriceForServiceUah += packingListItemSupplyService.ManagementValueUah;
            } else if (packingListItemSupplyService.BillOfLadingServiceId.HasValue) {
                billOfLadingService.SupplyOrganization = supplyOrganizationBillOfLadingService;
                packingListItemSupplyService.BillOfLadingService = billOfLadingService;

                PackingListPackageOrderItemSupplyService firstEl = toReturn.PackingListPackageOrderItems.First().PackingListPackageOrderItemSupplyServices
                    .First(x => x.BillOfLadingServiceId.Equals(billOfLadingService.Id));

                firstEl.TotalGeneralPriceForServiceEur += packingListItemSupplyService.GeneralValueEur;
                firstEl.TotalGeneralPriceForServiceUah += packingListItemSupplyService.GeneralValueUah;
                firstEl.TotalNetPriceForServiceEur += packingListItemSupplyService.NetValueEur;
                firstEl.TotalNetPriceForServiceUah += packingListItemSupplyService.NetValueUah;
                firstEl.TotalManagementPriceForServiceEur += packingListItemSupplyService.ManagementValueEur;
                firstEl.TotalManagementPriceForServiceUah += packingListItemSupplyService.ManagementValueUah;
            } else if (packingListItemSupplyService.ContainerServiceId.HasValue) {
                containerService.ContainerOrganization = supplyOrganizationContainerService;
                packingListItemSupplyService.ContainerService = containerService;

                PackingListPackageOrderItemSupplyService firstEl = toReturn.PackingListPackageOrderItems.First().PackingListPackageOrderItemSupplyServices
                    .First(x => x.ContainerServiceId.Equals(containerService.Id));

                firstEl.TotalGeneralPriceForServiceEur += packingListItemSupplyService.GeneralValueEur;
                firstEl.TotalGeneralPriceForServiceUah += packingListItemSupplyService.GeneralValueUah;
                firstEl.TotalNetPriceForServiceEur += packingListItemSupplyService.NetValueEur;
                firstEl.TotalNetPriceForServiceUah += packingListItemSupplyService.NetValueUah;
                firstEl.TotalManagementPriceForServiceEur += packingListItemSupplyService.ManagementValueEur;
                firstEl.TotalManagementPriceForServiceUah += packingListItemSupplyService.ManagementValueUah;
            } else if (packingListItemSupplyService.VehicleServiceId.HasValue) {
                vehicleService.VehicleOrganization = supplyOrganizationVehicleService;
                packingListItemSupplyService.VehicleService = vehicleService;

                PackingListPackageOrderItemSupplyService firstEl = toReturn.PackingListPackageOrderItems.First().PackingListPackageOrderItemSupplyServices
                    .First(x => x.VehicleServiceId.Equals(vehicleService.Id));

                firstEl.TotalGeneralPriceForServiceEur += packingListItemSupplyService.GeneralValueEur;
                firstEl.TotalGeneralPriceForServiceUah += packingListItemSupplyService.GeneralValueUah;
                firstEl.TotalNetPriceForServiceEur += packingListItemSupplyService.NetValueEur;
                firstEl.TotalNetPriceForServiceUah += packingListItemSupplyService.NetValueUah;
                firstEl.TotalManagementPriceForServiceEur += packingListItemSupplyService.ManagementValueEur;
                firstEl.TotalManagementPriceForServiceUah += packingListItemSupplyService.ManagementValueUah;
            }

            return packingListItem;
        };

        _connection.Query(
            "SELECT " +
            "[PackingListPackageOrderItem].* " +
            ", [PackingListPackageOrderItemSupplyService].* " +
            ", [dbo].GetGovExchangedToEuroValue( " +
            "[PackingListPackageOrderItemSupplyService].[NetValue], " +
            "[Currency].[ID], " +
            "[PackingListPackageOrderItemSupplyService].[ExchangeRateDate] " +
            ") AS [NetValueEur] " +
            ", [dbo].GetGovExchangedToUahValue( " +
            "[PackingListPackageOrderItemSupplyService].[NetValue], " +
            "[Currency].[ID], " +
            "[PackingListPackageOrderItemSupplyService].[ExchangeRateDate] " +
            ") AS [NetValueUah] " +
            ", [dbo].GetGovExchangedToEuroValue( " +
            "[PackingListPackageOrderItemSupplyService].[GeneralValue], " +
            "[Currency].[ID], " +
            "[PackingListPackageOrderItemSupplyService].[ExchangeRateDate] " +
            ") AS [GeneralValueEur] " +
            ", [dbo].GetGovExchangedToUahValue( " +
            "[PackingListPackageOrderItemSupplyService].[GeneralValue], " +
            "[Currency].[ID], " +
            "[PackingListPackageOrderItemSupplyService].[ExchangeRateDate] " +
            ") AS [GeneralValueUah] " +
            ", [dbo].GetGovExchangedToEuroValue( " +
            "[PackingListPackageOrderItemSupplyService].[ManagementValue], " +
            "[Currency].[ID], " +
            "[PackingListPackageOrderItemSupplyService].[ExchangeRateDate] " +
            ") AS [ManagementValueEur] " +
            ", [dbo].GetGovExchangedToUahValue( " +
            "[PackingListPackageOrderItemSupplyService].[ManagementValue], " +
            "[Currency].[ID], " +
            "[PackingListPackageOrderItemSupplyService].[ExchangeRateDate] " +
            ") AS [ManagementValueUah] " +
            ", [MergedService].* " +
            ", [BillOfLadingService].* " +
            ", [ContainerService].* " +
            ", [VehicleService].* " +
            ", [SupplyOrganizationMergedService].* " +
            ", [SupplyOrganizationBillOfLadingService].* " +
            ", [SupplyOrganizationContainerService].* " +
            ", [SupplyOrganizationVehicleService].* " +
            ", [ConsumableProduct].* " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [PackingListPackageOrderItemSupplyService] " +
            "ON [PackingListPackageOrderItemSupplyService].[PackingListPackageOrderItemID] = [PackingListPackageOrderItem].[ID] " +
            "AND [PackingListPackageOrderItemSupplyService].[Deleted] = 0 " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [PackingListPackageOrderItemSupplyService].[CurrencyID] " +
            "LEFT JOIN [MergedService] " +
            "ON [MergedService].[ID] = [PackingListPackageOrderItemSupplyService].[MergedServiceID] " +
            "LEFT JOIN [BillOfLadingService] " +
            "ON [BillOfLadingService].[ID] = [PackingListPackageOrderItemSupplyService].[BillOfLadingServiceID] " +
            "LEFT JOIN [ContainerService] " +
            "ON [ContainerService].[ID] = [PackingListPackageOrderItemSupplyService].[ContainerServiceID] " +
            "LEFT JOIN [VehicleService] " +
            "ON [VehicleService].[ID] = [PackingListPackageOrderItemSupplyService].[VehicleServiceID] " +
            "LEFT JOIN [SupplyOrganization] AS [SupplyOrganizationMergedService] " +
            "ON [SupplyOrganizationMergedService].[ID] = [MergedService].[SupplyOrganizationID] " +
            "LEFT JOIN [SupplyOrganization] AS [SupplyOrganizationBillOfLadingService] " +
            "ON [SupplyOrganizationBillOfLadingService].[ID] = [BillOfLadingService].[SupplyOrganizationID] " +
            "LEFT JOIN [SupplyOrganization] AS [SupplyOrganizationContainerService] " +
            "ON [SupplyOrganizationContainerService].[ID] = [ContainerService].[ContainerOrganizationID] " +
            "LEFT JOIN [SupplyOrganization] AS [SupplyOrganizationVehicleService] " +
            "ON [SupplyOrganizationVehicleService].[ID] = [VehicleService].[VehicleOrganizationID] " +
            "LEFT JOIN [ConsumableProduct] " +
            "ON [ConsumableProduct].[ID] = [MergedService].[ConsumableProductID] " +
            "WHERE [PackingListPackageOrderItem].[PackingListID] = @Id " +
            "AND [PackingListPackageOrderItem].[Deleted] = 0; ",
            typeWithSupplyService, mapperWithSupplyService, new { toReturn.Id });

        return toReturn;
    }

    public long Add(PackingList packingList) {
        return _connection.Query<long>(
                "INSERT INTO [PackingList] " +
                "(MarkNumber, InvNo, PlNo, RefNo, No, FromDate, SupplyInvoiceId, IsDocumentsAdded, ExtraCharge, ContainerServiceId, Comment, IsPlaced, " +
                "IsVatOneApplied, IsVatTwoApplied, VatOnePercent, VatTwoPercent, Updated, AccountingExtraCharge) " +
                "VALUES " +
                "(@MarkNumber, @InvNo, @PlNo, @RefNo, @No, @FromDate, @SupplyInvoiceId, @IsDocumentsAdded, @ExtraCharge, @ContainerServiceId, @Comment, 0, 0, 0, 0, 0, getutcdate(), " +
                "@AccountingExtraCharge); " +
                "SELECT SCOPE_IDENTITY()",
                packingList
            )
            .Single();
    }

    public List<PackingListForSpecification> GetByNetIdForSpecification(Guid netId) {
        List<PackingListForSpecification> toReturn = new();

        Type[] types = {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(SupplyInvoice),
            typeof(ProductSpecification),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(ProductSpecification),
            typeof(Organization),
            typeof(ClientBankDetails),
            typeof(ClientBankDetailAccountNumber),
            typeof(ClientBankDetailIbanNo)
        };

        Func<object[], PackingList> mapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[6];
            ProductSpecification productSpecification = (ProductSpecification)objects[7];
            Client client = (Client)objects[8];
            ClientAgreement clientAgreement = (ClientAgreement)objects[9];
            Agreement agreement = (Agreement)objects[10];
            Currency currency = (Currency)objects[11];
            ProductSpecification plProductSpecification = (ProductSpecification)objects[12];
            Organization organization = (Organization)objects[13];
            ClientBankDetails bankDetails = (ClientBankDetails)objects[14];
            ClientBankDetailAccountNumber accountNumber = (ClientBankDetailAccountNumber)objects[15];
            ClientBankDetailIbanNo ibanNo = (ClientBankDetailIbanNo)objects[16];

            if (packingListPackageOrderItem == null || productSpecification == null) return packingList;

            if (toReturn.Any(s => s.ProductSpecificationCode.ToLower().Equals(productSpecification.SpecificationCode.ToLower()))) {
                PackingListForSpecification fromList = toReturn.First(s => s.ProductSpecificationCode.ToLower().Equals(productSpecification.SpecificationCode.ToLower()));

                if (fromList.OrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) return packingList;

                product.MeasureUnit = measureUnit;
                product.ProductSpecifications.Add(productSpecification);

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;
                supplyInvoiceOrderItem.SupplyInvoice = supplyInvoice;

                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;
                packingListPackageOrderItem.ProductSpecification = plProductSpecification;

                fromList.OrderItems.Add(packingListPackageOrderItem);
            } else {
                product.MeasureUnit = measureUnit;
                product.ProductSpecifications.Add(productSpecification);

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.Product = product;
                supplyInvoiceOrderItem.SupplyInvoice = supplyInvoice;

                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                agreement.Currency = currency;

                clientAgreement.Agreement = agreement;

                if (bankDetails != null) {
                    bankDetails.AccountNumber = accountNumber;
                    bankDetails.ClientBankDetailIbanNo = ibanNo;
                }

                client.ClientBankDetails = bankDetails;

                client.ClientAgreements.Add(clientAgreement);

                PackingListForSpecification toAdd = new() {
                    ProductSpecificationCode = productSpecification.SpecificationCode,
                    SupplyInvoice = supplyInvoice,
                    Client = client,
                    Organization = organization
                };

                toAdd.OrderItems.Add(packingListPackageOrderItem);

                toReturn.Add(toAdd);
            }

            return packingList;
        };

        string sqlExpression =
            "SELECT " +
            "[PackingList].* " +
            ",[PackingListPackageOrderItem].* " +
            ",[SupplyInvoiceOrderItem].* " +
            ",[SupplyOrderItem].Created " +
            ",[SupplyOrderItem].Deleted " +
            ",[SupplyOrderItem].Description " +
            ",[SupplyOrderItem].GrossWeight " +
            ",[SupplyOrderItem].ID " +
            ",[SupplyOrderItem].IsPacked " +
            ",[SupplyOrderItem].ItemNo " +
            ",[SupplyOrderItem].NetUID " +
            ",[SupplyOrderItem].NetWeight " +
            ",[SupplyOrderItem].ProductID " +
            ",[SupplyOrderItem].Qty " +
            ",[SupplyOrderItem].StockNo " +
            ",[SupplyOrderItem].SupplyOrderID " +
            ",[SupplyOrderItem].TotalAmount " +
            ",ROUND([SupplyOrderItem].UnitPrice / dbo.GetCrossExchangeRateToBaseCurrencyByCurrencyId([Currency].ID), 2) AS [UnitPrice] " +
            ",[SupplyOrderItem].Updated " +
            ",[Product].* " +
            ",[MeasureUnit].* " +
            ",[SupplyInvoice].* " +
            ",[ProductSpecification].* " +
            ",[Client].* " +
            ",[ClientAgreement].* " +
            ",[Agreement].* " +
            ",[Currency].* " +
            ",[PlSpecification].* " +
            ",[Organization].* " +
            ",[ClientBankDetails].* " +
            ",[ClientBankDetailAccountNumber].* " +
            ",[ClientBankDetailIbanNo].* " +
            "FROM [PackingList] " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
            "AND [PackingListPackageOrderItem].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN ( " +
            "SELECT [Product].[ID] " +
            ",[Product].[Created] " +
            ",[Product].[Deleted] " +
            ",[Product].[HasAnalogue] " +
            ",[Product].[HasImage] " +
            ",[Product].[IsForSale] " +
            ",[Product].[IsForWeb] " +
            ",[Product].[IsForZeroSale] " +
            ",[Product].[MainOriginalNumber] " +
            ",[Product].[Name] " +
            ",[Product].[NameUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[Description] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[MeasureUnitID] " +
            ",[Product].[NetUID] " +
            ",[Product].[OrderStandard] " +
            ",[Product].[PackingStandard] " +
            ",[Product].[Size] " +
            ",[Product].[UCGFEA] " +
            ",[Product].[Updated] " +
            ",[Product].[VendorCode] " +
            ",[Product].[Volume] " +
            ",[Product].[Weight] " +
            ",[Product].[HasComponent] " +
            ",[Product].[Image] " +
            ",[Product].[Top] " +
            "FROM [Product] " +
            ") AS [Product] " +
            "ON [Product].ID = [SupplyInvoiceOrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ID = (" +
            "SELECT TOP(1) [ProductSpecification].ID " +
            "FROM [ProductSpecification] " +
            "LEFT JOIN [OrderProductSpecification] " +
            "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
            "AND [OrderProductSpecification].SupplyInvoiceID = [PackingList].SupplyInvoiceID " +
            "AND [OrderProductSpecification].Deleted = 0 " +
            "WHERE [ProductSpecification].Deleted = 0 " +
            "AND [ProductSpecification].ProductID = [Product].ID " +
            "AND [OrderProductSpecification].ID IS NOT NULL " +
            "ORDER BY [OrderProductSpecification].ID DESC" +
            ") " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [ClientBankDetails] " +
            "ON [ClientBankDetails].ID = [Client].ClientBankDetailsID " +
            "LEFT JOIN [ClientBankDetailAccountNumber] " +
            "ON [ClientBankDetailAccountNumber].ID = [ClientBankDetails].AccountNumberID " +
            "LEFT JOIN [ClientBankDetailIbanNo] " +
            "ON [ClientBankDetailIbanNo].ID = [ClientBankDetails].ClientBankDetailIbanNoID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [ProductSpecification] AS [PlSpecification] " +
            "ON [PlSpecification].ID = (" +
            "SELECT [JoinSpecification].ID " +
            "FROM [ProductSpecification] AS [JoinSpecification] " +
            "WHERE [JoinSpecification].ProductID = [Product].ID " +
            "AND [JoinSpecification].Locale = N'pl' " +
            "AND [JoinSpecification].Deleted = 0 " +
            "AND [JoinSpecification].IsActive = 1" +
            ") " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyOrder].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [PackingList].NetUID = @NetId " +
            "ORDER BY [ProductSpecification].SpecificationCode DESC, [ProductSpecification].Name ";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { NetId = netId, Culture = "pl" }
        );

        if (!toReturn.Any()) return toReturn;

        PackingListForSpecification firstSpecification = toReturn.First();

        firstSpecification.Organization.PaymentRegisters =
            _connection.Query<PaymentRegister>(
                "SELECT [PaymentRegister].* " +
                "FROM [PaymentRegister] " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
                "AND [PaymentCurrencyRegister].Deleted = 0 " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
                "WHERE [PaymentRegister].Deleted = 0 " +
                "AND [PaymentRegister].IsActive = 1 " +
                "AND [PaymentRegister].OrganizationID = @Id " +
                "AND [Currency].ID = @CurrencyId",
                new { firstSpecification.Organization.Id, CurrencyId = firstSpecification.Client.ClientAgreements.FirstOrDefault()?.Agreement.CurrencyId ?? 0 }
            ).ToList();

        return toReturn;
    }

    public List<GroupedSpecificationByPackingList> GetGroupedSpecificationForDocumentByPackingListNetId(Guid netId) {
        return _connection.Query<GroupedSpecificationByPackingList>(
                "SELECT [ProductSpecification].Name AS [SpecificationName] " +
                ",ROUND(SUM([PackingListPackageOrderItem].Qty), 2) AS [TotalQty] " +
                ",[MeasureUnit].[NameUk] AS [MeasureUnitNameUk] " +
                ",[MeasureUnit].[NamePl] AS [MeasureUnitNamePl] " +
                ",[ProductSpecification].SpecificationCode AS [SpecificationCode] " +
                "FROM [PackingList] " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].PackingListID = [PackingList].ID " +
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
                "LEFT JOIN [ProductSpecification] " +
                "ON [ProductSpecification].ID = (" +
                "SELECT TOP(1) [ProductSpecification].ID " +
                "FROM [ProductSpecification] " +
                "LEFT JOIN [OrderProductSpecification] " +
                "ON [OrderProductSpecification].ProductSpecificationID = [ProductSpecification].ID " +
                "AND [OrderProductSpecification].SupplyInvoiceID = [PackingList].SupplyInvoiceID " +
                "AND [OrderProductSpecification].Deleted = 0 " +
                "WHERE [ProductSpecification].Deleted = 0 " +
                "AND [ProductSpecification].ProductID = [Product].ID " +
                "AND [OrderProductSpecification].ID IS NOT NULL " +
                "ORDER BY [OrderProductSpecification].ID DESC" +
                ") " +
                "WHERE [PackingList].NetUID = @NetId " +
                "GROUP BY [ProductSpecification].Name, [ProductSpecification].SpecificationCode, [MeasureUnit].[NameUk], [MeasureUnit].[NamePl] " +
                "ORDER BY [ProductSpecification].SpecificationCode ",
                new { NetId = netId, Culture = "pl" }
            )
            .ToList();
    }

    public void SetIsDocumentsAddedFalse() {
        _connection.Execute(
            "UPDATE [PackingList] SET IsDocumentsAdded = 0 WHERE [PackingList].Updated > (getutcdate() - 1)"
        );
    }

    public PackingList GetById(long id) {
        PackingList packingListToReturn = null;

        Type[] types = {
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
            typeof(MeasureUnit)
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

            if (packingList != null) {
                if (packingListToReturn == null) {
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

                    packingListToReturn = packingList;
                } else {
                    if (packingListPackageOrderItem != null)
                        if (!packingListToReturn.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                            product.MeasureUnit = measureUnit;

                            if (supplyOrderItem != null)
                                supplyOrderItem.Product = product;

                            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                            supplyInvoiceOrderItem.Product = product;

                            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                            packingListToReturn.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                        }

                    if (package != null) {
                        if (package.Type.Equals(PackingListPackageType.Box)) {
                            if (packingListToReturn.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                                PackingListPackage packageFromList = packingListToReturn.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                                if (packageOrderItem != null)
                                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                                        if (packageSupplyOrderItem != null)
                                            packageSupplyOrderItem.Product = packageProduct;

                                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                        packageSupplyInvoiceOrderItem.Product = product;

                                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                    }
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

                                packingListToReturn.PackingListBoxes.Add(package);
                            }
                        } else {
                            if (packingListToReturn.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                                PackingListPackage packageFromList = packingListToReturn.PackingListPallets.First(p => p.Id.Equals(package.Id));

                                if (packageOrderItem != null)
                                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                                        if (packageSupplyOrderItem != null)
                                            packageSupplyOrderItem.Product = packageProduct;

                                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                        packageSupplyInvoiceOrderItem.Product = product;

                                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                    }
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

                                packingListToReturn.PackingListPallets.Add(package);
                            }
                        }
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
            "WHERE [PackingList].ID = @Id",
            types,
            packingListMapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (packingListToReturn != null) {
            _connection.Query<SupplyInvoice, SupplyOrder, DeliveryProductProtocol, Organization, SupplyInvoice>(
                "SELECT * " +
                "FROM [SupplyInvoice] " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [DeliveryProductProtocol] " +
                "ON [DeliveryProductProtocol].ID = [SupplyInvoice].DeliveryProductProtocolID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "WHERE [SupplyInvoice].ID = @SupplyInvoiceId",
                (invoice, order, protocol, organization) => {
                    order.Organization = organization;

                    invoice.SupplyOrder = order;

                    invoice.DeliveryProductProtocol = protocol;

                    packingListToReturn.SupplyInvoice = invoice;

                    return invoice;
                },
                new { packingListToReturn.SupplyInvoiceId }
            );

            _connection.Query<DynamicProductPlacementColumn, DynamicProductPlacementRow, PackingListPackageOrderItem, DynamicProductPlacement, DynamicProductPlacementColumn>(
                "SELECT * " +
                "FROM [DynamicProductPlacementColumn] " +
                "LEFT JOIN [DynamicProductPlacementRow] " +
                "ON [DynamicProductPlacementRow].DynamicProductPlacementColumnID = [DynamicProductPlacementColumn].ID " +
                "AND [DynamicProductPlacementRow].Deleted = 0 " +
                "LEFT JOIN [PackingListPackageOrderItem] " +
                "ON [PackingListPackageOrderItem].ID = [DynamicProductPlacementRow].PackingListPackageOrderItemID " +
                "LEFT JOIN [DynamicProductPlacement] " +
                "ON [DynamicProductPlacement].DynamicProductPlacementRowID = [DynamicProductPlacementRow].ID " +
                "AND [DynamicProductPlacement].Deleted = 0 " +
                "WHERE [DynamicProductPlacementColumn].Deleted = 0 " +
                "AND [DynamicProductPlacementColumn].PackingListID = @Id",
                (column, row, item, placement) => {
                    if (!packingListToReturn.DynamicProductPlacementColumns.Any(c => c.Id.Equals(column.Id))) {
                        if (row != null) {
                            if (placement != null) row.DynamicProductPlacements.Add(placement);

                            row.PackingListPackageOrderItem = item;

                            column.DynamicProductPlacementRows.Add(row);
                        }

                        packingListToReturn.DynamicProductPlacementColumns.Add(column);
                    } else {
                        DynamicProductPlacementColumn columnFromList = packingListToReturn.DynamicProductPlacementColumns.First(c => c.Id.Equals(column.Id));

                        if (row != null) {
                            if (!columnFromList.DynamicProductPlacementRows.Any(r => r.Id.Equals(row.Id))) {
                                if (placement != null) row.DynamicProductPlacements.Add(placement);

                                row.PackingListPackageOrderItem = item;

                                columnFromList.DynamicProductPlacementRows.Add(row);
                            } else {
                                if (placement != null) columnFromList.DynamicProductPlacementRows.First(r => r.Id.Equals(row.Id)).DynamicProductPlacements.Add(placement);
                            }
                        }
                    }

                    return column;
                },
                new { packingListToReturn.Id }
            );
        }

        return packingListToReturn;
    }

    public PackingList GetByIdForPlacement(long id) {
        PackingList toReturn =
            _connection.Query<PackingList>(
                    "SELECT * " +
                    "FROM [PackingList] " +
                    "WHERE [PackingList].ID = @Id",
                    new { Id = id }
                )
                .SingleOrDefault();

        if (toReturn != null)
            toReturn.PackingListPackageOrderItems =
                _connection.Query<PackingListPackageOrderItem, ProductIncomeItem, ProductIncome, PackingListPackageOrderItem>(
                    "SELECT * " +
                    "FROM [PackingListPackageOrderItem] " +
                    "LEFT JOIN [ProductIncomeItem] " +
                    "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
                    "LEFT JOIN [ProductIncome] " +
                    "ON [ProductIncome].ID = [ProductIncomeItem].ProductIncomeID " +
                    "WHERE [PackingListPackageOrderItem].PackingListID = @Id",
                    (item, incomeItem, productIncome) => {
                        if (incomeItem != null) incomeItem.ProductIncome = productIncome;

                        item.ProductIncomeItem = incomeItem;

                        return item;
                    },
                    new { toReturn.Id }
                ).ToList();

        return toReturn;
    }

    public PackingList GetByNetIdForPlacement(Guid netId) {
        PackingList packingListToReturn = null;

        Type[] types = {
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
            typeof(ProductPlacement),
            typeof(ProductPlacement)
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
            ProductPlacement itemProductPlacement = (ProductPlacement)objects[12];
            ProductPlacement packageItemProductPlacement = (ProductPlacement)objects[13];

            if (packingList != null) {
                if (packingListToReturn == null) {
                    if (packingListPackageOrderItem != null) {
                        product.MeasureUnit = measureUnit;

                        if (supplyOrderItem != null)
                            supplyOrderItem.Product = product;

                        supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                        supplyInvoiceOrderItem.Product = product;

                        packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                        packingListPackageOrderItem.UnitPrice = decimal.Round(packingListPackageOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);
                        packingListPackageOrderItem.UnitPriceEur = decimal.Round(packingListPackageOrderItem.UnitPriceEur, 2, MidpointRounding.AwayFromZero);
                        packingListPackageOrderItem.GrossUnitPriceEur = decimal.Round(packingListPackageOrderItem.GrossUnitPriceEur, 2, MidpointRounding.AwayFromZero);

                        packingListPackageOrderItem.TotalNetWeight =
                            Math.Round(packingListPackageOrderItem.NetWeight * packingListPackageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                        packingListPackageOrderItem.TotalGrossWeight =
                            Math.Round(packingListPackageOrderItem.GrossWeight * packingListPackageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                        packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                        packingListPackageOrderItem.GrossWeight = Math.Round(packingListPackageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                        packingListPackageOrderItem.TotalPriceWithVatOne =
                            packingList.IsVatOneApplied
                                ? decimal.Round(
                                    packingListPackageOrderItem.UnitPrice
                                    + decimal.Round(packingListPackageOrderItem.UnitPrice * packingList.VatOnePercent / 100, 2, MidpointRounding.AwayFromZero),
                                    2,
                                    MidpointRounding.AwayFromZero
                                )
                                : decimal.Zero;

                        packingListPackageOrderItem.TotalPriceWithVatTwo =
                            packingList.IsVatTwoApplied
                                ? decimal.Round(
                                    packingListPackageOrderItem.UnitPrice
                                    + decimal.Round(packingListPackageOrderItem.UnitPrice * packingList.VatTwoPercent / 100, 2, MidpointRounding.AwayFromZero),
                                    2,
                                    MidpointRounding.AwayFromZero
                                )
                                : decimal.Zero;

                        packingListPackageOrderItem.TotalNetPrice =
                            decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                        packingListPackageOrderItem.TotalGrossPrice =
                            decimal.Round(packingListPackageOrderItem.TotalGrossPrice + packingListPackageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                MidpointRounding.AwayFromZero);

                        packingList.TotalNetWeight = Math.Round(packingList.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3, MidpointRounding.AwayFromZero);
                        packingList.TotalGrossWeight = Math.Round(packingList.TotalGrossWeight + packingListPackageOrderItem.TotalGrossWeight, 3,
                            MidpointRounding.AwayFromZero);

                        packingList.TotalNetPrice =
                            decimal.Round(packingList.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                        packingList.TotalGrossPrice =
                            decimal.Round(packingList.TotalGrossPrice + packingListPackageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                        packingList.TotalQuantity = packingList.TotalQuantity + packingListPackageOrderItem.Qty;

                        if (itemProductPlacement != null) packingListPackageOrderItem.ProductPlacements.Add(itemProductPlacement);

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

                            packageOrderItem.TotalNetWeight = packageOrderItem.NetWeight;
                            packageOrderItem.TotalGrossWeight = packageOrderItem.GrossWeight;

                            packageOrderItem.TotalNetPrice =
                                decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                            packageOrderItem.TotalGrossPrice =
                                decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2, MidpointRounding.AwayFromZero);

                            packingList.TotalNetWeight += packageOrderItem.TotalNetWeight;
                            packingList.TotalGrossWeight += packageOrderItem.TotalGrossWeight;

                            packingList.TotalNetPrice =
                                decimal.Round(packingList.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                            packingList.TotalGrossPrice =
                                decimal.Round(packingList.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                            packingList.TotalQuantity = packingList.TotalQuantity + packageOrderItem.Qty;

                            if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                            package.PackingListPackageOrderItems.Add(packageOrderItem);
                        }

                        if (package.Type.Equals(PackingListPackageType.Box))
                            packingList.PackingListBoxes.Add(package);
                        else
                            packingList.PackingListPallets.Add(package);
                    }

                    packingListToReturn = packingList;
                } else {
                    if (packingListPackageOrderItem != null) {
                        if (!packingListToReturn.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                            product.MeasureUnit = measureUnit;

                            if (supplyOrderItem != null)
                                supplyOrderItem.Product = product;

                            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                            supplyInvoiceOrderItem.Product = product;

                            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                            packingListPackageOrderItem.UnitPrice = decimal.Round(packingListPackageOrderItem.UnitPrice, 2, MidpointRounding.AwayFromZero);
                            packingListPackageOrderItem.UnitPriceEur = decimal.Round(packingListPackageOrderItem.UnitPriceEur, 2, MidpointRounding.AwayFromZero);
                            packingListPackageOrderItem.GrossUnitPriceEur = decimal.Round(packingListPackageOrderItem.GrossUnitPriceEur, 2, MidpointRounding.AwayFromZero);

                            packingListPackageOrderItem.TotalNetWeight =
                                Math.Round(packingListPackageOrderItem.NetWeight * packingListPackageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                            packingListPackageOrderItem.TotalGrossWeight =
                                Math.Round(packingListPackageOrderItem.GrossWeight * packingListPackageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                            packingListPackageOrderItem.NetWeight = Math.Round(packingListPackageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                            packingListPackageOrderItem.GrossWeight = Math.Round(packingListPackageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                            packingListPackageOrderItem.TotalNetPrice =
                                decimal.Round(packingListPackageOrderItem.UnitPrice * Convert.ToDecimal(packingListPackageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                            packingListPackageOrderItem.TotalGrossPrice =
                                decimal.Round(packingListPackageOrderItem.TotalGrossPrice + packingListPackageOrderItem.TotalNetPrice * 23m / 100m, 2,
                                    MidpointRounding.AwayFromZero);

                            packingListToReturn.TotalNetWeight = Math.Round(packingListToReturn.TotalNetWeight + packingListPackageOrderItem.TotalNetWeight, 3,
                                MidpointRounding.AwayFromZero);
                            packingListToReturn.TotalGrossWeight = Math.Round(packingListToReturn.TotalGrossWeight + packingListPackageOrderItem.TotalGrossWeight, 3,
                                MidpointRounding.AwayFromZero);

                            packingListPackageOrderItem.TotalPriceWithVatOne =
                                packingList.IsVatOneApplied
                                    ? decimal.Round(
                                        packingListPackageOrderItem.UnitPrice
                                        + decimal.Round(packingListPackageOrderItem.UnitPrice * packingList.VatOnePercent / 100, 2, MidpointRounding.AwayFromZero),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    )
                                    : decimal.Zero;

                            packingListPackageOrderItem.TotalPriceWithVatTwo =
                                packingList.IsVatTwoApplied
                                    ? decimal.Round(
                                        packingListPackageOrderItem.UnitPrice
                                        + decimal.Round(packingListPackageOrderItem.UnitPrice * packingList.VatTwoPercent / 100, 2, MidpointRounding.AwayFromZero),
                                        2,
                                        MidpointRounding.AwayFromZero
                                    )
                                    : decimal.Zero;

                            packingListToReturn.TotalNetPrice =
                                decimal.Round(packingListToReturn.TotalNetPrice + packingListPackageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                            packingListToReturn.TotalGrossPrice =
                                decimal.Round(packingListToReturn.TotalGrossPrice + packingListPackageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                            packingListToReturn.TotalQuantity = packingListToReturn.TotalQuantity + packingListPackageOrderItem.Qty;

                            if (itemProductPlacement != null) packingListPackageOrderItem.ProductPlacements.Add(itemProductPlacement);

                            packingListToReturn.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                        } else if (itemProductPlacement != null) {
                            packingListToReturn
                                .PackingListPackageOrderItems
                                .First(i => i.Id.Equals(packingListPackageOrderItem.Id))
                                .ProductPlacements
                                .Add(itemProductPlacement);
                        }
                    }

                    if (package != null) {
                        if (package.Type.Equals(PackingListPackageType.Box)) {
                            if (packingListToReturn.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                                PackingListPackage packageFromList = packingListToReturn.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                                if (packageOrderItem != null) {
                                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                                        if (packageSupplyOrderItem != null)
                                            packageSupplyOrderItem.Product = packageProduct;

                                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                        packageSupplyInvoiceOrderItem.Product = product;

                                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                        packageOrderItem.TotalNetWeight =
                                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                                        packageOrderItem.TotalGrossWeight =
                                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                                        packageOrderItem.TotalNetPrice =
                                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                                        packageOrderItem.TotalGrossPrice =
                                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2, MidpointRounding.AwayFromZero);

                                        packingListToReturn.TotalNetWeight += packageOrderItem.TotalNetWeight;
                                        packingListToReturn.TotalGrossWeight += packageOrderItem.TotalGrossWeight;

                                        packingListToReturn.TotalNetPrice =
                                            decimal.Round(packingListToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                                        packingListToReturn.TotalGrossPrice =
                                            decimal.Round(packingListToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                                        packingListToReturn.TotalQuantity = packingListToReturn.TotalQuantity + packageOrderItem.Qty;

                                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                    } else if (packageItemProductPlacement != null) {
                                        packageFromList
                                            .PackingListPackageOrderItems
                                            .First(p => p.Id.Equals(packageOrderItem.Id))
                                            .ProductPlacements
                                            .Add(packageItemProductPlacement);
                                    }
                                }
                            } else {
                                if (packageOrderItem != null) {
                                    packageProduct.MeasureUnit = packageProductMeasureUnit;

                                    if (packageSupplyOrderItem != null)
                                        packageSupplyOrderItem.Product = packageProduct;

                                    packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                    packageSupplyInvoiceOrderItem.Product = product;

                                    packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                    packageOrderItem.TotalNetWeight =
                                        Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                                    packageOrderItem.TotalGrossWeight =
                                        Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                                    packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                                    packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                                    packageOrderItem.TotalNetPrice =
                                        decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                                    packageOrderItem.TotalGrossPrice =
                                        decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2, MidpointRounding.AwayFromZero);

                                    packingListToReturn.TotalNetWeight += packageOrderItem.TotalNetWeight;
                                    packingListToReturn.TotalGrossWeight += packageOrderItem.TotalGrossWeight;

                                    packingListToReturn.TotalNetPrice =
                                        decimal.Round(packingListToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                                    packingListToReturn.TotalGrossPrice =
                                        decimal.Round(packingListToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                                    packingListToReturn.TotalQuantity = packingListToReturn.TotalQuantity + packageOrderItem.Qty;

                                    if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                                    package.PackingListPackageOrderItems.Add(packageOrderItem);
                                }

                                packingListToReturn.PackingListBoxes.Add(package);
                            }
                        } else {
                            if (packingListToReturn.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                                PackingListPackage packageFromList = packingListToReturn.PackingListPallets.First(p => p.Id.Equals(package.Id));

                                if (packageOrderItem != null) {
                                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                                        if (packageSupplyOrderItem != null)
                                            packageSupplyOrderItem.Product = packageProduct;

                                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                        packageSupplyInvoiceOrderItem.Product = product;

                                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                        packageOrderItem.TotalNetWeight =
                                            Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                                        packageOrderItem.TotalGrossWeight =
                                            Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                                        packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                                        packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                                        packageOrderItem.TotalNetPrice =
                                            decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                                        packageOrderItem.TotalGrossPrice =
                                            decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2, MidpointRounding.AwayFromZero);

                                        packingListToReturn.TotalNetWeight += packageOrderItem.TotalNetWeight;
                                        packingListToReturn.TotalGrossWeight += packageOrderItem.TotalGrossWeight;

                                        packingListToReturn.TotalNetPrice =
                                            decimal.Round(packingListToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                                        packingListToReturn.TotalGrossPrice =
                                            decimal.Round(packingListToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                                        packingListToReturn.TotalQuantity = packingListToReturn.TotalQuantity + packageOrderItem.Qty;

                                        if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                    } else if (packageItemProductPlacement != null) {
                                        packageFromList
                                            .PackingListPackageOrderItems
                                            .First(p => p.Id.Equals(packageOrderItem.Id))
                                            .ProductPlacements
                                            .Add(packageItemProductPlacement);
                                    }
                                }
                            } else {
                                if (packageOrderItem != null) {
                                    packageProduct.MeasureUnit = packageProductMeasureUnit;

                                    if (packageSupplyOrderItem != null)
                                        packageSupplyOrderItem.Product = packageProduct;

                                    packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                    packageSupplyInvoiceOrderItem.Product = product;

                                    packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                    packageOrderItem.TotalNetWeight =
                                        Math.Round(packageOrderItem.NetWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);
                                    packageOrderItem.TotalGrossWeight =
                                        Math.Round(packageOrderItem.GrossWeight * packageOrderItem.Qty, 3, MidpointRounding.AwayFromZero);

                                    packageOrderItem.NetWeight = Math.Round(packageOrderItem.NetWeight, 3, MidpointRounding.AwayFromZero);
                                    packageOrderItem.GrossWeight = Math.Round(packageOrderItem.GrossWeight, 3, MidpointRounding.AwayFromZero);

                                    packageOrderItem.TotalNetPrice =
                                        decimal.Round(packageOrderItem.UnitPrice * Convert.ToDecimal(packageOrderItem.Qty), 2, MidpointRounding.AwayFromZero);

                                    packageOrderItem.TotalGrossPrice =
                                        decimal.Round(packageOrderItem.TotalGrossPrice + packageOrderItem.TotalNetPrice * 23m / 100m, 2, MidpointRounding.AwayFromZero);

                                    packingListToReturn.TotalNetWeight += packageOrderItem.TotalNetWeight;
                                    packingListToReturn.TotalGrossWeight += packageOrderItem.TotalGrossWeight;

                                    packingListToReturn.TotalNetPrice =
                                        decimal.Round(packingListToReturn.TotalNetPrice + packageOrderItem.TotalNetPrice, 2, MidpointRounding.AwayFromZero);

                                    packingListToReturn.TotalGrossPrice =
                                        decimal.Round(packingListToReturn.TotalGrossPrice + packageOrderItem.TotalGrossPrice, 2, MidpointRounding.AwayFromZero);

                                    packingListToReturn.TotalQuantity = packingListToReturn.TotalQuantity + packageOrderItem.Qty;

                                    if (packageItemProductPlacement != null) packageOrderItem.ProductPlacements.Add(packageItemProductPlacement);

                                    package.PackingListPackageOrderItems.Add(packageOrderItem);
                                }

                                packingListToReturn.PackingListPallets.Add(package);
                            }
                        }
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
            "LEFT JOIN [ProductPlacement] AS [ItemProductPlacement] " +
            "ON [ItemProductPlacement].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [ProductPlacement] AS [PackageProductPlacement] " +
            "ON [PackageProductPlacement].PackingListPackageOrderItemID = [PalletPackageOrderItem].ID " +
            "WHERE [PackingList].NetUID = @NetId",
            types,
            packingListMapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (packingListToReturn != null)
            _connection.Query<PackingList, InvoiceDocument, PackingList>(
                "SELECT * " +
                "FROM [PackingList] " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "WHERE [PackingList].NetUID = @NetId",
                (packingList, document) => {
                    if (document != null) packingListToReturn.InvoiceDocuments.Add(document);

                    return packingList;
                },
                new { NetId = netId }
            );

        return packingListToReturn;
    }

    public PackingList GetByNetId(Guid netId) {
        PackingList packingListToReturn = null;

        Type[] types = {
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
            typeof(MeasureUnit)
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

            if (packingList != null) {
                if (packingListToReturn == null) {
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

                    packingListToReturn = packingList;
                } else {
                    if (packingListPackageOrderItem != null)
                        if (!packingListToReturn.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                            product.MeasureUnit = measureUnit;

                            if (supplyOrderItem != null)
                                supplyOrderItem.Product = product;

                            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                            supplyInvoiceOrderItem.Product = product;

                            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                            packingListToReturn.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                        }

                    if (package != null) {
                        if (package.Type.Equals(PackingListPackageType.Box)) {
                            if (packingListToReturn.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                                PackingListPackage packageFromList = packingListToReturn.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                                if (packageOrderItem != null)
                                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                                        if (packageSupplyOrderItem != null)
                                            packageSupplyOrderItem.Product = packageProduct;

                                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                        packageSupplyInvoiceOrderItem.Product = product;

                                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                    }
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

                                packingListToReturn.PackingListBoxes.Add(package);
                            }
                        } else {
                            if (packingListToReturn.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                                PackingListPackage packageFromList = packingListToReturn.PackingListPallets.First(p => p.Id.Equals(package.Id));

                                if (packageOrderItem != null)
                                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                                        if (packageSupplyOrderItem != null)
                                            packageSupplyOrderItem.Product = packageProduct;

                                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                        packageSupplyInvoiceOrderItem.Product = product;

                                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                    }
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

                                packingListToReturn.PackingListPallets.Add(package);
                            }
                        }
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
            "WHERE [PackingList].NetUID = @NetId",
            types,
            packingListMapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (packingListToReturn != null)
            _connection.Query<PackingList, InvoiceDocument, PackingList>(
                "SELECT * " +
                "FROM [PackingList] " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "WHERE [PackingList].NetUID = @NetId",
                (packingList, document) => {
                    if (document != null) packingListToReturn.InvoiceDocuments.Add(document);

                    return packingList;
                },
                new { NetId = netId }
            );

        return packingListToReturn;
    }

    public PackingList GetByNetIdWithContainerOrVehicleInfo(Guid netId) {
        PackingList packingListToReturn = null;

        Type[] types = {
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
            typeof(MeasureUnit)
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

            if (packingList != null) {
                if (packingListToReturn == null) {
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

                    packingListToReturn = packingList;
                } else {
                    if (packingListPackageOrderItem != null)
                        if (!packingListToReturn.PackingListPackageOrderItems.Any(i => i.Id.Equals(packingListPackageOrderItem.Id))) {
                            product.MeasureUnit = measureUnit;

                            if (supplyOrderItem != null)
                                supplyOrderItem.Product = product;

                            supplyInvoiceOrderItem.Product = product;
                            supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;

                            packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                            packingListToReturn.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                        }

                    if (package != null) {
                        if (package.Type.Equals(PackingListPackageType.Box)) {
                            if (packingListToReturn.PackingListBoxes.Any(p => p.Id.Equals(package.Id))) {
                                PackingListPackage packageFromList = packingListToReturn.PackingListBoxes.First(p => p.Id.Equals(package.Id));

                                if (packageOrderItem != null)
                                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                                        if (packageSupplyOrderItem != null)
                                            packageSupplyOrderItem.Product = packageProduct;

                                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                        packageSupplyInvoiceOrderItem.Product = product;

                                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                    }
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

                                packingListToReturn.PackingListBoxes.Add(package);
                            }
                        } else {
                            if (packingListToReturn.PackingListPallets.Any(p => p.Id.Equals(package.Id))) {
                                PackingListPackage packageFromList = packingListToReturn.PackingListPallets.First(p => p.Id.Equals(package.Id));

                                if (packageOrderItem != null)
                                    if (!packageFromList.PackingListPackageOrderItems.Any(p => p.Id.Equals(packageOrderItem.Id))) {
                                        packageProduct.MeasureUnit = packageProductMeasureUnit;

                                        if (packageSupplyOrderItem != null)
                                            packageSupplyOrderItem.Product = packageProduct;

                                        packageSupplyInvoiceOrderItem.SupplyOrderItem = packageSupplyOrderItem;
                                        packageSupplyInvoiceOrderItem.Product = product;

                                        packageOrderItem.SupplyInvoiceOrderItem = packageSupplyInvoiceOrderItem;

                                        packageFromList.PackingListPackageOrderItems.Add(packageOrderItem);
                                    }
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

                                packingListToReturn.PackingListPallets.Add(package);
                            }
                        }
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
            "WHERE [PackingList].NetUID = @NetId",
            types,
            packingListMapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (packingListToReturn != null) {
            _connection.Query<PackingList, InvoiceDocument, PackingList>(
                "SELECT * " +
                "FROM [PackingList] " +
                "LEFT JOIN [InvoiceDocument] " +
                "ON [InvoiceDocument].PackingListID = [PackingList].ID " +
                "AND [InvoiceDocument].Deleted = 0 " +
                "WHERE [PackingList].NetUID = @NetId",
                (packingList, document) => {
                    if (document != null) packingListToReturn.InvoiceDocuments.Add(document);

                    return packingList;
                },
                new { NetId = netId }
            );

            if (packingListToReturn.ContainerServiceId.HasValue)
                packingListToReturn.ContainerService =
                    _connection.Query<ContainerService, SupplyOrganizationAgreement, Currency, ContainerService>(
                            "SELECT * " +
                            "FROM [ContainerService] " +
                            "LEFT JOIN [SupplyOrganizationAgreement] " +
                            "ON [SupplyOrganizationAgreement].ID = [ContainerService].SupplyOrganizationAgreementID " +
                            "LEFT JOIN [Currency] " +
                            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                            "WHERE [ContainerService].ID = @Id",
                            (container, agreement, currency) => {
                                agreement.Currency = currency;

                                container.SupplyOrganizationAgreement = agreement;

                                return container;
                            },
                            new { Id = packingListToReturn.ContainerServiceId.Value }
                        )
                        .Single();
            else if (packingListToReturn.VehicleServiceId.HasValue)
                packingListToReturn.VehicleService =
                    _connection.Query<VehicleService, SupplyOrganizationAgreement, Currency, VehicleService>(
                            "SELECT * " +
                            "FROM [VehicleService] " +
                            "LEFT JOIN [SupplyOrganizationAgreement] " +
                            "ON [SupplyOrganizationAgreement].ID = [VehicleService].SupplyOrganizationAgreementID " +
                            "LEFT JOIN [Currency] " +
                            "ON [Currency].ID = [SupplyOrganizationAgreement].CurrencyID " +
                            "WHERE [VehicleService].ID = @Id",
                            (vehicle, agreement, currency) => {
                                agreement.Currency = currency;

                                vehicle.SupplyOrganizationAgreement = agreement;

                                return vehicle;
                            },
                            new { Id = packingListToReturn.VehicleServiceId.Value }
                        )
                        .Single();
        }

        return packingListToReturn;
    }

    public PackingList GetByNetIdWithInvoice(Guid netId) {
        return _connection.Query<PackingList, SupplyInvoice, PackingList>(
                "SELECT * " +
                "FROM [PackingList] " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "WHERE [PackingList].NetUID = @NetId",
                (packingList, supplyInvoice) => {
                    packingList.SupplyInvoice = supplyInvoice;

                    return packingList;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public PackingList GetByNetIdWithOrderInfo(Guid netId) {
        return _connection.Query<PackingList, SupplyInvoice, SupplyOrder, Organization, PackingList>(
                "SELECT * " +
                "FROM [PackingList] " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].ID = [SupplyOrder].OrganizationID " +
                "WHERE [PackingList].NetUID = @NetId",
                (packingList, supplyInvoice, supplyOrder, organization) => {
                    supplyOrder.Organization = organization;

                    supplyInvoice.SupplyOrder = supplyOrder;

                    packingList.SupplyInvoice = supplyInvoice;

                    return packingList;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public void Update(PackingList packingList) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET [MarkNumber] = @MarkNumber, SupplyInvoiceId = @SupplyInvoiceId, [InvNo] = @InvNo, [PlNo] = @PlNo, [RefNo] = @RefNo, [No] = @No, [FromDate] = @FromDate, " +
            "[IsDocumentsAdded] = @IsDocumentsAdded, [ExtraCharge] = @ExtraCharge, ContainerServiceId = @ContainerServiceId, Comment = @Comment, Updated = getutcdate(), " +
            "AccountingExtraCharge = @AccountingExtraCharge " +
            "WHERE [PackingList].NetUID = @NetUid",
            packingList
        );
    }

    public void UpdateSupplyInvoiceIdAndRootId(IEnumerable<PackingList> packingLists) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET SupplyInvoiceId = @SupplyInvoiceId, RootPackingListID = @RootPackingListId, Deleted = 1, Updated = getutcdate() " +
            "WHERE [PackingList].NetUID = @NetUid",
            packingLists
        );
    }

    public void UpdateVats(PackingList packingList) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET IsVatOneApplied = @IsVatOneApplied, IsVatTwoApplied = @IsVatTwoApplied, VatOnePercent = @VatOnePercent, VatTwoPercent = @VatTwoPercent, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            packingList
        );
    }

    public void UpdateIsPlaced(PackingList packingList) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET IsPlaced = @IsPlaced, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            packingList
        );
    }

    public void UpdateExtraCharge(IEnumerable<PackingList> packingLists) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET [ExtraCharge] = @ExtraCharge, Updated = getutcdate(), " +
            "[AccountingExtraCharge] = @AccountingExtraCharge " +
            "WHERE [PackingList].NetUID = @NetUid",
            packingLists
        );
    }

    public void AssignProvidedToContainerService(long containerServiceId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET ContainerServiceId = @ContainerServiceId " +
            "WHERE [PackingList].ID IN @Ids",
            new { ContainerServiceId = containerServiceId, Ids = ids }
        );
    }

    public void UnassignAllByContainerServiceId(long containerServiceId) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET ContainerServiceId = NULL " +
            "WHERE [PackingList].ContainerServiceID = @ContainerServiceId",
            new { ContainerServiceId = containerServiceId }
        );
    }

    public void UnassignAllByContainerServiceIdExceptProvided(long containerServiceId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET ContainerServiceId = NULL " +
            "WHERE [PackingList].ContainerServiceID = @ContainerServiceId AND [PackingList].ID NOT IN @Ids",
            new { ContainerServiceId = containerServiceId, Ids = ids }
        );
    }

    public void RemoveAllByInvoiceId(long invoiceId) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PackingList].SupplyInvoiceID = @InvoiceId",
            new { InvoiceId = invoiceId }
        );
    }

    public void RemoveAllByInvoiceIdExceptProvided(long invoiceId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PackingList].SupplyInvoiceID = @InvoiceId AND [PackingList].ID NOT IN @Ids",
            new { InvoiceId = invoiceId, Ids = ids }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [PackingList].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void SetPlaced(long id, bool value) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET IsPlaced = @Value " +
            "WHERE ID = @Id",
            new { Id = id, Value = value }
        );
    }

    public void UnassigningAllByContainerAndSupplyOrderId(
        long supplyOrderId,
        long containerServiceId) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET [PackingList].[ContainerServiceID] = null " +
            ", [Updated] = GETUTCDATE() " +
            "FROM [PackingList] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "WHERE [PackingList].[ContainerServiceID] = @ServiceId " +
            "AND [SupplyInvoice].[SupplyOrderID] = @SupplyOrderId; ",
            new {
                SupplyOrderId = supplyOrderId,
                ServiceId = containerServiceId
            });
    }

    public void UnassigningAllByVehicleAndSupplyOrderId(
        long supplyOrderId,
        long vehicleServiceId) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET [PackingList].[VehicleServiceID] = null " +
            ", [Updated] = GETUTCDATE() " +
            "FROM [PackingList] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "WHERE [PackingList].[VehicleServiceID] = @ServiceId " +
            "AND [SupplyInvoice].[SupplyOrderID] = @SupplyOrderId; ",
            new {
                SupplyOrderId = supplyOrderId,
                ServiceId = vehicleServiceId
            });
    }

    public void UnassigninAllByContainerServiceId(long containerServiceId) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET [PackingList].[ContainerServiceID] = null " +
            ", [Updated] = GETUTCDATE() " +
            "FROM [PackingList] " +
            "WHERE [PackingList].[ContainerServiceID] = @Id " +
            new { Id = containerServiceId });
    }

    public void UnassigninAllByVehicleServiceId(long vehicleServiceId) {
        _connection.Execute(
            "UPDATE [PackingList] " +
            "SET [PackingList].[VehicleServiceID] = null " +
            ", [Updated] = GETUTCDATE() " +
            "FROM [PackingList] " +
            "WHERE [PackingList].[VehicleServiceID] = @Id " +
            new { Id = vehicleServiceId });
    }
}