using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.DataSync.Contracts;

namespace GBA.Domain.Repositories.DataSync;

public sealed class GbaDataExportRepository : IGbaDataExportRepository {
    private readonly IDbConnection _connection;

    public GbaDataExportRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<PackingList> GetPackingListForSpecification(DateTime from, DateTime to) {
        bool isPlCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("pl");

        List<PackingList> toReturn = new List<PackingList>();

        List<long> packingListIds = _connection.Query<long>(
            "SELECT [PackingList].ID " +
            "FROM [PackingList] " +
            "LEFT JOIN [SupplyInvoice] ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "WHERE [SupplyInvoice].[SupplyOrganizationAgreementID] IS NOT NULL " +
            "AND [SupplyOrder].IsFullyPlaced = 1 "+
            "AND [SupplyInvoice].SupplyOrganizationID IS NOT NULL " +
            "AND [SupplyInvoice].[NumberCustomDeclaration] IS NOT NULL " +
            "AND [PackingList].Created >= @From " +
            "AND [PackingList].Created <= @To " +
            "AND [PackingList].Deleted = 0",
            new {
                From = from,
                To = to,
            }
        ).ToList();


        Type[] types = {
            typeof(PackingList),
            typeof(PackingListPackageOrderItem),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyOrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductSpecification),
            typeof(User),
        };

