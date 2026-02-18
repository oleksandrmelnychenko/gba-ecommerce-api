using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.DepreciatedOrders;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Transfers;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.EntityHelpers.ReSaleModels;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.ReSales;

public sealed class ReSaleRepository : IReSaleRepository {
    private readonly IDbConnection _connection;

    public ReSaleRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ReSale reSale) {
        return _connection.Query<long>(
            "INSERT INTO [ReSale] " +
            "([Comment], [ClientAgreementID], [OrganizationID], [UserID], [Updated], [SaleNumberID], [BaseLifeCycleStatusID], [BaseSalePaymentStatusID], " +
            "[FromStorageID], [IsCompleted], [TotalPaymentAmount]) " +
            "VALUES " +
            "(@Comment, @ClientAgreementId, @OrganizationId, @UserId, GETUTCDATE(), @SaleNumberId, @BaseLifeCycleStatusId, @BaseSalePaymentStatusID, " +
            "@FromStorageId, @IsCompleted, @TotalPaymentAmount); " +
            "SELECT SCOPE_IDENTITY()",
            reSale
        ).Single();
    }

    public void Update(ReSale reSale) {
        _connection.Execute(
            "UPDATE [ReSale] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [Comment] = @Comment " +
            ", [ClientAgreementID] = @ClientAgreementId " +
            ", [OrganizationID] = @OrganizationId " +
            ", [UserID] = @UserId " +
            ", [SaleNumberID] = @SaleNumberId " +
            ", [BaseLifeCycleStatusID] = @BaseLifeCycleStatusId " +
            ", [BaseSalePaymentStatusID] = @BaseSalePaymentStatusID " +
            ", [FromStorageID] = @FromStorageId " +
            ", [IsCompleted] = @IsCompleted " +
            ", [TotalPaymentAmount] = @TotalPaymentAmount " +
            "WHERE [ID] = @Id ",
            reSale
        );
    }

    public void Delete(long id) {
        _connection.Execute(
            "UPDATE [ReSale] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [Deleted] = 1 " +
            "WHERE [ID] = @Id ",
            new { Id = id }
        );
    }

    public ReSale GetById(long id) {
        ReSale toReturn = null;

        Type[] types = {
            typeof(ReSale),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(ReSaleItem),
            typeof(ReSaleAvailability),
            typeof(OrderItem),
            typeof(Product),
            typeof(Organization),
            typeof(SaleNumber)
        };

        Func<object[], ReSale> mapper = objects => {
            ReSale reSale = (ReSale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Client client = (Client)objects[2];
            ReSaleItem reSaleItem = (ReSaleItem)objects[3];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            Product product = (Product)objects[6];
            Organization organization = (Organization)objects[7];
            SaleNumber saleNumber = (SaleNumber)objects[8];

            if (toReturn == null) {
                toReturn = reSale;

                clientAgreement.Client = client;

                toReturn.ClientAgreement = clientAgreement;
                toReturn.Organization = organization;

                toReturn.SaleNumber = saleNumber;
            }

            if (reSaleItem == null) return reSale;

            if (orderItem != null) {
                orderItem.Product = product;
                reSaleAvailability.OrderItem = orderItem;
            }

            reSaleItem.ReSaleAvailability = reSaleAvailability;

            toReturn.ReSaleItems.Add(reSaleItem);

            return reSale;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ReSale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [ReSaleItem] " +
            "ON [ReSaleItem].ReSaleID = [ReSale].ID " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].[ID] = [ReSaleItem].[ReSaleAvailabilityID] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [ReSaleAvailability].OrderItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [ReSale].OrganizationID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].ID = [ReSale].[SaleNumberID] " +
            "WHERE [ReSale].ID = @Id",
            types,
            mapper,
            new { Id = id }
        );

        return toReturn;
    }

    public ReSale GetByNetId(Guid netId) {
        ReSale toReturn = null;

        Type[] reSaleTypes = {
            typeof(ReSale),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Organization),
            typeof(Agreement),
            typeof(User),
            typeof(SaleNumber),
            typeof(Storage),
            typeof(ReSaleItem),
            typeof(ReSaleAvailability),
            typeof(OrderItem),
            typeof(ConsignmentItem),
            typeof(Product),
            typeof(ProductAvailability),
            typeof(ProductReservation),
            typeof(MeasureUnit),
            typeof(DepreciatedOrderItem),
            typeof(ProductTransferItem),
            typeof(Currency),
            typeof(Organization),
            typeof(RegionCode),
            typeof(ProductSpecification)
        };

        Func<object[], ReSale> reSaleMapper = objects => {
            ReSale reSale = (ReSale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Client client = (Client)objects[2];
            Organization organization = (Organization)objects[3];
            Agreement agreement = (Agreement)objects[4];
            User user = (User)objects[5];
            SaleNumber saleNumber = (SaleNumber)objects[6];
            Storage storage = (Storage)objects[7];
            ReSaleItem reSaleItem = (ReSaleItem)objects[8];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[9];
            OrderItem orderItem = (OrderItem)objects[10];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[11];
            Product product = (Product)objects[12];
            ProductAvailability productAvailability = (ProductAvailability)objects[13];
            ProductReservation productReservation = (ProductReservation)objects[14];
            MeasureUnit measureUnit = (MeasureUnit)objects[15];
            DepreciatedOrderItem depreciatedOrderItem = (DepreciatedOrderItem)objects[16];
            ProductTransferItem productTransferItem = (ProductTransferItem)objects[17];
            Currency currency = (Currency)objects[18];
            Organization clientOrganization = (Organization)objects[19];
            RegionCode regionCode = (RegionCode)objects[20];
            ProductSpecification productSpecification = (ProductSpecification)objects[21];

            if (toReturn == null) {
                if (clientAgreement != null) {
                    agreement.Currency = currency;
                    client.RegionCode = regionCode;
                    clientAgreement.Client = client;
                    agreement.Organization = clientOrganization;
                    clientAgreement.Agreement = agreement;
                    reSale.ClientAgreement = clientAgreement;
                }

                reSale.Organization = organization;
                reSale.User = user;
                reSale.SaleNumber = saleNumber;

                toReturn = reSale;
            }

            if (reSaleItem == null) return reSale;

            reSaleItem.PricePerItem = decimal.Round(
                reSaleItem.PricePerItem, 2, MidpointRounding.AwayFromZero);

            reSaleItem.TotalPrice = decimal.Round(
                Convert.ToDecimal(reSaleItem.Qty) * reSaleItem.PricePerItem, 2, MidpointRounding.AwayFromZero);

            toReturn.TotalPrice = decimal.Round(
                toReturn.TotalPrice + reSaleItem.TotalPrice, 2, MidpointRounding.AwayFromZero);

            toReturn.TotalQty += reSaleItem.Qty;

            product.MeasureUnit = measureUnit;

            if (reSaleAvailability != null) {
                consignmentItem.Product = product;
                consignmentItem.ProductSpecification = productSpecification;
                reSaleAvailability.ConsignmentItem = consignmentItem;
                reSaleAvailability.ProductAvailability = productAvailability;
                reSaleAvailability.ProductReservation = productReservation;
                reSaleAvailability.OrderItem = orderItem;
                reSaleAvailability.DepreciatedOrderItem = depreciatedOrderItem;
                reSaleAvailability.ProductTransferItem = productTransferItem;
                reSaleItem.ReSaleAvailability = reSaleAvailability;
            }

            toReturn.FromStorage = storage;
            reSaleItem.Product = product;

            toReturn.ReSaleItems.Add(reSaleItem);

            return reSale;
        };

        _connection.Query(
            "SELECT " +
            "[ReSale].* " +
            ", [ClientAgreement].* " +
            ", [Client].* " +
            ", [Organization].* " +
            ", [Agreement].* " +
            ", [User].* " +
            ", [SaleNumber].* " +
            ", [Storage].* " +
            ", [ReSaleItem].* " +
            ", [ReSaleAvailability].* " +
            ", [OrderItem].* " +
            ", [ConsignmentItem].* " +
            ", [Product].* " +
            ", [ProductAvailability].* " +
            ", [ProductReservation].* " +
            ", [MeasureUnit].* " +
            ", [DepreciatedOrderItem].* " +
            ", [ProductTransferItem].* " +
            ", [Currency].* " +
            ", [ClientOrganization].* " +
            ", [RegionCode].* " +
            ", [ProductSpecification].* " +
            "FROM [ReSale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [ReSale].OrganizationID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [ReSale].[UserID] " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].[ID] = [ReSale].[SaleNumberID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ReSale].[FromStorageID] " +
            "LEFT JOIN [ReSaleItem] " +
            "ON [ReSaleItem].[ReSaleID] = [ReSale].[ID] " +
            "AND [ReSaleItem].[Deleted] = 0 " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].[ID] = [ReSaleItem].[ReSaleAvailabilityID] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].[ID] = [ReSaleAvailability].[OrderItemID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ReSaleItem].[ProductID] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].[ID] = [ReSaleAvailability].[ProductAvailabilityID] " +
            "LEFT JOIN [ProductReservation] " +
            "ON [ProductReservation].[ID] = [ReSaleAvailability].[ProductReservationID] " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [DepreciatedOrderItem] " +
            "ON [DepreciatedOrderItem].[ID] = [ReSaleAvailability].[DepreciatedOrderItemID] " +
            "LEFT JOIN [ProductTransferItem] " +
            "ON [ProductTransferItem].[ID] = [ReSaleAvailability].[ProductTransferItemID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Organization] AS [ClientOrganization] " +
            "ON [ClientOrganization].[ID] = [Agreement].[OrganizationID] " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].[RegionCodeID] = [RegionCode].[ID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE ReSale.NetUID = @NetId " +
            "AND ReSale.Deleted = 0 ",
            reSaleTypes,
            reSaleMapper,
            new { NetId = netId });

        return toReturn;
    }

    public ReSale GetForDocumentExportByNetId(Guid netId) {
        ReSale toReturn = null;

        Type[] reSaleTypes = {
            typeof(ReSale),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Organization),
            typeof(Agreement),
            typeof(User),
            typeof(SaleNumber),
            typeof(ReSaleItem),
            typeof(ReSaleAvailability),
            typeof(OrderItem),
            typeof(ConsignmentItem),
            typeof(Product),
            typeof(ProductAvailability),
            typeof(ProductReservation),
            typeof(MeasureUnit),
            typeof(DepreciatedOrderItem),
            typeof(ProductTransferItem),
            typeof(Currency),
            typeof(Organization),
            typeof(RegionCode),
            typeof(ProductSpecification)
        };

        Func<object[], ReSale> reSaleMapper = objects => {
            ReSale reSale = (ReSale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Client client = (Client)objects[2];
            Organization organization = (Organization)objects[3];
            Agreement agreement = (Agreement)objects[4];
            User user = (User)objects[5];
            SaleNumber saleNumber = (SaleNumber)objects[6];
            ReSaleItem reSaleItem = (ReSaleItem)objects[7];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[8];
            OrderItem orderItem = (OrderItem)objects[9];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[10];
            Product product = (Product)objects[11];
            ProductAvailability productAvailability = (ProductAvailability)objects[12];
            ProductReservation productReservation = (ProductReservation)objects[13];
            MeasureUnit measureUnit = (MeasureUnit)objects[14];
            DepreciatedOrderItem depreciatedOrderItem = (DepreciatedOrderItem)objects[15];
            ProductTransferItem productTransferItem = (ProductTransferItem)objects[16];
            Currency currency = (Currency)objects[17];
            Organization clientOrganization = (Organization)objects[18];
            RegionCode regionCode = (RegionCode)objects[19];
            ProductSpecification productSpecification = (ProductSpecification)objects[20];

            if (toReturn == null) {
                if (clientAgreement != null) {
                    agreement.Currency = currency;
                    client.RegionCode = regionCode;
                    clientAgreement.Client = client;
                    agreement.Organization = clientOrganization;
                    clientAgreement.Agreement = agreement;
                    reSale.ClientAgreement = clientAgreement;
                }

                reSale.Organization = organization;
                reSale.User = user;
                reSale.SaleNumber = saleNumber;

                toReturn = reSale;
            }

            if (reSaleItem == null) return reSale;

            product.MeasureUnit = measureUnit;

            consignmentItem.Product = product;

            consignmentItem.ProductSpecification = productSpecification;
            reSaleAvailability.ConsignmentItem = consignmentItem;
            reSaleAvailability.ProductAvailability = productAvailability;
            reSaleAvailability.ProductReservation = productReservation;
            reSaleAvailability.OrderItem = orderItem;
            reSaleAvailability.DepreciatedOrderItem = depreciatedOrderItem;
            reSaleAvailability.ProductTransferItem = productTransferItem;
            reSaleItem.ReSaleAvailability = reSaleAvailability;

            reSaleItem.PricePerItem = decimal.Round(
                reSaleItem.PricePerItem, 2, MidpointRounding.AwayFromZero);

            reSaleItem.TotalPrice = decimal.Round(
                Convert.ToDecimal(reSaleItem.Qty) * reSaleItem.PricePerItem, 2, MidpointRounding.AwayFromZero);

            toReturn.ReSaleItems.Add(reSaleItem);

            toReturn.TotalQty += reSaleItem.Qty;

            toReturn.TotalPrice = decimal.Round(
                toReturn.TotalPrice + reSaleItem.TotalPrice, 2, MidpointRounding.AwayFromZero);

            return reSale;
        };

        _connection.Query(
            "SELECT " +
            "[ReSale].* " +
            ", [ClientAgreement].* " +
            ", [Client].* " +
            ", [Organization].* " +
            ", [Agreement].* " +
            ", [User].* " +
            ", [SaleNumber].* " +
            ", [ReSaleItem].* " +
            ", [ReSaleAvailability].* " +
            ", [OrderItem].* " +
            ", [ConsignmentItem].* " +
            ", [Product].* " +
            ", [ProductAvailability].* " +
            ", [ProductReservation].* " +
            ", [MeasureUnit].* " +
            ", [DepreciatedOrderItem].* " +
            ", [ProductTransferItem].* " +
            ", [Currency].* " +
            ", [ClientOrganization].* " +
            ", [RegionCode].* " +
            ", [ProductSpecification].* " +
            "FROM [ReSale] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [ReSale].OrganizationID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [ReSale].[UserID] " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].[ID] = [ReSale].[SaleNumberID] " +
            "LEFT JOIN [ReSaleItem] " +
            "ON [ReSaleItem].[ReSaleID] = [ReSale].[ID] " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].[ID] = [ReSaleItem].[ReSaleAvailabilityID] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].[ID] = [ReSaleAvailability].[OrderItemID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ConsignmentItem].[ProductID] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].[ID] = [ReSaleAvailability].[ProductAvailabilityID] " +
            "LEFT JOIN [ProductReservation] " +
            "ON [ProductReservation].[ID] = [ReSaleAvailability].[ProductReservationID] " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [DepreciatedOrderItem] " +
            "ON [DepreciatedOrderItem].[ID] = [ReSaleAvailability].[DepreciatedOrderItemID] " +
            "LEFT JOIN [ProductTransferItem] " +
            "ON [ProductTransferItem].[ID] = [ReSaleAvailability].[ProductTransferItemID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [Organization] AS [ClientOrganization] " +
            "ON [ClientOrganization].[ID] = [Agreement].[OrganizationID] " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].[RegionCodeID] = [RegionCode].[ID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "WHERE ReSale.NetUID = @NetId " +
            "AND ReSale.Deleted = 0 ",
            reSaleTypes,
            reSaleMapper,
            new { NetId = netId });

        return toReturn;
    }

    public IEnumerable<ReSale> GetAll(
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        FilterReSaleStatusOption status) {
        List<ReSale> reSales = new();

        string statusCondition = "";

        switch (status) {
            case FilterReSaleStatusOption.Score:
                statusCondition =
                    "AND [ReSale].[ChangedToInvoice] IS NULL ";
                break;
            case FilterReSaleStatusOption.Invoice:
                statusCondition =
                    "AND [ReSale].[ChangedToInvoice] IS NOT NULL ";
                break;
        }

        Type[] types = {
            typeof(ReSale),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(ReSaleItem),
            typeof(ReSaleAvailability),
            typeof(OrderItem),
            typeof(Product),
            typeof(Organization),
            typeof(Agreement),
            typeof(SaleNumber),
            typeof(Organization),
            typeof(BaseSalePaymentStatus),
            typeof(BaseLifeCycleStatus),
            typeof(Currency)
        };

        Func<object[], ReSale> mapper = objects => {
            ReSale reSale = (ReSale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Client client = (Client)objects[2];
            ReSaleItem reSaleItem = (ReSaleItem)objects[3];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            Product product = (Product)objects[6];
            Organization organization = (Organization)objects[7];
            Agreement agreement = (Agreement)objects[8];
            SaleNumber saleNumber = (SaleNumber)objects[9];
            Organization clientOrganization = (Organization)objects[10];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[11];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[12];
            Currency currency = (Currency)objects[13];

            if (reSales.Any(r => r.Id.Equals(reSale.Id))) {
                reSale = reSales.First(r => r.Id.Equals(reSale.Id));
            } else {
                if (client != null) {
                    clientAgreement.Client = client;

                    agreement.Currency = currency;
                    agreement.Organization = clientOrganization;
                    clientAgreement.Agreement = agreement;
                    reSale.ClientAgreement = clientAgreement;
                }

                reSale.Organization = organization;
                reSale.SaleNumber = saleNumber;
                reSales.Add(reSale);
            }

            reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
            reSale.BaseLifeCycleStatus = baseLifeCycleStatus;

            if (reSaleItem == null) return reSale;

            if (orderItem != null) {
                orderItem.Product = product;
                reSaleAvailability.OrderItem = orderItem;
            }

            reSaleItem.ReSaleAvailability = reSaleAvailability;

            reSale.ReSaleItems.Add(reSaleItem);

            return reSale;
        };

        _connection.Query(
            "DECLARE @UahId bigint = (SELECT TOP 1 [Currency].[ID] " +
            "FROM [Currency] " +
            "WHERE [Currency].[Code] = 'UAH' " +
            "AND [Currency].[Deleted] = 0); " +
            ";WITH [FILTERED_RESALES] AS ( " +
            "SELECT " +
            "ROW_NUMBER() OVER (ORDER BY [ReSale].[ID] DESC) AS RowNumber " +
            ", [ReSale].[ID] " +
            "FROM [ReSale] " +
            "WHERE [ReSale].[Deleted] = 0 " +
            "AND [ReSale].[Created] >= @From " +
            "AND [ReSale].[Created] <= @To " +
            ") " +
            "SELECT " +
            "[ReSale].* " +
            ", ROUND( " +
            "(SELECT " +
            "SUM([ReSaleItem].[PricePerItem] * [ReSaleItem].[Qty]) / " +
            "CASE WHEN [dbo].GetExchangeRateByCurrencyIdAndCode(@UahId, " +
            "CASE WHEN [ClientAgreement].[ID] IS NOT NULL THEN [Currency].[Code] ELSE 'UAH' END, " +
            "CASE WHEN [ReSale].[ChangedToInvoice] IS NOT NULL THEn [ReSale].[ChangedToInvoice] ELSE GETUTCDATE() END) IS NULL THEN 1 ELSE " +
            "[dbo].GetExchangeRateByCurrencyIdAndCode(@UahId, " +
            "CASE WHEN [ClientAgreement].[ID] IS NOT NULL THEN [Currency].[Code] ELSE 'UAH' END, " +
            "CASE WHEN [ReSale].[ChangedToInvoice] IS NOT NULL THEn [ReSale].[ChangedToInvoice] ELSE GETUTCDATE() END) END " +
            "FROM [ReSaleItem] " +
            "WHERE [ReSaleItem].[ReSaleID] = [ReSale].[ID] " +
            "AND [ReSaleItem].[Deleted] = 0) " +
            ",2) AS [TotalAmount] " +
            ", [ClientAgreement].* " +
            ", [Client].* " +
            ", [ReSaleItem].* " +
            ", [ReSaleAvailability].* " +
            ", [OrderItem].* " +
            ", [Product].* " +
            ", [Organization].* " +
            ", [Agreement].* " +
            ", [SaleNumber].* " +
            ", [ClientOrganization].* " +
            ", [BaseSalePaymentStatus].* " +
            ", [BaseLifeCycleStatus].* " +
            ", [Currency].* " +
            "FROM [FILTERED_RESALES] " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].ID = [FILTERED_RESALES].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ReSale].[ClientAgreementID] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [ReSaleItem] " +
            "ON [ReSaleItem].ReSaleID = [ReSale].[ID] " +
            "AND [ReSaleItem].[Deleted] = 0 " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].[ID] = [ReSaleItem].[ReSaleAvailabilityID] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ID = [ReSaleAvailability].[OrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].[ProductID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [ReSale].[OrganizationID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].[ID] = [ReSale].[SaleNumberID] " +
            "LEFT JOIN [Organization] AS [ClientOrganization] " +
            "ON [ClientOrganization].[ID] = [Agreement].[OrganizationID] " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].[ID] = [ReSale].[BaseSalePaymentStatusID] " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].[ID] = [ReSale].[BaseLifeCycleStatusID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [FILTERED_RESALES].[RowNumber] > @Offset " +
            "AND [FILTERED_RESALES].[RowNumber] <= @Limit + @Offset " +
            "AND [ReSale].[Deleted] = 0 " +
            statusCondition,
            types,
            mapper,
            new { From = from, To = to, Limit = limit, Offset = offset }
        );

        return reSales.OrderByDescending(x => x.Created);
    }

    public UpdatedReSaleModel GetProductLocations(UpdatedReSaleModel reSale) {
        IEnumerable<long> orderItemIds =
            reSale.ReSale.ReSaleItems
                .Where(x => x.ReSaleAvailability.OrderItemId.HasValue)
                .Select(x => x.ReSaleAvailability.OrderItemId.Value);

        IEnumerable<long> depreciatedItemsIds =
            reSale.ReSale.ReSaleItems
                .Where(x => x.ReSaleAvailability.DepreciatedOrderItemId.HasValue)
                .Select(x => x.ReSaleAvailability.DepreciatedOrderItemId.Value);

        IEnumerable<long> transferItemIds =
            reSale.ReSale.ReSaleItems
                .Where(x => x.ReSaleAvailability.ProductTransferItemId.HasValue)
                .Select(x => x.ReSaleAvailability.ProductTransferItemId.Value);

        _connection.Query<ProductLocation, ProductPlacement, ProductLocation>(
            "SELECT * " +
            "FROM [ProductLocation] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].ID = [ProductLocation].ProductPlacementID " +
            "WHERE [ProductLocation].Deleted = 0 " +
            "AND ([ProductLocation].[OrderItemID] IN @OrderItemIds " +
            "OR [ProductLocation].[DepreciatedOrderItemID] IN @DepreciatedItemsIds " +
            "OR [ProductLocation].[ProductTransferItemID] IN @TransferItemIds)",
            (location, placement) => {
                location.ProductPlacement = placement;

                ReSaleItem reSaleItem = null;

                if (location.OrderItemId.HasValue) {
                    reSaleItem = reSale.ReSale.ReSaleItems
                        .First(i => i.ReSaleAvailability.OrderItemId.Equals(location.OrderItemId.Value));

                    reSaleItem.ReSaleAvailability.OrderItem.ProductLocations
                        .Add(location);
                } else if (location.DepreciatedOrderItemId.HasValue) {
                    reSaleItem = reSale.ReSale.ReSaleItems
                        .First(i => i.ReSaleAvailability.DepreciatedOrderItemId.Equals(location.DepreciatedOrderItemId.Value));

                    reSaleItem.ReSaleAvailability.DepreciatedOrderItem.ProductLocations
                        .Add(location);
                } else if (location.ProductTransferItemId.HasValue) {
                    reSaleItem = reSale.ReSale.ReSaleItems
                        .First(i => i.ReSaleAvailability.ProductTransferItemId.Equals(location.ProductTransferItemId.Value));

                    reSaleItem.ReSaleAvailability.ProductTransferItem.ProductLocations
                        .Add(location);
                }

                reSaleItem?.ReSaleAvailability.ProductLocations.Add(location);

                return location;
            },
            new {
                OrderItemIds = orderItemIds,
                DepreciatedItemsIds = depreciatedItemsIds,
                TransferItemIds = transferItemIds
            }
        );

        return reSale;
    }

    public UpdatedReSaleModel GetUpdatedByNetId(Guid netId) {
        Type[] resaleTypes = {
            typeof(ReSale),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(SaleNumber),
            typeof(User),
            typeof(User),
            typeof(Client),
            typeof(Currency),
            typeof(VatRate),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(Storage)
        };

        Func<object[], ReSale> mapperResale = objects => {
            ReSale reSale = (ReSale)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Organization organization = (Organization)objects[3];
            SaleNumber saleNumber = (SaleNumber)objects[4];
            User responsible = (User)objects[5];
            User changedToInvoiceBy = (User)objects[6];
            Client client = (Client)objects[7];
            Currency сurrency = (Currency)objects[8];
            VatRate vatRate = (VatRate)objects[9];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[10];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[11];
            Storage storage = (Storage)objects[12];

            if (clientAgreement != null) {
                clientAgreement.Client = client;
                agreement.Currency = сurrency;
                clientAgreement.Agreement = agreement;
            }

            reSale.ClientAgreement = clientAgreement;
            organization.VatRate = vatRate;
            reSale.Organization = organization;
            reSale.SaleNumber = saleNumber;
            reSale.User = responsible;
            reSale.ChangedToInvoiceBy = changedToInvoiceBy;
            reSale.FromStorage = storage;
            reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
            reSale.BaseSalePaymentStatus = baseSalePaymentStatus;

            return reSale;
        };

        UpdatedReSaleModel toReturn = new() {
            ReSale = _connection.Query(
                "SELECT * FROM [ReSale] " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].[ID] = [ReSale].[ClientAgreementID] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].[ID] = [ReSale].[OrganizationID] " +
                "LEFT JOIN [saleNumber] " +
                "ON [SaleNumber].[ID] = [ReSale].[SaleNumberID] " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].[ID] = [ReSale].[UserID] " +
                "LEFT JOIN [User] AS [ChangedToInvoiceBy] " +
                "ON [ChangedToInvoiceBy].[ID] = [ReSale].[ChangedToInvoiceByID] " +
                "LEFT JOIN [Client] " +
                "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                "LEFT JOIN [VatRate] " +
                "ON [VatRate].[ID] = [Organization].[VatRateID] " +
                "LEFT JOIN [BaseLifeCycleStatus] " +
                "ON [BaseLifeCycleStatus].[ID] = [ReSale].[BaseLifeCycleStatusID] " +
                "LEFT JOIN [BaseSalePaymentStatus] " +
                "ON [BaseSalePaymentStatus].[ID] = [ReSale].[BaseSalePaymentStatusID] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].[ID] = [ReSale].[FromStorageID] " +
                "WHERE [ReSale].[NetUID] = @NetId; ",
                resaleTypes,
                mapperResale,
                new { NetId = netId }).FirstOrDefault()
        };

        if (toReturn.ReSale == null)
            return toReturn;

        Type[] types = {
            typeof(ReSaleItem),
            typeof(ReSaleAvailability),
            typeof(ProductAvailability),
            typeof(ConsignmentItem),
            typeof(Product),
            typeof(Storage),
            typeof(MeasureUnit),
            typeof(ProductSpecification),
            typeof(OrderItem),
            typeof(DepreciatedOrderItem),
            typeof(ProductTransferItem),
            typeof(double?)
        };

        Func<object[], ReSaleItem> mapper = objects => {
            ReSaleItem item = (ReSaleItem)objects[0];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[1];
            ProductAvailability productAvailability = (ProductAvailability)objects[2];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[3];
            Product product = (Product)objects[4];
            Storage storage = (Storage)objects[5];
            MeasureUnit measureUnit = (MeasureUnit)objects[6];
            ProductSpecification productSpecification = (ProductSpecification)objects[7];
            OrderItem orderItem = (OrderItem)objects[8];
            DepreciatedOrderItem depreciatedOrderItem = (DepreciatedOrderItem)objects[9];
            ProductTransferItem productTransferItem = (ProductTransferItem)objects[10];
            double? remainingQty = (double?)objects[11];

            if (!toReturn.ReSale.ReSaleItems.Any(x => x.Id.Equals(item.Id)))
                toReturn.ReSale.ReSaleItems.Add(item);

            if (reSaleAvailability != null) {
                consignmentItem.ProductSpecification = productSpecification;

                reSaleAvailability.ConsignmentItem = consignmentItem;
                productAvailability.Storage = storage;
                reSaleAvailability.ProductAvailability = productAvailability;
                product.MeasureUnit = measureUnit;
                consignmentItem.Product = product;
                reSaleAvailability.Product = product;

                reSaleAvailability.OrderItem = orderItem;
                reSaleAvailability.DepreciatedOrderItem = depreciatedOrderItem;
                reSaleAvailability.ProductTransferItem = productTransferItem;
            }

            product.MeasureUnit = measureUnit;
            item.Product = product;
            item.RemainingQty = remainingQty ?? 0;

            item.ReSaleAvailability = reSaleAvailability;

            return item;
        };

        _connection.Query(
            "SELECT " +
            "[ReSaleItem].* " +
            ", [ReSaleAvailability].* " +
            ", [ProductAvailability].* " +
            ", [ConsignmentItem].* " +
            ", [Product].* " +
            ", [Storage].* " +
            ", [MeasureUnit].* " +
            ", [ProductSpecification].* " +
            ", [OrderItem].* " +
            ", [DepreciatedOrderItem].* " +
            ", [ProductTransferItem].* " +
            ",CASE WHEN [ReSaleItem].[ReSaleAvailabilityID] IS NULL THEN " +
            "(SELECT SUM([ReSaleAvailability].[RemainingQty]) - " +
            "(CASE WHEN (SELECT SUM([ReSaleItem].[Qty]) FROM [ReSaleItem] " +
            "WHERE [ReSaleItem].[Deleted] = 0 " +
            "AND [ReSaleItem].[ID] != [ReSaleItem].[ID] " +
            "AND [ReSaleItem].[ProductID] = [ReSaleItem].[ProductID]) IS NULL THEN 0 " +
            "ELSE (SELECT SUM([ReSaleItem].[Qty]) FROM [ReSaleItem] " +
            "WHERE [ReSaleItem].[Deleted] = 0 " +
            "AND [ReSaleItem].[ID] != [ReSaleItem].[ID] " +
            "AND [ReSaleItem].[ProductID] = [ReSaleItem].[ProductID]) " +
            "END) " +
            "FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "WHERE [ConsignmentItem].[ProductID] = [ReSaleItem].[ProductID]) " +
            " ELSE " +
            "(SELECT SUM([ReSaleAvailability].[RemainingQty]) " +
            "FROM [ReSaleAvailability] " +
            "WHERE [ReSaleAvailability].[ConsignmentItemID] = [ConsignmentItem].[ID]) " +
            "END AS [RemainingQty] " +
            "FROM [ReSaleItem] " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].[ID] = " +
            "( " +
            "CASE " +
            "WHEN [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
            "THEN [ReSaleItem].[ReSaleAvailabilityID] " +
            "ELSE ( " +
            "SELECT TOP 1 [ReSaleAvailability].[ID] FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "WHERE [ConsignmentItem].[ProductID] = [ReSaleItem].[ProductID] " +
            "AND [Consignment].[StorageID] = @FromStorageID " +
            "AND [ReSaleAvailability].[Deleted] = 0 " +
            "AND [ReSaleAvailability].[RemainingQty] > 0 " +
            "ORDER BY [ReSaleAvailability].[Created] " +
            ") " +
            "END " +
            ") " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].[ID] = [ReSaleAvailability].[ProductAvailabilityID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ReSaleItem].[ProductID] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [ConsignmentItem].[ProductSpecificationID] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].[ID] = [ReSaleAvailability].[OrderItemID] " +
            "LEFT JOIN [DepreciatedOrderItem] " +
            "ON [DepreciatedOrderItem].[ID] = [ReSaleAvailability].[DepreciatedOrderItemID] " +
            "LEFT JOIN [ProductTransferItem] " +
            "ON [ProductTransferItem].[ID] = [ReSaleAvailability].[ProductTransferItemID] " +
            "WHERE [ReSaleItem].[Deleted] = 0 " +
            "AND [ReSaleItem].[ReSaleID] = @Id; ",
            types, mapper,
            new { toReturn.ReSale.Id, toReturn.ReSale.FromStorageId }, splitOn: "ID,RemainingQty");

        return toReturn;
    }

    public ReSale GetByNetIdWithItemsInfo(Guid netId) {
        ReSale toReturn = _connection.Query<ReSale, BaseLifeCycleStatus, BaseSalePaymentStatus, ClientAgreement, Agreement, Currency, ReSale>(
            "SELECT * FROM [ReSale] " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [BaseLifeCycleStatus].[ID] = [ReSale].[BaseLifeCycleStatusID] " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON [BaseSalePaymentStatus].[ID] = [ReSale].[BaseSalePaymentStatusID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [ReSale].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ReSale].[NetUID] = @NetId; ",
            (reSale, baseLifeCycleStatus, baseSalePaymentStatus, clientAgreement, agreement, currency) => {
                reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                reSale.BaseSalePaymentStatus = baseSalePaymentStatus;

                if (clientAgreement != null) {
                    agreement.Currency = currency;
                    clientAgreement.Agreement = agreement;
                    reSale.ClientAgreement = clientAgreement;
                }

                return reSale;
            },
            new { NetId = netId }).FirstOrDefault();

        if (toReturn == null) return null;

        Type[] types = {
            typeof(ReSaleItem),
            typeof(ReSaleAvailability)
        };

        Func<object[], ReSaleItem> mapper = objects => {
            ReSaleItem item = (ReSaleItem)objects[0];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[1];

            if (!toReturn.ReSaleItems.Any(x => x.Id.Equals(item.Id)))
                toReturn.ReSaleItems.Add(item);

            item.ReSaleAvailability = reSaleAvailability;

            return item;
        };

        _connection.Query(
            "SELECT * FROM [ReSaleItem] " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].[ID] = [ReSaleItem].[ReSaleAvailabilityID] " +
            "WHERE [ReSaleItem].[Deleted] = 0 " +
            "AND [ReSaleItem].[ReSaleID] = @Id; ",
            types, mapper,
            new { toReturn.Id });

        return toReturn;
    }


    public void UpdateChangeToInvoice(ReSale reSale) {
        _connection.Execute(
            "UPDATE [ReSale] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [ChangedToInvoice] = @ChangedToInvoice " +
            ", [ChangedToInvoiceById] = @ChangedToInvoiceById " +
            ", [TotalPaymentAmount] = @TotalPaymentAmount " +
            "WHERE [ReSale].[ID] = @Id; ",
            reSale);
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [ReSale] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [Deleted] = 1 " +
            "WHERE [ReSale].[ID] = @Id; ",
            new { Id = id });
    }

    public IEnumerable<ReSale> GetLastPaidReSalesByClientAgreementId(long clientAgreementId, DateTime created) {
        List<ReSale> sales = new();

        string sqlExpression =
            "SELECT ReSale.* " +
            ",BaseLifeCycleStatus.* " +
            ",BaseSalePaymentStatus.* " +
            ",SaleNumber.* " +
            ",SaleUser.* " +
            ",ClientAgreement.* " +
            ",Agreement.* " +
            ",AgreementPricing.ID " +
            ",AgreementPricing.BasePricingID " +
            ",AgreementPricing.Comment " +
            ",AgreementPricing.Created " +
            ",AgreementPricing.Culture " +
            ",AgreementPricing.CurrencyID " +
            ",AgreementPricing.Deleted " +
            ",AgreementPricing.Deleted " +
            ",dbo.GetPricingExtraCharge(AgreementPricing.NetUID) AS ExtraCharge " +
            ",AgreementPricing.Name " +
            ",AgreementPricing.NetUID " +
            ",AgreementPricing.PriceTypeID " +
            ",AgreementPricing.Updated " +
            ",Currency.* " +
            ",CurrencyTranslation.* " +
            ",ExchangeRate.* " +
            ",Client.* " +
            ",RegionCode.* " +
            ",ReSaleItem.* " +
            ",ReSaleAvailability.* " +
            ",ConsignmentItem.* " +
            ",[Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].[Image] " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].[Weight] " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductGroupDiscount.* " +
            "FROM ReSale " +
            "LEFT JOIN BaseLifeCycleStatus " +
            "ON BaseLifeCycleStatus.ID = ReSale.BaseLifeCycleStatusID " +
            "LEFT JOIN BaseSalePaymentStatus " +
            "ON BaseSalePaymentStatus.ID = ReSale.BaseSalePaymentStatusID " +
            "LEFT JOIN SaleNumber " +
            "ON SaleNumber.ID = ReSale.SaleNumberID " +
            "LEFT JOIN [User] AS SaleUser " +
            "ON SaleUser.ID = ReSale.UserID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ID = ReSale.ClientAgreementID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS AgreementPricing " +
            "ON AgreementPricing.ID = Agreement.PricingID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "LEFT JOIN ExchangeRate " +
            "ON ExchangeRate.CurrencyID = Currency.ID " +
            "AND ExchangeRate.Culture = @Culture " +
            "AND ExchangeRate.Code = 'EUR' " +
            "LEFT JOIN Client " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeId = RegionCode.ID " +
            "LEFT JOIN [ReSaleItem] " +
            "ON [ReSaleItem].[ID] = [ReSale].ID " +
            "AND ReSaleItem.Deleted = 0 " +
            "AND ReSaleItem.Qty > 0 " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].[ID] = [ReSaleItem].[ReSaleAvailabilityID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN Product " +
            "ON Product.ID = ConsignmentItem.ProductID " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ProductGroupID = ProductProductGroup.ProductGroupID " +
            "AND ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "WHERE ReSale.Deleted = 0 " +
            "AND ReSale.ClientAgreementId = @ClientAgreementId " +
            "AND ReSale.Created >= @FromDate " +
            "AND BaseLifeCycleStatus.SaleLifeCycleType > 0 " +
            "AND BaseSalePaymentStatus.SalePaymentStatusType > 0 " +
            "ORDER BY ReSale.Created DESC";

        Type[] types = {
            typeof(ReSale),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus),
            typeof(SaleNumber),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ExchangeRate),
            typeof(Client),
            typeof(RegionCode),
            typeof(ReSaleItem),
            typeof(ReSaleAvailability),
            typeof(ConsignmentItem),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductGroupDiscount)
        };

        Func<object[], ReSale> mapper = objects => {
            ReSale resale = (ReSale)objects[0];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[1];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[2];
            SaleNumber saleNumber = (SaleNumber)objects[3];
            User saleUser = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Currency currency = (Currency)objects[8];
            CurrencyTranslation currencyTranslation = (CurrencyTranslation)objects[9];
            ExchangeRate exchangeRate = (ExchangeRate)objects[10];
            ReSaleItem reSaleItem = (ReSaleItem)objects[14];
            ReSaleAvailability reSaleAvailability = (ReSaleAvailability)objects[15];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[16];
            Product product = (Product)objects[17];
            ProductPricing productPricing = (ProductPricing)objects[18];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[19];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[20];

            if (sales.Any(s => s.Id.Equals(resale.Id))) {
                ReSale saleFromList = sales.First(c => c.Id.Equals(resale.Id));

                if (productGroupDiscount != null && !saleFromList.ClientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                    saleFromList.ClientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                if (reSaleItem == null) return resale;

                if (!saleFromList.ReSaleItems.Any(i => i.Id.Equals(reSaleItem.Id))) {
                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    reSaleAvailability.ConsignmentItem = consignmentItem;
                    reSaleItem.ReSaleAvailability = reSaleAvailability;
                    consignmentItem.Product = product;

                    saleFromList.ReSaleItems.Add(reSaleItem);
                }
            } else {
                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (currency != null) {
                    if (exchangeRate != null) currency.ExchangeRates.Add(exchangeRate);

                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;
                }

                if (productGroupDiscount != null) clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);

                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;

                if (reSaleItem != null) {
                    reSaleAvailability.ConsignmentItem = consignmentItem;
                    consignmentItem.Product = product;
                    reSaleItem.ReSaleAvailability = reSaleAvailability;

                    resale.ReSaleItems.Add(reSaleItem);
                }

                resale.ClientAgreement = clientAgreement;
                resale.BaseLifeCycleStatus = baseLifeCycleStatus;
                resale.BaseSalePaymentStatus = baseSalePaymentStatus;
                resale.SaleNumber = saleNumber;
                resale.UserFullName = $"{saleUser?.FirstName} {saleUser?.MiddleName} {saleUser?.LastName}";
                resale.User = saleUser;

                sales.Add(resale);
            }

            return resale;
        };

        var props = new {
            ClientAgreementId = clientAgreementId,
            FromDate = created,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        _connection.Query(sqlExpression, types, mapper, props);

        return sales.Where(s => s.ReSaleItems.Any());
    }

    public ReSale GetByNetIdWithoutInfo(
        Guid netId) {
        return _connection.Query<ReSale>(
            "SELECT * FROM [ReSale] " +
            "WHERE [ReSale].[NetUID] = @NetId; ",
            new { NetId = netId }).FirstOrDefault();
    }

    public void ChangeIsCompleted(
        Guid netId,
        bool isCompleted) {
        _connection.Execute(
            "UPDATE [ReSale] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [IsCompleted] = @IsCompleted " +
            "WHERE [ReSale].[NetUID] = @NetId; ",
            new {
                NetId = netId,
                IsCompleted = isCompleted
            });
    }

    public ConsignmentItem GetConsignmentItemByProductId(long productId) {
        return _connection.Query<ConsignmentItem>(
            "SELECT TOP 1 [ConsignmentItem].* FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "WHERE [ConsignmentItem].[ProductID] = @ProductId " +
            "ORDER BY [ConsignmentItem].[Created] ",
            new { ProductId = productId }).FirstOrDefault();
    }

    public ConsignmentItem GetConsignmentItemByProductAndStorageId(
        long productId,
        long storageId) {
        return _connection.Query<ConsignmentItem>(
            "SELECT TOP 1 [ConsignmentItem].* FROM [ReSaleAvailability] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [ReSaleAvailability].[ConsignmentItemID] " +
            "LEFT JOIN [Consignment] " +
            "ON [Consignment].[ID] = [ConsignmentItem].[ConsignmentID] " +
            "WHERE [ConsignmentItem].[ProductID] = @ProductId " +
            "AND [Consignment].[StorageID] = @StorageId " +
            "ORDER BY [ConsignmentItem].[Created] ",
            new { ProductId = productId, StorageId = storageId }).FirstOrDefault();
    }
}