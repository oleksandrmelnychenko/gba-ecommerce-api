using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientAgreementRepository : IClientAgreementRepository {
    private readonly IDbConnection _connection;

    public ClientAgreementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientAgreement clientAgreement) {
        return _connection.Query<long>(
            "INSERT INTO ClientAgreement (ProductReservationTerm, ClientId, AgreementId, Updated) " +
            "VALUES (@ProductReservationTerm, @ClientId, @AgreementId, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            clientAgreement
        ).Single();
    }

    public void Add(IEnumerable<ClientAgreement> clientAgreements) {
        _connection.Execute(
            "INSERT INTO ClientAgreement (ProductReservationTerm, ClientId, AgreementId, Updated) " +
            "VALUES (@ProductReservationTerm, @ClientId, @AgreementId, getutcdate())",
            clientAgreements
        );
    }

    public void Update(IEnumerable<ClientAgreement> clientAgreements) {
        _connection.Execute(
            "UPDATE ClientAgreement SET " +
            "ProductReservationTerm = @ProductReservationTerm, ClientId = @ClientId, AgreementId = @AgreementId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid ",
            clientAgreements
        );
    }

    public void UpdateAmountByNetId(Guid netId, decimal amount) {
        _connection.Execute(
            "UPDATE [ClientAgreement] " +
            "SET [CurrentAmount] = @Amount, [Updated] = getutcdate() " +
            "WHERE [ClientAgreement].NetUID = @NetId",
            new { NetId = netId, Amount = amount }
        );
    }

    public ClientAgreement GetByClientAndAgreementIds(long clientId, long agreementId) {
        return _connection.Query<ClientAgreement, Agreement, ClientAgreement>(
            "SELECT TOP(1) * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [ClientAgreement].ClientID = @ClientId " +
            "AND [ClientAgreement].AgreementID = @AgreementId",
            (clientAgreement, agreement) => {
                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { ClientId = clientId, AgreementId = agreementId }
        ).SingleOrDefault();
    }

    public List<ClientAgreement> GetAllByClientIdWithoutIncludes(long id) {
        return _connection.Query<ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "WHERE ClientID = @Id AND Deleted = 0",
            new { Id = id }
        ).ToList();
    }

    public List<ClientAgreement> GetAllByAgreementIds(IEnumerable<long> ids) {
        return _connection.Query<ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "WHERE AgreementID IN @Ids AND Deleted = 0",
            new { Ids = ids }
        ).ToList();
    }

    public void Remove(IEnumerable<ClientAgreement> clientAgreements) {
        _connection.Execute(
            "UPDATE ClientAgreement SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetUid ",
            clientAgreements
        );
    }

    public void RemoveAllByClientId(long clientId) {
        _connection.Execute(
            "UPDATE [ClientAgreement] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ClientAgreement].ClientId = @ClientId",
            new { ClientId = clientId }
        );
    }

    public ClientAgreement GetActiveByClientId(long id) {
        return _connection.Query<ClientAgreement, Agreement, ClientAgreement>(
            "SELECT TOP(1) ClientAgreement.*, Agreement.* " +
            "FROM ClientAgreement " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientAgreement.ClientID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "WHERE Client.ID = @Id " +
            "ORDER BY Agreement.IsActive DESC",
            (clientAgreement, agreement) => {
                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public ClientAgreement GetActiveByClientNetId(Guid netId) {
        return _connection.Query<ClientAgreement>(
            "SELECT TOP(1) ClientAgreement.* " +
            "FROM ClientAgreement " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientAgreement.ClientID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "WHERE Client.NetUID = @NetId " +
            "ORDER BY Agreement.IsActive DESC",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByIdWithoutIncludes(long id) {
        return _connection.Query<ClientAgreement>(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "WHERE [ClientAgreement].ID = @Id",
            new { Id = id }
        ).SingleOrDefault();
    }

    public ClientAgreement GetActiveByRootClientNetId(Guid clientNetId, bool withVat) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT TOP(1) ClientAgreement.* " +
            ",Agreement.* " +
            ",Organization.* " +
            "FROM ClientAgreement " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientAgreement.ClientID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "WHERE Client.NetUID = @ClientNetId " +
            "AND ClientAgreement.Deleted = 0 " +
            "AND [Agreement].WithVATAccounting = @WithVat " +
            "ORDER BY Agreement.IsActive DESC",
            (clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { ClientNetId = clientNetId, WithVat = withVat }
        ).SingleOrDefault();
    }

    public ClientAgreement GetSelectedByClientNetId(Guid clientNetId) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT TOP(1) ClientAgreement.* " +
            ",Agreement.* " +
            ",Organization.* " +
            "FROM ClientAgreement " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientAgreement.ClientID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "WHERE Client.NetUID = @ClientNetId " +
            "AND ClientAgreement.Deleted = 0 " +
            "AND [Agreement].IsSelected = 1 ",
            (clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { ClientNetId = clientNetId }
        ).SingleOrDefault();
    }

    public ClientAgreement GetSelectedByClientNotSelectedNetId(Guid clientNetId) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT TOP(1) ClientAgreement.* " +
            ",Agreement.* " +
            ",Organization.* " +
            "FROM ClientAgreement " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientAgreement.ClientID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "WHERE Client.NetUID = @ClientNetId " +
            "AND ClientAgreement.Deleted = 0 ",
            (clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { ClientNetId = clientNetId }
        ).SingleOrDefault();
    }

    public ClientAgreement GetSelectedByWorkplaceNetId(Guid workplaceNetId) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT TOP(1) " +
            "[ClientAgreement].* " +
            ", [Agreement].* " +
            ", [Organization].* " +
            "FROM [WorkplaceClientAgreement] " +
            "LEFT JOIN [Workplace] " +
            "ON [Workplace].ID = [WorkplaceClientAgreement].WorkplaceID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [WorkplaceClientAgreement].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [Workplace].NetUID = @WorkplaceNetId " +
            "AND [WorkplaceClientAgreement].IsSelected = 1 " +
            "AND [WorkplaceClientAgreement].Deleted = 0 " +
            "AND [ClientAgreement].Deleted = 0 ",
            (clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { WorkplaceNetId = workplaceNetId }
        ).SingleOrDefault();
    }

    public ClientAgreement GetActiveBySubClientNetId(Guid clientNetId) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT TOP(1) ClientAgreement.* " +
            ",Agreement.* " +
            ",Organization.* " +
            "FROM Client AS SubClient " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.SubClientID = SubClient.ID " +
            "LEFT JOIN Client " +
            "ON Client.ID = ClientSubClient.RootClientID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ClientID = Client.ID " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "WHERE SubClient.NetUID = @ClientNetId " +
            "AND ClientAgreement.Deleted = 0 " +
            "ORDER BY Agreement.IsActive DESC",
            (clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { ClientNetId = clientNetId }
        ).Single();
    }

    public ClientAgreement GetBySaleId(long id) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT ClientAgreement.* " +
            ",Agreement.* " +
            ",Organization.* " +
            "FROM Sale " +
            "LEFT JOIN ClientAgreement " +
            "ON Sale.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Organization " +
            "ON Organization.ID = Agreement.OrganizationID " +
            "WHERE Sale.ID = @Id",
            (clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { Id = id }
        ).Single();
    }

    public ClientAgreement GetById(long id) {
        ClientAgreement clientAgreementToReturn = null;

        string sqlExpression =
            "SELECT * FROM ClientAgreement " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS [Agreement.Pricing] " +
            "ON Agreement.PricingID = [Agreement.Pricing].ID " +
            "LEFT OUTER JOIN Currency AS [Agreement.Currency] " +
            "ON Agreement.CurrencyID = [Agreement.Currency].ID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = [Agreement.Currency].ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ClientAgreement.ID = ProductGroupDiscount.ClientAgreementID " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductGroupDiscount.ProductGroupID = ProductGroup.ID " +
            "LEFT OUTER JOIN ProductSubGroup " +
            "ON ProductSubGroup.SubProductGroupID = ProductGroup.ID " +
            "AND ProductSubGroup.[Deleted] = 0 " +
            "LEFT OUTER JOIN ProductGroup AS RootProductGroup " +
            "ON RootProductGroup.ID = ProductSubGroup.RootProductGroupID " +
            "LEFT OUTER JOIN ProductGroupDiscount AS RootProductGroupDiscount " +
            "ON RootProductGroupDiscount.ProductGroupID = RootProductGroup.ID " +
            "AND RootProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN Pricing AS BasePricing " +
            "ON [Agreement.Pricing].BasePricingID = BasePricing.ID " +
            "WHERE ClientAgreement.ID = @Id " +
            "AND ClientAgreement.Deleted = 0 " +
            "ORDER BY RootProductGroup.Name ASC, ProductGroup.Name ASC";

        Type[] types = {
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ProductGroupDiscount),
            typeof(ProductGroup),
            typeof(ProductSubGroup),
            typeof(ProductGroup),
            typeof(ProductGroupDiscount),
            typeof(Pricing)
        };

        Func<object[], ClientAgreement> mapper = objects => {
            ClientAgreement clientAgreement = (ClientAgreement)objects[0];
            Agreement agreement = (Agreement)objects[1];
            Pricing pricing = (Pricing)objects[2];
            Currency agreementCurrency = (Currency)objects[3];
            CurrencyTranslation agreementCurrencyTranslation = (CurrencyTranslation)objects[4];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[5];
            ProductGroup productGroup = (ProductGroup)objects[6];
            ProductSubGroup productSubGroup = (ProductSubGroup)objects[7];
            ProductGroup rootProductGroup = (ProductGroup)objects[8];
            ProductGroupDiscount rootProductGroupDiscount = (ProductGroupDiscount)objects[9];
            Pricing basePricing = (Pricing)objects[10];

            if (productGroupDiscount != null) {
                if (productSubGroup != null) {
                    if (clientAgreementToReturn != null &&
                        clientAgreementToReturn.ProductGroupDiscounts.Any(d => d.Id.Equals(rootProductGroupDiscount.Id))) {
                        if (!clientAgreementToReturn
                                .ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroupDiscount.Id))
                                .SubProductGroupDiscounts.Any(s => s.Id.Equals(productGroupDiscount.Id))) {
                            productGroupDiscount.ProductGroup = productGroup;

                            clientAgreementToReturn
                                .ProductGroupDiscounts
                                .First(d => d.Id.Equals(rootProductGroupDiscount.Id)).SubProductGroupDiscounts.Add(productGroupDiscount);
                        }
                    } else {
                        productGroupDiscount.ProductGroup = productGroup;
                        rootProductGroupDiscount.ProductGroup = rootProductGroup;
                        rootProductGroupDiscount.SubProductGroupDiscounts.Add(productGroupDiscount);

                        if (clientAgreementToReturn != null)
                            clientAgreementToReturn.ProductGroupDiscounts.Add(rootProductGroupDiscount);
                        else
                            clientAgreement.ProductGroupDiscounts.Add(rootProductGroupDiscount);
                    }
                } else {
                    productGroupDiscount.ProductGroup = productGroup;

                    if (clientAgreementToReturn != null) {
                        if (!clientAgreementToReturn.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                            clientAgreementToReturn.ProductGroupDiscounts.Add(productGroupDiscount);
                    } else {
                        clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);
                    }
                }
            }

            if (pricing != null) {
                if (basePricing != null) pricing.BasePricing = basePricing;

                agreement.Pricing = pricing;
            }

            if (agreementCurrency != null) {
                agreementCurrency.Name = agreementCurrencyTranslation?.Name;

                agreement.Currency = agreementCurrency;
            }

            clientAgreement.Agreement = agreement;

            if (clientAgreementToReturn == null) clientAgreementToReturn = clientAgreement;

            return clientAgreement;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return clientAgreementToReturn;
    }

    public ClientAgreement GetByIdWithAgreementAndOrganization(long id) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [ClientAgreement].ID = @Id",
            (clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByNetIdWithAgreement(Guid netId) {
        return _connection.Query<ClientAgreement, Agreement, Currency, ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "WHERE ClientAgreement.NetUID = @NetId",
            (clientAgreement, agreement, currency) => {
                if (clientAgreement != null) {
                    agreement.Currency = currency;
                    clientAgreement.Agreement = agreement;
                }

                return clientAgreement;
            },
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByNetIdWithClientInfo(Guid netId) {
        return _connection.Query<ClientAgreement, Client, ClientAgreement>(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID  " +
            "WHERE [ClientAgreement].NetUID = @NetId",
            (clientAgreement, client) => {
                clientAgreement.Client = client;

                return clientAgreement;
            },
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByNetIdWithDiscountForSpecificProduct(Guid netId, long productGroupId) {
        return _connection.Query<ClientAgreement, ProductGroupDiscount, ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.ProductGroupID = @ProductGroupId " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "WHERE ClientAgreement.NetUID = @NetId",
            (clientAgreement, discount) => {
                if (discount != null) clientAgreement.ProductGroupDiscounts.Add(discount);

                return clientAgreement;
            },
            new { NetId = netId, ProductGroupId = productGroupId }
        ).FirstOrDefault();
    }

    public ClientAgreement GetByNetIdWithAgreementAndDiscountForSpecificProduct(Guid netId, long productGroupId) {
        return _connection.Query<ClientAgreement, ProductGroupDiscount, Agreement, Currency, Pricing, ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "AND ProductGroupDiscount.ProductGroupID = @ProductGroupId " +
            "AND ProductGroupDiscount.IsActive = 1 " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "LEFT JOIN Pricing " +
            "ON Agreement.PricingID = Pricing.ID " +
            "WHERE ClientAgreement.NetUID = @NetId",
            (clientAgreement, discount, agreement, currency, pricing) => {
                if (discount != null) clientAgreement.ProductGroupDiscounts.Add(discount);

                agreement.Currency = currency;
                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { NetId = netId, ProductGroupId = productGroupId }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByNetIdWithIncludes(Guid netId) {
        return _connection.Query<ClientAgreement, Client, RegionCode, Agreement, Pricing, Organization, Currency, ClientAgreement>(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Pricing] " +
            "ON [Agreement].PricingID = [Pricing].ID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "WHERE [ClientAgreement].NetUID = @NetId",
            (clientAgreement, client, regionCode, agreement, pricing, organization, currency) => {
                client.RegionCode = regionCode;

                agreement.Pricing = pricing;
                agreement.Organization = organization;
                agreement.Currency = currency;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByNetIdWithClientRole(Guid netId) {
        Type[] types = {
            typeof(ClientAgreement),
            typeof(Client),
            typeof(RegionCode),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Organization),
            typeof(Currency),
            typeof(ClientInRole),
            typeof(ClientTypeRole),
            typeof(ClientType)
        };

        Func<object[], ClientAgreement> mapper = objects => {
            ClientAgreement clientAgreement = (ClientAgreement)objects[0];
            Client client = (Client)objects[1];
            RegionCode regionCode = (RegionCode)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Pricing pricing = (Pricing)objects[4];
            Organization organization = (Organization)objects[5];
            Currency currency = (Currency)objects[6];
            ClientInRole clientInRole = (ClientInRole)objects[7];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[8];
            ClientType clientType = (ClientType)objects[9];

            if (clientInRole != null) {
                clientInRole.ClientType = clientType;
                clientInRole.ClientTypeRole = clientTypeRole;
            }

            client.RegionCode = regionCode;
            client.ClientInRole = clientInRole;

            agreement.Pricing = pricing;
            agreement.Organization = organization;
            agreement.Currency = currency;

            clientAgreement.Client = client;
            clientAgreement.Agreement = agreement;

            return clientAgreement;
        };

        return _connection.Query(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [Pricing] " +
            "ON [Agreement].PricingID = [Pricing].ID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "WHERE [ClientAgreement].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByNetId(Guid netId) {
        ClientAgreement clientAgreementToReturn = null;

        string sqlExpression =
            "SELECT * FROM ClientAgreement " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS [Agreement.Pricing] " +
            "ON Agreement.PricingID = [Agreement.Pricing].ID " +
            "LEFT OUTER JOIN Currency AS [Agreement.Pricing.Currency] " +
            "ON [Agreement.Pricing].CurrencyID = [Agreement.Pricing.Currency].ID " +
            "LEFT JOIN ProductGroupDiscount " +
            "ON ClientAgreement.ID = ProductGroupDiscount.ClientAgreementID " +
            "AND ProductGroupDiscount.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductGroupDiscount.ProductGroupID = ProductGroup.ID " +
            "LEFT OUTER JOIN ProductSubGroup " +
            "ON ProductSubGroup.SubProductGroupID = ProductGroup.ID " +
            "AND ProductSubGroup.[Deleted] = 0 " +
            "LEFT OUTER JOIN ProductGroup AS RootProductGroup " +
            "ON RootProductGroup.ID = ProductSubGroup.RootProductGroupID " +
            "LEFT OUTER JOIN ProductGroupDiscount AS RootProductGroupDiscount " +
            "ON RootProductGroupDiscount.ProductGroupID = RootProductGroup.ID " +
            "AND RootProductGroupDiscount.ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN Pricing AS BasePricing " +
            "ON [Agreement.Pricing].BasePricingID = BasePricing.ID " +
            "LEFT JOIN Currency AS AgreementCurrency " +
            "ON AgreementCurrency.ID = Agreement.CurrencyID " +
            "WHERE ClientAgreement.NetUID = @NetId " +
            "AND ClientAgreement.Deleted = 0 " +
            "ORDER BY RootProductGroup.Name ASC, ProductGroup.Name ASC";

        Type[] types = {
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(ProductGroupDiscount),
            typeof(ProductGroup),
            typeof(ProductSubGroup),
            typeof(ProductGroup),
            typeof(ProductGroupDiscount),
            typeof(Pricing),
            typeof(Currency)
        };

        Func<object[], ClientAgreement> mapper = objects => {
            ClientAgreement clientAgreement = (ClientAgreement)objects[0];
            Agreement agreement = (Agreement)objects[1];
            Pricing pricing = (Pricing)objects[2];
            Currency pricingCurrency = (Currency)objects[3];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[4];
            ProductGroup productGroup = (ProductGroup)objects[5];
            ProductSubGroup productSubGroup = (ProductSubGroup)objects[6];
            ProductGroup rootProductGroup = (ProductGroup)objects[7];
            ProductGroupDiscount rootProductGroupDiscount = (ProductGroupDiscount)objects[8];
            Pricing basePricing = (Pricing)objects[9];
            Currency agreementCurrency = (Currency)objects[10];

            if (productGroupDiscount != null) {
                if (productSubGroup != null) {
                    if (clientAgreementToReturn != null &&
                        clientAgreementToReturn.ProductGroupDiscounts.Any(d => d.Id.Equals(rootProductGroupDiscount.Id))) {
                        if (!clientAgreementToReturn
                                .ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroupDiscount.Id))
                                .SubProductGroupDiscounts.Any(s => s.Id.Equals(productGroupDiscount.Id))) {
                            productGroupDiscount.ProductGroup = productGroup;

                            clientAgreementToReturn
                                .ProductGroupDiscounts
                                .First(d => d.Id.Equals(rootProductGroupDiscount.Id)).SubProductGroupDiscounts.Add(productGroupDiscount);
                        }
                    } else {
                        productGroupDiscount.ProductGroup = productGroup;
                        rootProductGroupDiscount.ProductGroup = rootProductGroup;
                        rootProductGroupDiscount.SubProductGroupDiscounts.Add(productGroupDiscount);

                        if (clientAgreementToReturn != null)
                            clientAgreementToReturn.ProductGroupDiscounts.Add(rootProductGroupDiscount);
                        else
                            clientAgreement.ProductGroupDiscounts.Add(rootProductGroupDiscount);
                    }
                } else {
                    productGroupDiscount.ProductGroup = productGroup;

                    if (clientAgreementToReturn != null) {
                        if (!clientAgreementToReturn.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id)))
                            clientAgreementToReturn.ProductGroupDiscounts.Add(productGroupDiscount);
                    } else {
                        clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);
                    }
                }
            }

            if (pricing != null) {
                if (basePricing != null) pricing.BasePricing = basePricing;

                pricing.Currency = pricingCurrency;

                agreement.Pricing = pricing;
            }

            agreement.Currency = agreementCurrency;

            clientAgreement.Agreement = agreement;

            if (clientAgreementToReturn == null) clientAgreementToReturn = clientAgreement;

            return clientAgreement;
        };

        var props = new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return clientAgreementToReturn;
    }

    public ClientAgreement GetByNetIdWithOrganizationInfo(Guid netId) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [ClientAgreement].NetUID = @NetId",
            (clientAgreement, agreement, organization) => {
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public List<ClientAgreement> GetAllByRetailClientNetId(Guid retailClientNetId) {
        List<ClientAgreement> toReturn = new();

        Type[] types = {
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(Organization),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Sale),
            typeof(RetailClient),
            typeof(ReSale),
            typeof(SaleNumber),
            typeof(User),
            typeof(Transporter),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus)
        };

        Func<object[], ClientAgreement> mapper = objects => {
            ClientAgreement clientAgreement = (ClientAgreement)objects[0];
            Client client = (Client)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Pricing pricing = (Pricing)objects[3];
            Currency currency = (Currency)objects[4];
            Organization organization = (Organization)objects[5];
            ClientInDebt clientInDebt = (ClientInDebt)objects[6];
            Debt debt = (Debt)objects[7];
            Sale sale = (Sale)objects[8];
            RetailClient retailClient = (RetailClient)objects[9];
            ReSale reSale = (ReSale)objects[10];
            SaleNumber saleNumber = (SaleNumber)objects[11];
            User user = (User)objects[12];
            Transporter transporter = (Transporter)objects[13];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[14];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[15];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[16];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[17];

            if (toReturn.Any(c => c.Id.Equals(clientAgreement.Id))) {
                if (debt == null) return clientAgreement;

                ClientAgreement fromList = toReturn.First(c => c.Id.Equals(clientAgreement.Id));

                if (fromList.Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) return clientAgreement;

                clientInDebt.Debt = debt;

                if (sale != null) {
                    sale.SaleNumber = saleNumber;
                    sale.User = user;
                    sale.RetailClient = retailClient;
                    sale.Transporter = transporter;
                    sale.DeliveryRecipient = deliveryRecipient;
                    sale.DeliveryRecipientAddress = deliveryRecipientAddress;
                    sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    sale.TotalAmount = Math.Round(Convert.ToDecimal(debt.Total), 2);
                } else if (reSale != null) {
                    reSale.SaleNumber = saleNumber;
                    reSale.User = user;
                    reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                    reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                    reSale.TotalAmount = Math.Round(Convert.ToDecimal(debt.Total), 2);
                }

                clientInDebt.Sale = sale;
                clientInDebt.ReSale = reSale;

                fromList.Agreement.ClientInDebts.Add(clientInDebt);
            } else {
                if (debt != null) {
                    clientInDebt.Debt = debt;

                    if (sale != null) {
                        sale.SaleNumber = saleNumber;
                        sale.User = user;
                        sale.RetailClient = retailClient;
                        sale.Transporter = transporter;
                        sale.DeliveryRecipient = deliveryRecipient;
                        sale.DeliveryRecipientAddress = deliveryRecipientAddress;
                        sale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        sale.BaseSalePaymentStatus = baseSalePaymentStatus;
                        sale.TotalAmount = Math.Round(Convert.ToDecimal(debt.Total), 2);
                    } else if (reSale != null) {
                        reSale.SaleNumber = saleNumber;
                        reSale.User = user;
                        reSale.BaseLifeCycleStatus = baseLifeCycleStatus;
                        reSale.BaseSalePaymentStatus = baseSalePaymentStatus;
                        reSale.TotalAmount = Math.Round(Convert.ToDecimal(debt.Total), 2);
                    }

                    clientInDebt.Sale = sale;
                    clientInDebt.ReSale = reSale;
                    agreement.ClientInDebts.Add(clientInDebt);
                }

                agreement.Currency = currency;
                agreement.Organization = organization;
                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;
                clientAgreement.Client = client;

                toReturn.Add(clientAgreement);
            }

            return clientAgreement;
        };

        var props = new { RetailClientNetId = retailClientNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            "SELECT " +
            "ClientAgreement.* " +
            ", Client.* " +
            ", Agreement.* " +
            ", Pricing.* " +
            ", Currency.* " +
            ", Organization.* " +
            ", ClientInDebt.* " +
            ", Debt.* " +
            ", Sale.* " +
            ", RetailClient.* " +
            ", ReSale.* " +
            ", SaleNumber.* " +
            ", [User].* " +
            ", Transporter.* " +
            ", DeliveryRecipient.* " +
            ", DeliveryRecipientAddress.* " +
            ", BaseLifeCycleStatus.* " +
            ", BaseSalePaymentStatus.* " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN ( " +
            "SELECT [Organization].ID " +
            ",[Organization].Created " +
            ",[Organization].Deleted " +
            ",[Organization].Code " +
            ", [Organization].FullName " +
            ", [Organization].TIN " +
            ", [Organization].USREOU " +
            ", [Organization].SROI " +
            ", [Organization].RegistrationNumber " +
            ", [Organization].PFURegistrationNumber " +
            ", [Organization].PhoneNumber " +
            ", [Organization].Address " +
            ",[OrganizationTranslation].Name " +
            ",[Organization].NetUID " +
            ",[Organization].Updated " +
            "FROM [Organization] " +
            "LEFT JOIN [OrganizationTranslation] " +
            "ON [OrganizationTranslation].OrganizationID = [Organization].ID " +
            "AND [OrganizationTranslation].CultureCode = @Culture " +
            "AND [OrganizationTranslation].Deleted = 0 " +
            ") AS [Organization] " +
            "ON [Agreement].OrganizationID = [Organization].ID " +
            "LEFT JOIN [ClientInDebt] " +
            "ON [ClientInDebt].AgreementID = [Agreement].ID " +
            "AND [ClientInDebt].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT [Debt].ID " +
            ",[Debt].Deleted " +
            ",[Debt].Created " +
            ",[Debt].NetUID " +
            ",[Debt].Updated " +
            ",DATEDIFF(DAY, [Debt].Created, GETUTCDATE()) AS [Days] " +
            ",ROUND([Debt].Total, 2) AS [Total] " +
            "FROM [Debt] " +
            ") AS [Debt] " +
            "ON [ClientInDebt].DebtID = [Debt].ID " +
            "AND [Debt].Deleted = 0 " +
            "AND [Debt].Total != 0 " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ClientInDebt].[SaleID] " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [ClientInDebt].[ReSaleID] " +
            "LEFT JOIN [SaleNumber] " +
            "ON " +
            "CASE " +
            "WHEN [Sale].[ID] IS NOT NULL " +
            "THEN [Sale].[SaleNumberID] " +
            "ELSE [ReSale].[SaleNumberID] " +
            "END = [SaleNumber].[ID] " +
            "LEFT JOIN [User] " +
            "ON " +
            "CASE " +
            "WHEN [Sale].[ID] IS NOT NULL " +
            "THEN [Sale].[UserID] " +
            "ELSE [ReSale].[UserID] " +
            "END = [User].[ID] " +
            "LEFT JOIN [Transporter] " +
            "ON [Transporter].ID = [Sale].TransporterID " +
            "LEFT JOIN [DeliveryRecipient] " +
            "ON [DeliveryRecipient].ID = [Sale].DeliveryRecipientID " +
            "LEFT JOIN [DeliveryRecipientAddress] " +
            "ON [DeliveryRecipientAddress].ID = [Sale].DeliveryRecipientAddressID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON " +
            "CASE " +
            "WHEN [Sale].[ID] IS NOT NULL " +
            "THEN [Sale].[BaseLifeCycleStatusID] " +
            "ELSE [ReSale].[BaseLifeCycleStatusID] " +
            "END = [BaseLifeCycleStatus].[ID] " +
            "LEFT JOIN [BaseSalePaymentStatus] " +
            "ON " +
            "CASE " +
            "WHEN [Sale].[ID] IS NOT NULL " +
            "THEN [Sale].[BaseSalePaymentStatusID] " +
            "ELSE [ReSale].[BaseSalePaymentStatusID] " +
            "END = [BaseSalePaymentStatus].[ID] " +
            "LEFT JOIN [RetailClient] " +
            "ON [RetailClient].ID = [Sale].RetailClientId " +
            "WHERE [RetailClient].NetUID = @RetailClientNetId " +
            "AND [ClientAgreement].Deleted = 0 " +
            "AND ClientInDebt.ID IS NOT NULL ",
            types,
            mapper,
            props);

        return toReturn;
    }

    public List<ClientAgreement> GetAllByClientNetId(Guid netId) {
        List<ClientAgreement> toReturn = new();

        Type[] types = {
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(Organization),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(PaymentRegister)
        };

        Func<object[], ClientAgreement> mapper = objects => {
            ClientAgreement clientAgreement = (ClientAgreement)objects[0];
            Agreement agreement = (Agreement)objects[1];
            Pricing pricing = (Pricing)objects[2];
            Currency currency = (Currency)objects[3];
            Organization organization = (Organization)objects[4];
            ClientInDebt clientInDebt = (ClientInDebt)objects[5];
            Debt debt = (Debt)objects[6];
            PaymentRegister paymentRegister = (PaymentRegister)objects[7];

            if (toReturn.Any(c => c.Id.Equals(clientAgreement.Id))) {
                if (debt == null) return clientAgreement;

                ClientAgreement fromList = toReturn.First(c => c.Id.Equals(clientAgreement.Id));

                if (fromList.Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) return clientAgreement;

                clientInDebt.Debt = debt;

                fromList.Agreement.ClientInDebts.Add(clientInDebt);
            } else {
                if (debt != null) {
                    clientInDebt.Debt = debt;

                    agreement.ClientInDebts.Add(clientInDebt);
                }

                agreement.Currency = currency;
                agreement.Organization = organization;
                organization.MainPaymentRegister = paymentRegister;
                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;

                toReturn.Add(clientAgreement);
            }

            return clientAgreement;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            "SELECT " +
            " [ClientAgreement].* " +
            ", CASE WHEN [ClientAgreement].OriginalClientAmgCode IS NULL THEN 0 ELSE 1 END [FromAmg] " +
            ", CASE WHEN [AmgOriginalClient].[Name] IS NULL THEN [FenixOriginalClient].[Name] ELSE [AmgOriginalClient].[Name] END [OriginalClientName] " +
            ", [Agreement].* " +
            ", [Pricing].* " +
            ", [Currency].* " +
            ", [Organization].* " +
            ", [ClientInDebt].* " +
            ", [Debt].* " +
            ", [PaymentRegister].* " +
            "FROM [Client] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [Client] [AmgOriginalClient] " +
            "ON [AmgOriginalClient].[SourceAmgCode] = [ClientAgreement].[OriginalClientAmgCode] " +
            "AND [AmgOriginalClient].[SourceAmgCode] != [Client].[SourceAmgCode] " +
            "AND [AmgOriginalClient].[Name] IS NOT NULL " +
            "LEFT JOIN [Client] [FenixOriginalClient] " +
            "ON [FenixOriginalClient].[SourceFenixCode] = [ClientAgreement].[OriginalClientFenixCode] " +
            "AND [FenixOriginalClient].[SourceFenixCode] != [Client].[SourceFenixCode] " +
            "AND [FenixOriginalClient].[Name] IS NOT NULL " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ClientInDebt] " +
            "ON [ClientInDebt].AgreementID = [Agreement].ID " +
            "AND [ClientInDebt].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT [Debt].ID " +
            ",[Debt].Deleted " +
            ",[Debt].Created " +
            ",[Debt].NetUID " +
            ",[Debt].Updated " +
            ",DATEDIFF(DAY, [Debt].Created, GETUTCDATE()) AS [Days] " +
            ",[Debt].Total AS [Total] " +
            "FROM [Debt] " +
            ") AS [Debt] " +
            "ON [ClientInDebt].DebtID = [Debt].ID " +
            "AND [Debt].Deleted = 0 " +
            "AND [Debt].Total != 0 " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].[OrganizationID] = [Organization].[ID] " +
            "AND [PaymentRegister].[IsMain] = 1 " +
            "WHERE [Client].NetUID = @NetId " +
            "AND [ClientAgreement].Deleted = 0",
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public List<ClientAgreement> GetAllByClientNetIdGrouped(Guid netId) {
        List<ClientAgreement> toReturn = new();

        Type[] types = {
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(Currency),
            typeof(Organization),
            typeof(ClientInDebt),
            typeof(Debt)
        };

        Func<object[], ClientAgreement> mapper = objects => {
            ClientAgreement clientAgreement = (ClientAgreement)objects[0];
            Agreement agreement = (Agreement)objects[1];
            Pricing pricing = (Pricing)objects[2];
            Currency currency = (Currency)objects[3];
            Organization organization = (Organization)objects[4];
            ClientInDebt clientInDebt = (ClientInDebt)objects[5];
            Debt debt = (Debt)objects[6];

            if (toReturn.Any(c => c.Id.Equals(clientAgreement.Id))) {
                if (debt == null) return clientAgreement;

                ClientAgreement fromList = toReturn.First(c => c.Id.Equals(clientAgreement.Id));

                if (fromList.Agreement.ClientInDebts.Any()) return clientAgreement;

                ClientInDebt debtFromList = fromList.Agreement.ClientInDebts.First();

                debtFromList.Debt.Total = Math.Round(debtFromList.Debt.Total + debt.Total, 2);
            } else {
                if (debt != null) {
                    clientInDebt.Debt = debt;

                    agreement.ClientInDebts.Add(clientInDebt);
                }

                agreement.Currency = currency;
                agreement.Organization = organization;
                agreement.Pricing = pricing;

                clientAgreement.Agreement = agreement;

                toReturn.Add(clientAgreement);
            }

            return clientAgreement;
        };

        var props = new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            "SELECT * " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].AgreementID = [Agreement].ID " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ClientInDebt] " +
            "ON [ClientInDebt].AgreementID = [Agreement].ID " +
            "AND [ClientInDebt].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT [Debt].Created " +
            ",[Debt].Deleted " +
            ",[Debt].ID " +
            ",[Debt].NetUID " +
            ",[Debt].Updated " +
            ",DATEDIFF(DAY, [Debt].Created, GETUTCDATE()) AS [Days] " +
            ",ROUND([Debt].Total, 2) AS [Total] " +
            "FROM [Debt] " +
            ") AS [Debt] " +
            "ON [ClientInDebt].DebtID = [Debt].ID " +
            "AND [Debt].Deleted = 0 " +
            "AND [Debt].Total != 0 " +
            "WHERE [ClientAgreement].ClientID = (SELECT [Client].ID FROM [Client] WHERE [Client].NetUID = @NetId)" +
            "AND [ClientAgreement].Deleted = 0",
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public List<ClientAgreement> GetAllWithSubClientsByClientNetId(Guid clientNetId) {
        List<ClientAgreement> clientAgreements = new();

        string sqlExpression =
            "SELECT * FROM ClientAgreement " +
            "LEFT OUTER JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Pricing " +
            "ON Agreement.PricingID = Pricing.ID " +
            "LEFT OUTER JOIN PricingTranslation " +
            "ON Pricing.ID = PricingTranslation.PricingID " +
            "AND PricingTranslation.CultureCode = @Culture " +
            "AND PricingTranslation.Deleted = 0 " +
            "LEFT OUTER JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "LEFT OUTER JOIN CurrencyTranslation " +
            "ON Currency.ID = CurrencyTranslation.CurrencyID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "AND CurrencyTranslation.Deleted = 0 " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "LEFT OUTER JOIN OrganizationTranslation " +
            "ON Organization.ID = OrganizationTranslation.OrganizationID " +
            "AND OrganizationTranslation.CultureCode = @Culture " +
            "AND OrganizationTranslation.Deleted = 0 " +
            "LEFT JOIN ClientInDebt " +
            "ON ClientInDebt.AgreementID = Agreement.ID " +
            "AND ClientInDebt.Deleted = 0 " +
            "LEFT JOIN Debt " +
            "ON ClientInDebt.DebtID = Debt.ID " +
            "WHERE ClientAgreement.ClientID IN (SELECT ID FROM Client WHERE Client.NetUID = @ClientNetID " +
            "UNION ALL " +
            "SELECT ClientSubClient.SubClientID AS ID FROM Client " +
            "LEFT OUTER JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "WHERE Client.NetUID = @ClientNetID " +
            ")";

        Type[] types = {
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Organization),
            typeof(OrganizationTranslation),
            typeof(ClientInDebt),
            typeof(Debt)
        };

        Func<object[], ClientAgreement> mapper = objects => {
            ClientAgreement clientAgreement = (ClientAgreement)objects[0];
            Agreement agreement = (Agreement)objects[1];
            Pricing pricing = (Pricing)objects[2];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[3];
            Currency agreementCurrency = (Currency)objects[4];
            CurrencyTranslation agreementCurrencyTranslation = (CurrencyTranslation)objects[5];
            Organization organization = (Organization)objects[6];
            OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[7];
            ClientInDebt clientInDebt = (ClientInDebt)objects[8];
            Debt debt = (Debt)objects[9];

            if (!clientAgreements.Any(c => c.Id.Equals(clientAgreement.Id))) {
                agreementCurrency.Name = agreementCurrencyTranslation?.Name;

                if (pricing != null) {
                    pricing.Name = pricingTranslation != null ? pricingTranslation.Name : pricing.Name;

                    agreement.Pricing = pricing;
                }

                organization.Name = organizationTranslation?.Name;

                if (clientInDebt != null) {
                    clientInDebt.Debt = debt;

                    agreement.ClientInDebts.Add(clientInDebt);
                }

                agreement.Currency = agreementCurrency;
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                clientAgreements.Add(clientAgreement);
            } else {
                if (clientInDebt == null || clientAgreements.Any(c => c.Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))))
                    return clientAgreement;

                clientInDebt.Debt = debt;

                clientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Add(clientInDebt);
            }

            return clientAgreement;
        };

        var props = new { ClientNetId = clientNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return clientAgreements;
    }

    public ClientAgreement GetWithOrganizationByNetId(Guid netId) {
        return _connection.Query<ClientAgreement, Agreement, Organization, Currency, ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "LEFT OUTER JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT OUTER JOIN Organization " +
            "ON Organization.ID = Agreement.OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE ClientAgreement.NetUID = @NetId",
            (clientAgreement, agreement, organization, currency) => {
                if (agreement == null) return clientAgreement;

                if (organization != null) agreement.Organization = organization;

                agreement.Currency = currency;
                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public ClientAgreement GetWithOrganizationById(long id) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "LEFT OUTER JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT OUTER JOIN Organization " +
            "ON Organization.ID = Agreement.OrganizationID " +
            "WHERE ClientAgreement.ID = @Id",
            (clientAgreement, agreement, organization) => {
                if (agreement == null) return clientAgreement;

                if (organization != null) agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { Id = id }
        ).SingleOrDefault();
    }

    public bool IsSubClientsHasAgreements(Guid netId) {
        return _connection.Query<bool>(
            "SELECT CAST(CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END AS BIT) " +
            "FROM ClientAgreement " +
            "WHERE ClientAgreement.ClientID IN (SELECT ClientSubClient.SubClientID AS ID FROM Client " +
            "LEFT OUTER JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "WHERE Client.NetUID = @ClientNetId " +
            ")",
            new { ClientNetId = netId }
        ).SingleOrDefault();
    }

    public ClientAgreement GetByNetIdWithAgreementAndOrganization(Guid netId) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT * FROM ClientAgreement " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
            "WHERE ClientAgreement.NetUID = @NetId",
            (clientAgreement, agreement, organization) => {
                if (clientAgreement == null) return null;
                agreement.Organization = organization;
                clientAgreement.Agreement = agreement;

                return clientAgreement;
            },
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public ClientAgreement GetClientAgreementWithAgreementByPackingListId(long id) {
        return _connection.Query<ClientAgreement>(
            "SELECT TOP 1 [ClientAgreement].* " +
            "FROM [PackingList] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrder].[ClientAgreementID] " +
            "WHERE [PackingList].[ID] = @Id ",
            new { Id = id }).FirstOrDefault();
    }

    public ClientAgreement GetClientAgreementBySupplyOrderUkraineId(long id) {
        return _connection.Query<ClientAgreement>(
            "SELECT [ClientAgreement].* FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "WHERE [SupplyOrderUkraine].[ID] = @Id; ",
            new { Id = id }).FirstOrDefault();
    }

    public ClientAgreement GetByClientNetIdWithOrWithoutVat(Guid netId, long organizationId, bool withVat) {
        return _connection.Query<ClientAgreement, Agreement, Organization, ClientAgreement>(
            "SELECT [ClientAgreement].* " +
            ", [Agreement].* " +
            ", [Organization].* " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN Currency " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "WHERE [Client].NetUID = @NetId " +
            "AND [Agreement].WithVATAccounting = @WithVat " +
            "AND [Agreement].OrganizationID = @OrganizationId ",
            (clientAgreement, agreement, organization) => {
                if (clientAgreement != null) {
                    if (agreement != null) agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;
                }

                return clientAgreement;
            },
            new {
                NetId = netId,
                OrganizationId = organizationId,
                WithVat = withVat
            }
        ).FirstOrDefault();
    }

    public ClientAgreement GetWithClientInfoByAgreementNetId(Guid netId) {
        ClientAgreement toReturn = null;

        Type[] types = {
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Client),
            typeof(Organization)
        };

        Func<object[], Agreement> mapper = objects => {
            ClientAgreement clientAgreement = (ClientAgreement)objects[0];
            Agreement agreement = (Agreement)objects[1];
            Client client = (Client)objects[2];
            Organization organization = (Organization)objects[3];

            if (toReturn == null)
                toReturn = clientAgreement;

            agreement.Organization = organization;
            toReturn.Agreement = agreement;
            toReturn.Client = client;

            return agreement;
        };

        _connection.Query(
            "SELECT * FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [ClientAgreement].[AgreementID] = [Agreement].[ID] " +
            "LEFT JOIN [Client] " +
            "ON  [Client].[ID] = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
            "WHERE [Agreement].[NetUID] = @NetId; ",
            types, mapper, new { NetId = netId });

        toReturn.Agreement.Organization.MainPaymentRegister =
            _connection.Query<PaymentRegister>(
                "SELECT TOP 1 * FROM [PaymentRegister] " +
                "WHERE [PaymentRegister].[OrganizationID] = @Id " +
                "AND [PaymentRegister].[IsMain] = 1 " +
                "ORDER BY [PaymentRegister].[Created] DESC; ",
                new { Id = toReturn.Agreement.OrganizationId }).FirstOrDefault();

        return toReturn;
    }
}