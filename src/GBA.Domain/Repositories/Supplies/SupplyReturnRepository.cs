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
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Supplies;

public sealed class SupplyReturnRepository : ISupplyReturnRepository {
    private readonly IDbConnection _connection;

    public SupplyReturnRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyReturn supplyReturn) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyReturn] (Number, Comment, FromDate, SupplierId, ClientAgreementId, OrganizationId, ResponsibleId, StorageId, IsManagement, Updated) " +
            "VALUES (@Number, @Comment, @FromDate, @SupplierId, @ClientAgreementId, @OrganizationId, @ResponsibleId, @StorageId, @IsManagement, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            supplyReturn
        ).Single();
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [SupplyReturn] " +
            "SET Deleted = 1, UPDATED = GETUTCDATE() " +
            "WHERE [SupplyReturn].ID = @Id",
            new { Id = id }
        );
    }

    public SupplyReturn GetLastRecord(long organizationId) {
        return _connection.Query<SupplyReturn>(
            "SELECT TOP(1) * " +
            "FROM [SupplyReturn] " +
            "WHERE [SupplyReturn].Deleted = 0 " +
            "AND [SupplyReturn].OrganizationID = @OrganizationId " +
            "ORDER BY ID DESC",
            new { OrganizationId = organizationId }
        ).SingleOrDefault();
    }

    public SupplyReturn GetLastRecord(string culture) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [SupplyReturn] " +
            "WHERE [SupplyReturn].Deleted = 0 ";

        sqlExpression +=
            culture.ToLower().Equals("pl")
                ? "AND [SupplyReturn].Number like 'P%' "
                : "AND [SupplyReturn].Number NOT like 'P%' ";

        sqlExpression += "ORDER BY [SupplyReturn].ID DESC";

        return _connection.Query<SupplyReturn>(
            sqlExpression
        ).SingleOrDefault();
    }

    public SupplyReturn GetById(long id) {
        Type[] types = {
            typeof(SupplyReturn),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], SupplyReturn> mapper = objects => {
            SupplyReturn supplyReturn = (SupplyReturn)objects[0];
            Client supplier = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Organization agreementOrganization = (Organization)objects[4];
            ProviderPricing providerPricing = (ProviderPricing)objects[5];
            Currency currency = (Currency)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Organization organization = (Organization)objects[8];
            User responsible = (User)objects[9];
            Storage storage = (Storage)objects[10];

            if (providerPricing != null) {
                providerPricing.Pricing = pricing;
                providerPricing.Currency = currency;
            }

            agreement.Organization = agreementOrganization;
            agreement.ProviderPricing = providerPricing;

            clientAgreement.Agreement = agreement;

            supplyReturn.ClientAgreement = clientAgreement;
            supplyReturn.Supplier = supplier;
            supplyReturn.Storage = storage;
            supplyReturn.Organization = organization;
            supplyReturn.Responsible = responsible;

            return supplyReturn;
        };

        SupplyReturn toReturn =
            _connection.Query(
                "SELECT * " +
                "FROM [SupplyReturn] " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyReturn].SupplierID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyReturn].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [ProviderPricing].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [views].[PricingView] AS [Pricing] " +
                "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
                "AND [Pricing].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyReturn].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyReturn].ResponsibleID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [SupplyReturn].StorageID " +
                "WHERE [SupplyReturn].ID = @Id",
                types,
                mapper,
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).SingleOrDefault();

        if (toReturn != null)
            toReturn.SupplyReturnItems =
                _connection.Query<SupplyReturnItem, Product, ConsignmentItem, SupplyReturnItem>(
                    "SELECT * " +
                    "FROM [SupplyReturnItem] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].ID = [SupplyReturnItem].ProductID " +
                    "LEFT JOIN [ConsignmentItem] " +
                    "ON [ConsignmentItem].ID = [SupplyReturnItem].ConsignmentItemID " +
                    "WHERE [SupplyReturnItem].SupplyReturnID = @Id",
                    (item, product, consignmentItem) => {
                        item.Product = product;
                        item.ConsignmentItem = consignmentItem;

                        return item;
                    },
                    new { toReturn.Id }
                ).ToList();

        return toReturn;
    }

    public SupplyReturn GetByIdForConsignment(long id) {
        SupplyReturn toReturn = null;

        Type[] types = {
            typeof(SupplyReturn),
            typeof(Organization),
            typeof(Storage),
            typeof(Client),
            typeof(SupplyReturnItem),
            typeof(ConsignmentItem),
            typeof(Product)
        };

        Func<object[], SupplyReturn> mapper = objects => {
            SupplyReturn supplyReturn = (SupplyReturn)objects[0];
            Organization organization = (Organization)objects[1];
            Storage storage = (Storage)objects[2];
            Client supplier = (Client)objects[3];
            SupplyReturnItem supplyReturnItem = (SupplyReturnItem)objects[4];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[5];
            Product product = (Product)objects[6];

            if (toReturn == null) {
                supplyReturn.Organization = organization;
                supplyReturn.Storage = storage;
                supplyReturn.Supplier = supplier;

                toReturn = supplyReturn;
            }

            if (supplyReturnItem == null) return supplyReturn;

            supplyReturnItem.ConsignmentItem = consignmentItem;
            supplyReturnItem.Product = product;

            toReturn.SupplyReturnItems.Add(supplyReturnItem);

            return supplyReturn;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [SupplyReturn] " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [SupplyReturn].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [SupplyReturn].StorageID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyReturn].SupplierID " +
            "LEFT JOIN [SupplyReturnItem] " +
            "ON [SupplyReturnItem].SupplyReturnID = [SupplyReturn].ID " +
            "AND [SupplyReturnItem].Deleted = 0 " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [SupplyReturnItem].ConsignmentItemID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyReturnItem].ProductID " +
            "WHERE [SupplyReturn].ID = @Id",
            types,
            mapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public SupplyReturn GetByNetId(Guid netId) {
        Type[] types = {
            typeof(SupplyReturn),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], SupplyReturn> mapper = objects => {
            SupplyReturn supplyReturn = (SupplyReturn)objects[0];
            Client supplier = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Organization agreementOrganization = (Organization)objects[4];
            ProviderPricing providerPricing = (ProviderPricing)objects[5];
            Currency currency = (Currency)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Organization organization = (Organization)objects[8];
            User responsible = (User)objects[9];
            Storage storage = (Storage)objects[10];

            if (providerPricing != null) {
                providerPricing.Pricing = pricing;
                providerPricing.Currency = currency;
            }

            agreement.Organization = agreementOrganization;
            agreement.ProviderPricing = providerPricing;

            clientAgreement.Agreement = agreement;

            supplyReturn.ClientAgreement = clientAgreement;
            supplyReturn.Supplier = supplier;
            supplyReturn.Storage = storage;
            supplyReturn.Organization = organization;
            supplyReturn.Responsible = responsible;

            return supplyReturn;
        };

        SupplyReturn toReturn =
            _connection.Query(
                "SELECT * " +
                "FROM [SupplyReturn] " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyReturn].SupplierID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyReturn].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [ProviderPricing].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [views].[PricingView] AS [Pricing] " +
                "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
                "AND [Pricing].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyReturn].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyReturn].ResponsibleID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [SupplyReturn].StorageID " +
                "WHERE [SupplyReturn].NetUID = @NetId",
                types,
                mapper,
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).SingleOrDefault();

        if (toReturn != null)
            toReturn.SupplyReturnItems =
                _connection.Query<SupplyReturnItem, Product, ConsignmentItem, SupplyReturnItem>(
                    "SELECT * " +
                    "FROM [SupplyReturnItem] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].ID = [SupplyReturnItem].ProductID " +
                    "LEFT JOIN [ConsignmentItem] " +
                    "ON [ConsignmentItem].ID = [SupplyReturnItem].ConsignmentItemID " +
                    "WHERE [SupplyReturnItem].SupplyReturnID = @Id",
                    (item, product, consignmentItem) => {
                        item.Product = product;
                        item.ConsignmentItem = consignmentItem;

                        return item;
                    },
                    new { toReturn.Id }
                ).ToList();

        return toReturn;
    }

    public List<SupplyReturn> GetAll() {
        Type[] types = {
            typeof(SupplyReturn),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], SupplyReturn> mapper = objects => {
            SupplyReturn supplyReturn = (SupplyReturn)objects[0];
            Client supplier = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Organization agreementOrganization = (Organization)objects[4];
            ProviderPricing providerPricing = (ProviderPricing)objects[5];
            Currency currency = (Currency)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Organization organization = (Organization)objects[8];
            User responsible = (User)objects[9];
            Storage storage = (Storage)objects[10];

            if (providerPricing != null) {
                providerPricing.Pricing = pricing;
                providerPricing.Currency = currency;
            }

            agreement.Organization = agreementOrganization;
            agreement.ProviderPricing = providerPricing;

            clientAgreement.Agreement = agreement;

            supplyReturn.ClientAgreement = clientAgreement;
            supplyReturn.Supplier = supplier;
            supplyReturn.Storage = storage;
            supplyReturn.Organization = organization;
            supplyReturn.Responsible = responsible;

            return supplyReturn;
        };

        List<SupplyReturn> returns =
            _connection.Query(
                "SELECT * " +
                "FROM [SupplyReturn] " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyReturn].SupplierID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyReturn].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [ProviderPricing].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [views].[PricingView] AS [Pricing] " +
                "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
                "AND [Pricing].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyReturn].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyReturn].ResponsibleID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [SupplyReturn].StorageID " +
                "WHERE [SupplyReturn].Deleted = 0 " +
                "ORDER BY [SupplyReturn].FromDate DESC, [SupplyReturn].Number DESC",
                types,
                mapper,
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).ToList();

        return returns;
    }

    public List<SupplyReturn> GetAllFiltered(DateTime from, DateTime to, long limit, long offset) {
        Type[] types = {
            typeof(SupplyReturn),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], SupplyReturn> mapper = objects => {
            SupplyReturn supplyReturn = (SupplyReturn)objects[0];
            Client supplier = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Organization agreementOrganization = (Organization)objects[4];
            ProviderPricing providerPricing = (ProviderPricing)objects[5];
            Currency currency = (Currency)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Organization organization = (Organization)objects[8];
            User responsible = (User)objects[9];
            Storage storage = (Storage)objects[10];

            if (providerPricing != null) {
                providerPricing.Pricing = pricing;
                providerPricing.Currency = currency;
            }

            agreement.Organization = agreementOrganization;
            agreement.ProviderPricing = providerPricing;

            clientAgreement.Agreement = agreement;

            supplyReturn.ClientAgreement = clientAgreement;
            supplyReturn.Supplier = supplier;
            supplyReturn.Storage = storage;
            supplyReturn.Organization = organization;
            supplyReturn.Responsible = responsible;

            return supplyReturn;
        };

        List<SupplyReturn> returns =
            _connection.Query(
                ";WITH [Search_CTE] " +
                "AS (" +
                "SELECT [SupplyReturn].ID " +
                ", [SupplyReturn].FromDate " +
                "FROM [SupplyReturn] " +
                "WHERE [SupplyReturn].Deleted = 0 " +
                "AND [SupplyReturn].FromDate >= @From " +
                "AND [SupplyReturn].FromDate <= @To" +
                "), " +
                "[Rowed_CTE] " +
                "AS (" +
                "SELECT [Search_CTE].ID " +
                ", ROW_NUMBER() OVER(ORDER BY [Search_CTE].FromDate DESC) AS [RowNumber] " +
                "FROM [Search_CTE]" +
                ")" +
                "SELECT * " +
                "FROM [SupplyReturn] " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyReturn].SupplierID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyReturn].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [ProviderPricing].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [views].[PricingView] AS [Pricing] " +
                "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
                "AND [Pricing].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyReturn].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyReturn].ResponsibleID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [SupplyReturn].StorageID " +
                "WHERE [SupplyReturn].ID IN (" +
                "SELECT [Rowed_CTE].ID " +
                "FROM [Rowed_CTE] " +
                "WHERE [Rowed_CTE].RowNumber > @Offset " +
                "AND [Rowed_CTE].RowNumber <= @Limit + @Offset" +
                ") " +
                "ORDER BY [SupplyReturn].FromDate DESC, [SupplyReturn].Number DESC",
                types,
                mapper,
                new {
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    From = from,
                    To = to,
                    Limit = limit,
                    Offset = offset
                }
            ).ToList();

        if (!returns.Any()) return returns;

        _connection.Query<SupplyReturnItem, Product, ConsignmentItem, SupplyReturnItem>(
            "SELECT * " +
            "FROM [SupplyReturnItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyReturnItem].ProductID " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].ID = [SupplyReturnItem].ConsignmentItemID " +
            "WHERE [SupplyReturnItem].SupplyReturnID IN @Ids",
            (item, product, consignmentItem) => {
                item.Product = product;
                item.ConsignmentItem = consignmentItem;

                item.TotalNetPrice = decimal.Round(Convert.ToDecimal(item.Qty) * consignmentItem.Price, 2, MidpointRounding.AwayFromZero);
                item.TotalNetWeight = Math.Round(item.Qty * consignmentItem.Weight, 2, MidpointRounding.AwayFromZero);

                SupplyReturn supplyReturn = returns.First(r => r.Id.Equals(item.SupplyReturnId));

                supplyReturn.SupplyReturnItems.Add(item);

                supplyReturn.TotalNetPrice =
                    decimal.Round(supplyReturn.TotalNetPrice + item.TotalNetPrice, 2, MidpointRounding.AwayFromZero);
                supplyReturn.TotalNetWeight =
                    Math.Round(supplyReturn.TotalNetWeight + item.TotalNetWeight, 2, MidpointRounding.AwayFromZero);

                return item;
            },
            new { Ids = returns.Select(r => r.Id) }
        );

        return returns;
    }

    public SupplyReturn GetByNetIdForPrintingDocument(Guid netId) {
        Type[] types = {
            typeof(SupplyReturn),
            typeof(Client),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(ProviderPricing),
            typeof(Currency),
            typeof(Pricing),
            typeof(Organization),
            typeof(User),
            typeof(Storage)
        };

        Func<object[], SupplyReturn> mapper = objects => {
            SupplyReturn supplyReturn = (SupplyReturn)objects[0];
            Client supplier = (Client)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Organization agreementOrganization = (Organization)objects[4];
            ProviderPricing providerPricing = (ProviderPricing)objects[5];
            Currency currency = (Currency)objects[6];
            Pricing pricing = (Pricing)objects[7];
            Organization organization = (Organization)objects[8];
            User responsible = (User)objects[9];
            Storage storage = (Storage)objects[10];

            if (providerPricing != null) {
                providerPricing.Pricing = pricing;
                providerPricing.Currency = currency;
            }

            agreement.Organization = agreementOrganization;
            agreement.ProviderPricing = providerPricing;

            clientAgreement.Agreement = agreement;

            supplyReturn.ClientAgreement = clientAgreement;
            supplyReturn.Supplier = supplier;
            supplyReturn.Storage = storage;
            supplyReturn.Organization = organization;
            supplyReturn.Responsible = responsible;

            return supplyReturn;
        };

        SupplyReturn toReturn =
            _connection.Query(
                "SELECT * " +
                "FROM [SupplyReturn] " +
                "LEFT JOIN [Client] AS [Supplier] " +
                "ON [Supplier].ID = [SupplyReturn].SupplierID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [SupplyReturn].ClientAgreementID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [views].[OrganizationView] AS [AgreementOrganization] " +
                "ON [AgreementOrganization].ID = [Agreement].OrganizationID " +
                "AND [AgreementOrganization].CultureCode = @Culture " +
                "LEFT JOIN [ProviderPricing] " +
                "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
                "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
                "ON [Currency].ID = [ProviderPricing].CurrencyID " +
                "AND [Currency].CultureCode = @Culture " +
                "LEFT JOIN [views].[PricingView] AS [Pricing] " +
                "ON [Pricing].ID = [ProviderPricing].BasePricingID " +
                "AND [Pricing].CultureCode = @Culture " +
                "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
                "ON [Organization].ID = [SupplyReturn].OrganizationID " +
                "AND [Organization].CultureCode = @Culture " +
                "LEFT JOIN [User] AS [Responsible] " +
                "ON [Responsible].ID = [SupplyReturn].ResponsibleID " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [SupplyReturn].StorageID " +
                "WHERE [SupplyReturn].NetUID = @NetId",
                types,
                mapper,
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            ).SingleOrDefault();

        if (toReturn == null) return null;

        List<SupplyReturnItem> supplyReturnItems = new();

        Type[] supplyReturnItemTypes = {
            typeof(SupplyReturnItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(MeasureUnitTranslation),
            typeof(ConsignmentItem)
        };

        Func<object[], SupplyReturnItem> supplyReturnItemMapper = objects => {
            SupplyReturnItem supplyReturnItem = (SupplyReturnItem)objects[0];
            Product product = (Product)objects[1];
            MeasureUnit measureUnit = (MeasureUnit)objects[2];
            MeasureUnitTranslation measureUnitTranslation = (MeasureUnitTranslation)objects[3];
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[4];

            if (supplyReturnItems.Any(x => x.Id == supplyReturnItem.Id)) {
                supplyReturnItem = supplyReturnItems.First(x => x.Id == supplyReturnItem.Id);
            } else {
                supplyReturnItem.ConsignmentItem = consignmentItem;

                supplyReturnItem.TotalNetPrice = Convert.ToDecimal(supplyReturnItem.Qty) * consignmentItem.Price;

                toReturn.TotalNetPrice += supplyReturnItem.TotalNetPrice;

                supplyReturnItems.Add(supplyReturnItem);
            }

            supplyReturnItem.Product = product;

            product.MeasureUnit = measureUnit;

            measureUnit.MeasureUnitTranslations.Add(measureUnitTranslation);

            return supplyReturnItem;
        };

        string sqlQuery =
            "SELECT * " +
            "FROM [SupplyReturnItem] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyReturnItem].ProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [MeasureUnitTranslation] " +
            "ON  [MeasureUnitTranslation].[MeasureUnitID] = [Product].[MeasureUnitID] " +
            "LEFT JOIN [ConsignmentItem] " +
            "ON [ConsignmentItem].[ID] = [SupplyReturnItem].[ConsignmentItemID] " +
            "WHERE [SupplyReturnItem].[SupplyReturnID] = @Id";

        _connection.Query(
            sqlQuery,
            supplyReturnItemTypes,
            supplyReturnItemMapper,
            new { toReturn.Id }
        );

        toReturn.SupplyReturnItems = supplyReturnItems;

        return toReturn;
    }
}