        Func<object[], PackingList> packingListItemsMapper = objects => {
            PackingList packingList = (PackingList)objects[0];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[1];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[2];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[6];
            ProductGroup productGroup = (ProductGroup)objects[7];
            ProductSpecification productSpecification = (ProductSpecification)objects[8];
            User user = (User)objects[9];

            // ---- 1. Ensure PackingList instance exists ----
            if (!toReturn.Any(p => p.Id.Equals(packingList.Id))) {
                if (product != null) {
                    if (productProductGroup != null && productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        product.ProductProductGroups.Add(productProductGroup);
                    }

                    product.MeasureUnit = measureUnit;
                    product.Name = isPlCulture ? product.NamePL : product.NameUA;
                }

                // ---- 5. SupplyOrderItem + Product ----
                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                // ---- 6. SupplyInvoiceOrderItem ----
                if (supplyInvoiceOrderItem != null) {
                    supplyInvoiceOrderItem.Product = product;
                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    // ---- 7. Link order item to package ----
                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;
                }

                if (packingListPackageOrderItem != null) {
                    // ---- 8. Calculate weights & prices ----
                    packingListPackageOrderItem.TotalNetWeight = packingListPackageOrderItem.Qty * packingListPackageOrderItem.NetWeight;
                    packingListPackageOrderItem.TotalGrossWeight = packingListPackageOrderItem.Qty * packingListPackageOrderItem.GrossWeight;

                    packingListPackageOrderItem.TotalNetPrice =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.UnitPrice;

                    packingListPackageOrderItem.TotalNetPriceEur =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.UnitPriceEur;
                }
                

                // ---- 9. Product specification mapping ----
                if (productSpecification != null) {
                    productSpecification.AddedBy = user;
                    product.ProductSpecifications.Add(productSpecification);
                }

                if (packingListPackageOrderItem != null) {
                    // ---- 10. Accounting price calculations ----
                    packingListPackageOrderItem.AccountingTotalGrossPriceEur =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.AccountingGrossUnitPriceEur;

                    packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.AccountingGeneralGrossUnitPriceEur;

                    packingListPackageOrderItem.TotalGrossPriceEur =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.GrossUnitPriceEur
                        + packingListPackageOrderItem.AccountingTotalGrossPriceEur
                        + packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur;

                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                }

                toReturn.Add(packingList);
            } else {
                PackingList existingPackingList = toReturn.FirstOrDefault(p => p.Id.Equals(packingList.Id));

                if (packingListPackageOrderItem == null)
                    return existingPackingList;
                
                // ---- 3. Ensure PKG item exists only once ----
                if (!existingPackingList.PackingListPackageOrderItems.Any(i => i.Id == packingListPackageOrderItem.Id)) {
                    existingPackingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);
                } else {
                    packingListPackageOrderItem = existingPackingList.PackingListPackageOrderItems
                        .First(i => i.Id == packingListPackageOrderItem.Id);
                }
                
                if (product != null) {
                    if (productProductGroup != null && productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        product.ProductProductGroups.Add(productProductGroup);
                    }

                    product.MeasureUnit = measureUnit;
                    product.Name = isPlCulture ? product.NamePL : product.NameUA;
                }

                if (supplyOrderItem != null)
                    supplyOrderItem.Product = product;

                if (supplyInvoiceOrderItem != null) {
                    supplyInvoiceOrderItem.Product = product;
                    supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                    packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;
                }

                if (packingListPackageOrderItem != null) {

                    packingListPackageOrderItem.TotalNetWeight = packingListPackageOrderItem.Qty * packingListPackageOrderItem.NetWeight;
                    packingListPackageOrderItem.TotalGrossWeight = packingListPackageOrderItem.Qty * packingListPackageOrderItem.GrossWeight;

                    packingListPackageOrderItem.TotalNetPrice =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.UnitPrice;

                    packingListPackageOrderItem.TotalNetPriceEur =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.UnitPriceEur;
                }

                if (productSpecification != null) {
                    productSpecification.AddedBy = user;
                    product.ProductSpecifications.Add(productSpecification);

                    existingPackingList.TotalCustomValue += productSpecification.CustomsValue;
                    existingPackingList.TotalVatAmount += productSpecification.VATValue;
                    existingPackingList.TotalDuty += productSpecification.Duty;
                }

                if (packingListPackageOrderItem != null) {

                    packingListPackageOrderItem.AccountingTotalGrossPriceEur =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.AccountingGrossUnitPriceEur;

                    packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.AccountingGeneralGrossUnitPriceEur;

                    packingListPackageOrderItem.TotalGrossPriceEur =
                        (decimal)packingListPackageOrderItem.Qty * packingListPackageOrderItem.GrossUnitPriceEur
                        + packingListPackageOrderItem.AccountingTotalGrossPriceEur
                        + packingListPackageOrderItem.AccountingGeneralTotalGrossPriceEur;

                    packingList.PackingListPackageOrderItems.Add(packingListPackageOrderItem);

                    existingPackingList.TotalQuantity += packingListPackageOrderItem.Qty;

                    existingPackingList.TotalNetPrice += packingListPackageOrderItem.TotalNetPrice;
                    existingPackingList.TotalGrossPrice += packingListPackageOrderItem.TotalGrossPrice;
                    existingPackingList.AccountingTotalGrossPrice += packingListPackageOrderItem.AccountingTotalGrossPrice;
                    existingPackingList.AccountingTotalGrossPriceEur += packingListPackageOrderItem.AccountingTotalGrossPriceEur;
                    existingPackingList.TotalGrossPriceEur += packingListPackageOrderItem.TotalGrossPriceEur;

                    existingPackingList.TotalNetWeight += packingListPackageOrderItem.TotalNetWeight;
                    existingPackingList.TotalGrossWeight += packingListPackageOrderItem.TotalGrossWeight;
                }

                existingPackingList.TotalNetWeight = Math.Round(existingPackingList.TotalNetWeight, 3);
                existingPackingList.TotalGrossWeight = Math.Round(existingPackingList.TotalGrossWeight, 3);
            }

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
            "WHERE [PackingList].ID IN @Ids " +
            "GROUP BY [Product].[ID], [ProductSpecification].[VATValue], [OrderProductSpecification].Qty, [OrderProductSpecification].UnitPrice " +
            ") " +
            "SELECT PackingList.* " +
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
            ", [ProductProductGroup].* " +
            ", [ProductGroup].* " +
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
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].[ProductID] = [Product].[ID] " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].[ID] = [ProductProductGroup].[ProductGroupID] " +
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
            "WHERE [PackingList].ID IN @Ids " +
            "ORDER BY [SupplyInvoiceOrderItem].[RowNumber] ",
            types,
            packingListItemsMapper,
            new {
                Ids = packingListIds, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower()
            },
            commandTimeout: 10000
        );

        return toReturn;
    }

    public SupplyInvoice GetSupplyInvoiceByPackingListNetId(Guid netId) {
        
        Type[] types = {
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(SupplyOrganizationAgreement),
            typeof(SupplyOrganization),
        };

        Func<object[], SupplyInvoice> packingListItemsMapper = objects =>
        {
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            Client client = (Client)objects[2];
            ClientAgreement clientAgreement = (ClientAgreement)objects[3];
            Agreement agreement = (Agreement)objects[4];
            Organization organization = (Organization)objects[5];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[6];
            SupplyOrganization supplyOrganization = (SupplyOrganization)objects[7];

            supplyOrder.Client = client;
            clientAgreement.Agreement = agreement;
            supplyOrder.ClientAgreement = clientAgreement;
            supplyOrder.Organization = organization;
            supplyInvoice.SupplyOrder = supplyOrder;
            supplyInvoice.SupplyOrganizationAgreement = supplyOrganizationAgreement;
            supplyInvoice.SupplyOrganization = supplyOrganization;
            return supplyInvoice;
        };

       return _connection.Query(
                "SELECT [SupplyInvoice].*, SupplyOrder.*, Client.*, ClientAgreement.*, Agreement.*, Organization.*,SupplyOrganizationAgreement.*,SupplyOrganization.* " +
                "FROM [PackingList] " +
                "LEFT JOIN [SupplyInvoice] " +
                "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
                "LEFT JOIN SupplyOrder " +
                "ON SupplyOrder.ID = SupplyInvoice.SupplyOrderID " +
                "LEFT JOIN Client " +
                "ON Client.ID = SupplyOrder.ClientID " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.ID = SupplyOrder.ClientAgreementID " +
                "LEFT JOIN Agreement " +
                "ON Agreement.ID = ClientAgreement.AgreementID " +
                "LEFT JOIN Organization " +
                "ON Organization.ID = SupplyOrder.OrganizationID " +
                "LEFT JOIN SupplyOrganizationAgreement "+
                "ON SupplyOrganizationAgreement.ID = SupplyInvoice.SupplyOrganizationAgreementID " +
                "LEFT JOIN SupplyOrganization "+
                "ON SupplyOrganization.ID = SupplyInvoice.SupplyOrganizationID " +
                "WHERE [PackingList].[NetUID] = @NetId ",
                types,
                packingListItemsMapper,
                new { NetId = netId })
            .FirstOrDefault();}
}