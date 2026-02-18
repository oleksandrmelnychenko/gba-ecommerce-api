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
using GBA.Domain.Entities.Clients.Documents;
using GBA.Domain.Entities.Clients.PackingMarkings;
using GBA.Domain.Entities.Clients.PerfectClients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.Entities.Transporters;
using GBA.Domain.EntityHelpers.ClientModels;
using GBA.Domain.EntityHelpers.OrderItemModels;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientRepository : IClientRepository {
    private readonly IDbConnection _connection;

    public ClientRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Client client) {
        return _connection.Query<long>(
                "INSERT INTO Client " +
                "(TIN, USREOU, SROI, Name, FullName, SupplierName, SupplierContactName, SupplierCode, Manufacturer, Brand, Comment, MobileNumber, ClientNumber, " +
                "SMSNumber, FaxNumber, AccountantNumber, DirectorNumber, ICQ, EmailAddress, DeliveryAddress, LegalAddress, ActualAddress, IncotermsElse, RegionId, " +
                "RegionCodeId, CountryID, ClientBankDetailsID, TermsOfDeliveryID, PackingMarkingID, PackingMarkingPaymentID, IsIndividual, IsActive, IsSubClient, " +
                "IsBlocked, IsTradePoint, IsPayForDelivery, IsIncotermsElse, IsTemporaryClient, FirstName, MiddleName, LastName, Street, " +
                "ZipCode, HouseNumber, IsFromECommerce, Updated, [Manager], IsForRetail, [IsNotResident], [OrderExpireDays], [ClearCartAfterDays]) " +
                "VALUES (@TIN, @USREOU, @SROI, @Name, @FullName, @SupplierName, @SupplierContactName, @SupplierCode, @Manufacturer, @Brand, @Comment, " +
                "@MobileNumber, @ClientNumber, @SMSNumber, @FaxNumber, @AccountantNumber, @DirectorNumber, @ICQ, @EmailAddress, @DeliveryAddress, " +
                "@LegalAddress, @ActualAddress, @IncotermsElse, @RegionId, @RegionCodeId, @CountryID, @ClientBankDetailsID, @TermsOfDeliveryID, " +
                "@PackingMarkingID, @PackingMarkingPaymentID, @IsIndividual, @IsActive, @IsSubClient, @IsBlocked, @IsTradePoint, @IsPayForDelivery, " +
                "@IsIncotermsElse, @IsTemporaryClient, @FirstName, @MiddleName, @LastName, @Street, @ZipCode, @HouseNumber, @IsFromECommerce, getutcdate(), " +
                "@Manager, @IsForRetail, @IsNotResident, @OrderExpireDays, @ClearCartAfterDays); " +
                "SELECT SCOPE_IDENTITY() ",
                client
            )
            .Single();
    }

    public void Update(Client client) {
        _connection.Execute(
            "UPDATE Client SET " +
            "TIN = @TIN, USREOU = @USREOU, SROI = @SROI, Name = @Name, FullName = @FullName, SupplierName = @SupplierName, SupplierContactName = @SupplierContactName, " +
            "SupplierCode = @SupplierCode, Manufacturer = @Manufacturer, Brand = @Brand, Comment = @Comment, MobileNumber = @MobileNumber, ClientNumber = @ClientNumber, " +
            "SMSNumber = @SMSNumber, FaxNumber = @FaxNumber, AccountantNumber = @AccountantNumber, DirectorNumber = @DirectorNumber, " +
            "ICQ = @ICQ, EmailAddress = @EmailAddress, DeliveryAddress = @DeliveryAddress, LegalAddress = @LegalAddress, ActualAddress = @ActualAddress, " +
            "IncotermsElse = @IncotermsElse, RegionId = @RegionId, RegionCodeId = @RegionCodeId, CountryID = @CountryID, ClientBankDetailsID = @ClientBankDetailsID, " +
            "TermsOfDeliveryID = @TermsOfDeliveryID, PackingMarkingID = @PackingMarkingID, PackingMarkingPaymentID = @PackingMarkingPaymentID, " +
            "IsIndividual = @IsIndividual, IsActive = @IsActive, IsSubClient = @IsSubClient, IsBlocked = @IsBlocked, IsTradePoint = @IsTradePoint, " +
            "IsPayForDelivery = @IsPayForDelivery, IsIncotermsElse = @IsIncotermsElse, IsTemporaryClient = @IsTemporaryClient, Street = @Street, ZipCode = @ZipCode, " +
            "FirstName = @FirstName, MiddleName = @MiddleName, LastName = @LastName, HouseNumber = @HouseNumber, ClearCartAfterDays = @ClearCartAfterDays, Updated = getutcdate(), " +
            "[Manager] = @Manager, [IsForRetail] = @IsForRetail, [IsNotResident] = @IsNotResident, [OrderExpireDays] = @OrderExpireDays " +
            "WHERE NetUID = @NetUid",
            client
        );
    }

    public void SetTemporaryClientById(long id) {
        _connection.Execute(
            "UPDATE [Client] " +
            "SET IsTemporaryClient = 0 " +
            "WHERE [Client].ID = @Id",
            new { Id = id }
        );
    }

    public Client GetRetailClient() {
        return _connection.Query<Client>(
            "SELECT * FROM [Client] " +
            "WHERE [IsForRetail] = 1 " +
            "AND [Deleted] = 0").SingleOrDefault();
    }

    public void SetWorkplaceStatus(long id) {
        _connection.Execute(
            "UPDATE [Client] SET " +
            "IsWorkPlace = 1 " +
            "WHERE [Client].ID = @Id",
            new { Id = id });
    }

    public Guid GetClientNetIdByRegionCode(string value) {
        return _connection.Query<Guid>(
                "SELECT [Client].NetUID " +
                "FROM [Client] " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "WHERE [Client].Deleted = 0 " +
                "AND [RegionCode].[Value] = @Value",
                new { Value = value }
            )
            .FirstOrDefault();
    }

    public Guid GetRootNetIdBySubClientNetId(Guid netId) {
        return _connection.Query<Guid>(
            "SELECT RootClient.NetUID FROM Client AS RootClient " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = RootClient.ID " +
            "LEFT JOIN Client AS SubClient " +
            "ON SubClient.ID = ClientSubClient.SubClientID " +
            "WHERE SubClient.NetUID = @NetId ",
            new { NetId = netId }
        ).FirstOrDefault();
    }

    public Client GetClientNetIdByMobileNumber(string value) {
        return _connection.Query<Client>(
                "SELECT * FROM [Client] " +
                "WHERE [Client].Deleted = 0 " +
                "AND [Client].[MobileNumber] = @Value",
                new { Value = value }
            )
            .FirstOrDefault();
    }


    public Client GetById(long id) {
        Client clientToReturn = null;

        string sqlExpression = "SELECT * FROM Client " +
                               "LEFT OUTER JOIN ClientInRole " +
                               "ON Client.ID = ClientInRole.ClientID " +
                               "LEFT JOIN ClientTypeRole " +
                               "ON ClientInRole.ClientTypeRoleID = ClientTypeRole.ID " +
                               "LEFT JOIN ClientType " +
                               "ON ClientInRole.ClientTypeID = ClientType.ID " +
                               "LEFT JOIN ClientTypeTranslation " +
                               "ON ClientType.ID = ClientTypeTranslation.ClientTypeID AND ClientTypeTranslation.CultureCode = @Culture " +
                               "LEFT JOIN ClientAgreement " +
                               "ON Client.ID = ClientAgreement.ClientID " +
                               "LEFT JOIN Agreement " +
                               "ON ClientAgreement.AgreementID = Agreement.ID " +
                               "LEFT JOIN Region " +
                               "ON Client.RegionID = Region.ID " +
                               "LEFT JOIN RegionCode " +
                               "ON RegionCode.ID = Client.RegionCodeId " +
                               "WHERE Client.ID = @Id";

        Type[] types = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientTypeRole),
            typeof(ClientType),
            typeof(ClientTypeTranslation),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Region),
            typeof(RegionCode)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[2];
            ClientType clientType = (ClientType)objects[3];
            ClientTypeTranslation clientTypeTranslation = (ClientTypeTranslation)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Region region = (Region)objects[7];
            RegionCode regionCode = (RegionCode)objects[8];

            if (regionCode != null) client.RegionCode = regionCode;

            if (region != null) client.Region = region;

            if (clientInRole != null) {
                clientType.Name = clientTypeTranslation?.Name;

                clientInRole.ClientType = clientType;
                clientInRole.ClientTypeRole = clientTypeRole;

                client.ClientInRole = clientInRole;
            }

            if (clientAgreement != null) {
                clientAgreement.Agreement = agreement;

                if (clientToReturn == null) {
                    client.ClientAgreements.Add(clientAgreement);

                    clientToReturn = client;
                } else {
                    clientToReturn.ClientAgreements.Add(clientAgreement);
                }
            } else {
                clientToReturn = client;
            }

            return client;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return clientToReturn;
    }

    public Client GetRootClientBySubClientNetId(Guid netId) {
        Client clientToReturn = null;

        Guid rootClientNetId = _connection.Query<Guid>(
            "SELECT RootClient.NetUID FROM Client AS RootClient " +
            "LEFT JOIN ClientSubClient " +
            "ON ClientSubClient.RootClientID = RootClient.ID " +
            "LEFT JOIN Client AS SubClient " +
            "ON SubClient.ID = ClientSubClient.SubClientID " +
            "WHERE SubClient.NetUID = @NetId ",
            new { NetId = netId }
        ).FirstOrDefault();

        string query =
            "SELECT * FROM Client " +
            "LEFT OUTER JOIN ClientInRole " +
            "ON Client.ID = ClientInRole.ClientID " +
            "LEFT JOIN ClientType " +
            "ON ClientInRole.ClientTypeID = ClientType.ID " +
            "LEFT JOIN ClientTypeTranslation " +
            "ON ClientType.ID = ClientTypeTranslation.ClientTypeID " +
            "AND ClientTypeTranslation.CultureCode = @Culture " +
            "AND ClientTypeTranslation.Deleted = 0 " +
            "LEFT JOIN ClientTypeRole " +
            "ON ClientInRole.ClientTypeRoleID = ClientTypeRole.ID " +
            "LEFT JOIN ClientTypeRoleTranslation " +
            "ON ClientTypeRole.ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
            "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
            "AND ClientTypeRoleTranslation.Deleted = 0 " +
            "LEFT OUTER JOIN ClientUserProfile " +
            "ON Client.ID = ClientUserProfile.ClientID " +
            "AND ClientUserProfile.Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON ClientUserProfile.UserProfileID = [User].ID " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON Client.ID = ClientAgreement.ClientID " +
            "AND ClientAgreement.Deleted = 0 " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS [Agreement.Pricing] " +
            "ON Agreement.PricingID = [Agreement.Pricing].ID " +
            "LEFT OUTER JOIN PricingTranslation AS [Agreement.PricingTranslation] " +
            "ON [Agreement.Pricing].ID = [Agreement.PricingTranslation].PricingID " +
            "AND [Agreement.PricingTranslation].CultureCode = @Culture " +
            "AND [Agreement.PricingTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN Currency AS [Agreement.Currency] " +
            "ON Agreement.CurrencyID = [Agreement.Currency].ID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [Agreement.CurrencyTranslation] " +
            "ON [Agreement.Currency].ID = [Agreement.CurrencyTranslation].CurrencyID " +
            "AND [Agreement.CurrencyTranslation].CultureCode = @Culture " +
            "AND [Agreement.CurrencyTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN Currency AS [Agreement.Pricing.Currency] " +
            "ON [Agreement.Pricing].CurrencyID = [Agreement.Pricing.Currency].ID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [Agreement.Pricing.CurrencyTranslation] " +
            "ON [Agreement.Pricing.Currency].ID = [Agreement.Pricing.CurrencyTranslation].CurrencyID " +
            "AND [Agreement.Pricing.CurrencyTranslation].CultureCode = @Culture " +
            "AND [Agreement.Pricing.CurrencyTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN ProviderPricing " +
            "ON Agreement.ProviderPricingID = ProviderPricing.ID " +
            "LEFT JOIN Pricing AS [ProviderPricing.BasePricing] " +
            "ON ProviderPricing.BasePricingID = [ProviderPricing.BasePricing].ID " +
            "LEFT JOIN PricingTranslation AS [ProviderPricing.BasePricingTranslation] " +
            "ON [ProviderPricing.BasePricing].ID = [ProviderPricing.BasePricingTranslation].PricingID " +
            "AND [ProviderPricing.BasePricingTranslation].CultureCode = @Culture " +
            "AND [ProviderPricing.BasePricingTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN Currency AS [ProviderPricing.BasePricing.Currency] " +
            "ON [ProviderPricing.BasePricing].CurrencyID = [ProviderPricing.BasePricing.Currency].ID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [ProviderPricing.BasePricing.CurrencyTranslation] " +
            "ON [ProviderPricing.BasePricing.Currency].ID = [ProviderPricing.BasePricing.CurrencyTranslation].CurrencyID " +
            "AND [ProviderPricing.BasePricing.CurrencyTranslation].CultureCode = @Culture " +
            "AND [ProviderPricing.BasePricing.CurrencyTranslation].Deleted = 0 " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "LEFT OUTER JOIN OrganizationTranslation " +
            "ON Organization.ID = OrganizationTranslation.OrganizationID " +
            "AND OrganizationTranslation.CultureCode = @Culture " +
            "AND OrganizationTranslation.Deleted = 0 " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "LEFT JOIN Region " +
            "ON RegionCode.RegionID = Region.ID " +
            "LEFT JOIN Region AS ClientRegion " +
            "ON Client.RegionID = ClientRegion.ID " +
            "LEFT OUTER JOIN ClientInDebt " +
            "ON ClientInDebt.AgreementID = Agreement.ID " +
            "AND ClientInDebt.Deleted = 0 " +
            "LEFT OUTER JOIN Debt " +
            "ON ClientInDebt.DebtID = Debt.ID " +
            "LEFT JOIN ServicePayer " +
            "ON Client.ID = ServicePayer.ClientID " +
            "AND ServicePayer.Deleted = 0 " +
            "LEFT OUTER JOIN Country " +
            "ON Country.ID = Client.CountryID " +
            "AND Country.Deleted = 0 " +
            "LEFT OUTER JOIN TermsOfDelivery " +
            "ON TermsOfDelivery.ID = Client.TermsOfDeliveryID " +
            "AND TermsOfDelivery.Deleted = 0 " +
            "LEFT OUTER JOIN ClientBankDetails " +
            "ON ClientBankDetails.ID = Client.ClientBankDetailsID " +
            "AND ClientBankDetails.Deleted = 0 " +
            "LEFT OUTER JOIN ClientBankDetailAccountNumber " +
            "ON ClientBankDetailAccountNumber.ID = ClientBankDetails.AccountNumberID " +
            "LEFT OUTER JOIN Currency AS [ClientBankDetailAccountNumber.Currency] " +
            "ON [ClientBankDetailAccountNumber.Currency].ID = ClientBankDetailAccountNumber.CurrencyID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [ClientBankDetailAccountNumber.CurrencyTranslation] " +
            "ON [ClientBankDetailAccountNumber.Currency].ID = [ClientBankDetailAccountNumber.CurrencyTranslation].CurrencyID " +
            "AND [ClientBankDetailAccountNumber.CurrencyTranslation].CultureCode = @Culture " +
            "AND [ClientBankDetailAccountNumber.CurrencyTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN ClientBankDetailIbanNo " +
            "ON ClientBankDetailIbanNo.ID = ClientBankDetails.ClientBankDetailIbanNoID " +
            "LEFT OUTER JOIN Currency AS [ClientBankDetailIbanNo.Currency] " +
            "ON [ClientBankDetailIbanNo.Currency].ID = ClientBankDetailIbanNo.CurrencyID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [ClientBankDetailIbanNo.CurrencyTranslation] " +
            "ON [ClientBankDetailIbanNo.Currency].ID = [ClientBankDetailIbanNo.CurrencyTranslation].CurrencyID " +
            "AND [ClientBankDetailIbanNo.CurrencyTranslation].CultureCode = @Culture " +
            "AND [ClientBankDetailIbanNo.CurrencyTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN PackingMarking " +
            "ON PackingMarking.ID = Client.PackingMarkingID " +
            "AND PackingMarking.Deleted = 0 " +
            "LEFT OUTER JOIN PackingMarkingPayment " +
            "ON PackingMarkingPayment.ID = Client.PackingMarkingPaymentID " +
            "AND PackingMarkingPayment.Deleted = 0 " +
            "LEFT OUTER JOIN ClientContractDocument " +
            "ON ClientContractDocument.ClientID = Client.ID " +
            "AND ClientContractDocument.Deleted = 0 " +
            "LEFT JOIN [Pricing] AS [PromotionalPricing] " +
            "ON [PromotionalPricing].[ID] = [Agreement].[PromotionalPricingID] " +
            "LEFT JOIN [Currency] AS [PromotionalPricingCurrency] " +
            "ON [PromotionalPricingCurrency].[ID] = [PromotionalPricing].[CurrencyID] " +
            "WHERE Client.NetUID = @NetId " +
            "AND Agreement.ForReSale = 0 ";

        Type[] types = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeTranslation),
            typeof(ClientTypeRole),
            typeof(ClientTypeRoleTranslation),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Organization),
            typeof(OrganizationTranslation),
            typeof(RegionCode),
            typeof(Region),
            typeof(Region),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(ServicePayer),
            typeof(Country),
            typeof(TermsOfDelivery),
            typeof(ClientBankDetails),
            typeof(ClientBankDetailAccountNumber),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ClientBankDetailIbanNo),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(PackingMarking),
            typeof(PackingMarkingPayment),
            typeof(ClientContractDocument),
            typeof(Pricing),
            typeof(Currency)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeTranslation clientTypeTranslation = (ClientTypeTranslation)objects[3];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[4];
            ClientTypeRoleTranslation clientTypeRoleTranslation = (ClientTypeRoleTranslation)objects[5];
            ClientUserProfile clientUserProfile = (ClientUserProfile)objects[6];
            User user = (User)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Agreement agreement = (Agreement)objects[9];
            Pricing pricing = (Pricing)objects[10];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[11];
            Currency agreementCurrency = (Currency)objects[12];
            CurrencyTranslation agreementCurrencyTranslation = (CurrencyTranslation)objects[13];
            Currency pricingCurrency = (Currency)objects[14];
            CurrencyTranslation pricingCurrencyTranslation = (CurrencyTranslation)objects[15];
            ProviderPricing providerPricing = (ProviderPricing)objects[16];
            Pricing providerBasePricing = (Pricing)objects[17];
            PricingTranslation providerBasePricingTranslation = (PricingTranslation)objects[18];
            Currency providerPricingCurrency = (Currency)objects[19];
            CurrencyTranslation providerPricingCurrencyTranslation = (CurrencyTranslation)objects[20];
            Organization organization = (Organization)objects[21];
            OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[22];
            RegionCode regionCode = (RegionCode)objects[23];
            Region region = (Region)objects[24];
            Region clientRegion = (Region)objects[25];
            ClientInDebt clientInDebt = (ClientInDebt)objects[26];
            Debt debt = (Debt)objects[27];
            ServicePayer servicePayer = (ServicePayer)objects[28];
            Country country = (Country)objects[29];
            TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[30];
            ClientBankDetails clientBankDetails = (ClientBankDetails)objects[31];
            ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[32];
            Currency clientBankDetailAccountNumberCurrency = (Currency)objects[33];
            CurrencyTranslation clientBankDetailAccountNumberCurrencyTranslation = (CurrencyTranslation)objects[34];
            ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[35];
            Currency clientBankDetailIbanNoCurrency = (Currency)objects[36];
            CurrencyTranslation clientBankDetailIbanNoCurrencyTranslation = (CurrencyTranslation)objects[37];
            PackingMarking packingMarking = (PackingMarking)objects[38];
            PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[39];
            ClientContractDocument clientContractDocument = (ClientContractDocument)objects[40];
            Pricing promotionalPricing = (Pricing)objects[41];
            Currency promotionalPricingCurrency = (Currency)objects[42];

            if (clientInRole != null) {
                if (clientTypeTranslation != null) clientType.Name = clientTypeTranslation.Name;

                if (clientTypeRoleTranslation != null) {
                    clientTypeRole.Name = clientTypeRoleTranslation.Name;
                    clientTypeRole.Description = clientTypeRoleTranslation.Description;
                }

                clientInRole.ClientType = clientType;
                clientInRole.ClientTypeRole = clientTypeRole;

                client.ClientInRole = clientInRole;
            }

            if (clientRegion != null) client.Region = clientRegion;

            if (regionCode != null) {
                regionCode.Region = region;

                client.RegionCode = regionCode;
            }

            if (country != null) client.Country = country;

            if (termsOfDelivery != null) client.TermsOfDelivery = termsOfDelivery;

            if (packingMarking != null) client.PackingMarking = packingMarking;

            if (packingMarkingPayment != null) client.PackingMarkingPayment = packingMarkingPayment;

            if (clientBankDetails != null) {
                if (clientBankDetailAccountNumber != null) {
                    if (clientBankDetailAccountNumberCurrencyTranslation != null)
                        clientBankDetailAccountNumberCurrency.Name = clientBankDetailAccountNumberCurrencyTranslation.Name;

                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;
                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                }

                if (clientBankDetailIbanNo != null) {
                    if (clientBankDetailIbanNoCurrencyTranslation != null) clientBankDetailIbanNoCurrency.Name = clientBankDetailIbanNoCurrencyTranslation.Name;

                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;
                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                }

                client.ClientBankDetails = clientBankDetails;
            }

            if (clientUserProfile != null) {
                clientUserProfile.UserProfile = user;

                if (clientToReturn == null) {
                    if (!client.ClientManagers.Any(c => c.Id.Equals(clientUserProfile.Id))) client.ClientManagers.Add(clientUserProfile);
                } else {
                    if (!clientToReturn.ClientManagers.Any(c => c.Id.Equals(clientUserProfile.Id))) clientToReturn.ClientManagers.Add(clientUserProfile);
                }
            }

            if (servicePayer != null) {
                if (clientToReturn != null) {
                    if (!clientToReturn.ServicePayers.Any(s => s.Id.Equals(servicePayer.Id))) clientToReturn.ServicePayers.Add(servicePayer);
                } else {
                    if (!client.ServicePayers.Any(s => s.Id.Equals(servicePayer.Id))) client.ServicePayers.Add(servicePayer);
                }
            }

            if (clientAgreement != null) {
                if (agreement != null && clientInDebt != null) {
                    clientInDebt.Debt = debt;

                    if (clientToReturn != null) {
                        if (clientToReturn.ClientAgreements.Any(c => c.Id.Equals(clientAgreement.Id))) {
                            if (!clientToReturn.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id)))
                                clientToReturn.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Add(clientInDebt);
                        } else {
                            if (!agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) agreement.ClientInDebts.Add(clientInDebt);
                        }
                    } else {
                        if (client.ClientAgreements.Any(c => c.Id.Equals(clientAgreement.Id))) {
                            if (!client.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id)))
                                client.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Add(clientInDebt);
                        } else {
                            if (!agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) agreement.ClientInDebts.Add(clientInDebt);
                        }
                    }
                }

                if (clientInDebt != null) {
                    clientInDebt.Debt = debt;

                    if (clientToReturn != null) {
                        if (!clientToReturn.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) clientToReturn.ClientInDebts.Add(clientInDebt);
                    } else {
                        client.ClientInDebts.Add(clientInDebt);
                    }
                }

                if (agreementCurrencyTranslation != null) agreementCurrency.Name = agreementCurrencyTranslation.Name;

                if (pricing != null) {
                    if (pricingTranslation != null) pricing.Name = pricingTranslation.Name;

                    if (pricingCurrencyTranslation != null) pricingCurrency.Name = pricingCurrencyTranslation.Name;

                    pricing.Currency = pricingCurrency;

                    agreement.Pricing = pricing;
                }

                if (promotionalPricing != null) {
                    promotionalPricing.Currency = promotionalPricingCurrency;

                    agreement.PromotionalPricing = promotionalPricing;
                }

                if (providerPricing != null) {
                    if (providerBasePricingTranslation != null) providerBasePricing.Name = providerBasePricingTranslation.Name;

                    if (providerPricingCurrencyTranslation != null) providerPricingCurrency.Name = providerPricingCurrencyTranslation.Name;

                    providerPricing.Currency = providerPricingCurrency;
                    providerPricing.Pricing = providerBasePricing;

                    agreement.ProviderPricing = providerPricing;
                }

                if (organizationTranslation != null) organization.Name = organizationTranslation.Name;

                agreement.Currency = agreementCurrency;
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                if (clientToReturn == null) {
                    client.ClientAgreements.Add(clientAgreement);
                } else {
                    if (!clientToReturn.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) clientToReturn.ClientAgreements.Add(clientAgreement);
                }
            }

            if (clientContractDocument != null) {
                if (clientToReturn == null) {
                    client.ClientContractDocuments.Add(clientContractDocument);
                } else {
                    if (!clientToReturn.ClientContractDocuments.Any(d => d.Id.Equals(clientContractDocument.Id)))
                        clientToReturn.ClientContractDocuments.Add(clientContractDocument);
                }
            }

            if (clientToReturn == null) clientToReturn = client;

            return client;
        };

        var props = new { NetId = rootClientNetId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(query, types, mapper, props);

        return clientToReturn;
    }

    public Client GetByIdWithoutIncludes(long id) {
        return _connection.Query<Client>(
                "SELECT * " +
                ", CONVERT(nvarchar(32), SourceID, 2) [RefId] " +
                "FROM [Client] " +
                "WHERE [Client].ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public Client GetByIdWithRegionCode(long id) {
        return _connection.Query<Client, RegionCode, Client>(
                "SELECT * " +
                "FROM [Client] " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "WHERE [Client].ID = @Id",
                (client, regionCode) => {
                    client.RegionCode = regionCode;

                    return client;
                },
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public Client GetByNetIdWithRegionCode(Guid netId) {
        return _connection.Query<Client, RegionCode, Client>(
                "SELECT * " +
                "FROM [Client] " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "WHERE [Client].NetUID = @NetId",
                (client, regionCode) => {
                    client.RegionCode = regionCode;

                    return client;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public Client GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<Client>(
                "SELECT * " +
                ", CONVERT(nvarchar(32), SourceAmgID, 2) [RefId] " +
                "FROM [Client] " +
                "WHERE [Client].NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public Client SearchClientByMobileNumber(string mobileNumber) {
        return _connection.Query<Client>(
                "SELECT TOP(1) * " +
                "FROM [Client] " +
                "WHERE [Client].Deleted = 0 " +
                "AND (" +
                "[Client].MobileNumber like '%' + @Value + '%' " +
                "OR " +
                "[Client].ClientNumber like '%' + @Value + '%' " +
                "OR " +
                "[Client].SMSNumber like '%' + @Value + '%' " +
                "OR " +
                "[Client].FaxNumber like '%' + @Value + '%' " +
                "OR " +
                "[Client].AccountantNumber like '%' + @Value + '%' " +
                "OR " +
                "[Client].DirectorNumber like '%' + @Value + '%'" +
                ")",
                new { Value = mobileNumber }
            )
            .SingleOrDefault();
    }

    public Client GetByNetIdWithRoleAndType(Guid netId) {
        return _connection.Query<Client, ClientInRole, ClientTypeRole, ClientType, Client>(
                "SELECT * " +
                "FROM [Client] " +
                "LEFT JOIN [ClientInRole] " +
                "ON [ClientInRole].ClientID = [Client].ID " +
                "AND [ClientInRole].Deleted = 0 " +
                "LEFT JOIN [ClientTypeRole] " +
                "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
                "LEFT JOIN [ClientType] " +
                "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
                "WHERE [Client].NetUID = @NetId",
                (client, clientInRole, clientTypeRole, clientType) => {
                    if (clientInRole != null) {
                        clientInRole.ClientTypeRole = clientTypeRole;
                        clientInRole.ClientType = clientType;
                    }

                    client.ClientInRole = clientInRole;

                    return client;
                },
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public IEnumerable<Client> GetByOldEcommerceIds(IEnumerable<long> oldEcommerceIds) {
        return _connection.Query<Client>(
            "SELECT [Client].* " +
            "FROM [Client] " +
            "WHERE ([Client].SourceAmgCode IN @OldEcommerceIds " +
            "OR [Client].SourceFenixCode IN @OldEcommerceIds) " +
            "AND [Client].Deleted = 0 " +
            "ORDER BY [Client].FullName, [Client].Name",
            new { OldEcommerceIds = oldEcommerceIds }
        );
    }

    public Client GetByNetId(Guid netId, bool isFromEcommerce = false) {
        Client clientToReturn = null;

        string sql = "SELECT " +
                     "Client.* " +
                     ", ClientInRole.* " +
                     ", ClientType.* " +
                     ", ClientTypeTranslation.* " +
                     ", ClientTypeRole.* " +
                     ", ClientTypeRoleTranslation.* " +
                     ", ClientUserProfile.* " +
                     ", [User].* " +
                     ", ClientAgreement.* " +
                     ", CASE WHEN [ClientAgreement].OriginalClientAmgCode IS NULL THEN 0 ELSE 1 END [FromAmg] " +
                     ", CASE WHEN [AmgOriginalClient].[Name] IS NULL THEN [FenixOriginalClient].[Name] ELSE [AmgOriginalClient].[Name] END [OriginalClientName] " +
                     ", Agreement.* " +
                     ", [Agreement.Pricing].* " +
                     ", [Agreement.PricingTranslation].* " +
                     ", [Agreement.Currency].* " +
                     ", [Agreement.CurrencyTranslation].* " +
                     ", [Agreement.Pricing.Currency].* " +
                     ", [Agreement.Pricing.CurrencyTranslation].* " +
                     ", ProviderPricing.* " +
                     ", [ProviderPricing.BasePricing].* " +
                     ", [ProviderPricing.BasePricingTranslation].* " +
                     ", [ProviderPricing.BasePricing.Currency].* " +
                     ", [ProviderPricing.BasePricing.CurrencyTranslation].* " +
                     ", Organization.* " +
                     ", OrganizationTranslation.* " +
                     ", RegionCode.* " +
                     ", Region.* " +
                     ", ClientRegion.* " +
                     ", ClientSubClient.* " +
                     ", SubClient.* " +
                     ", ClientInDebt.* " +
                     ", [Debt].* " +
                     ", ServicePayer.* " +
                     ", Country.* " +
                     ", TermsOfDelivery.* " +
                     ", ClientBankDetails.* " +
                     ", ClientBankDetailAccountNumber.* " +
                     ", [ClientBankDetailAccountNumber.Currency].* " +
                     ", [ClientBankDetailAccountNumber.CurrencyTranslation].* " +
                     ", ClientBankDetailIbanNo.* " +
                     ", [ClientBankDetailIbanNo.Currency].* " +
                     ", [ClientBankDetailIbanNo.CurrencyTranslation].* " +
                     ", PackingMarking.* " +
                     ", PackingMarkingPayment.* " +
                     ", ClientContractDocument.* " +
                     ", [PromotionalPricing].* " +
                     ", [PromotionalPricingCurrency].* " +
                     "FROM Client " +
                     "LEFT OUTER JOIN ClientInRole " +
                     "ON Client.ID = ClientInRole.ClientID " +
                     "LEFT JOIN ClientType " +
                     "ON ClientInRole.ClientTypeID = ClientType.ID " +
                     "LEFT JOIN ClientTypeTranslation " +
                     "ON ClientType.ID = ClientTypeTranslation.ClientTypeID " +
                     "AND ClientTypeTranslation.CultureCode = @Culture " +
                     "AND ClientTypeTranslation.Deleted = 0 " +
                     "LEFT JOIN ClientTypeRole " +
                     "ON ClientInRole.ClientTypeRoleID = ClientTypeRole.ID " +
                     "LEFT JOIN ClientTypeRoleTranslation " +
                     "ON ClientTypeRole.ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
                     "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
                     "AND ClientTypeRoleTranslation.Deleted = 0 " +
                     "LEFT OUTER JOIN ClientUserProfile " +
                     "ON Client.ID = ClientUserProfile.ClientID " +
                     "AND ClientUserProfile.Deleted = 0 " +
                     "LEFT JOIN [User] " +
                     "ON ClientUserProfile.UserProfileID = [User].ID " +
                     "LEFT OUTER JOIN ClientAgreement " +
                     "ON Client.ID = ClientAgreement.ClientID " +
                     "AND ClientAgreement.Deleted = 0 " +
                     "LEFT JOIN [Client] [AmgOriginalClient] " +
                     "ON [AmgOriginalClient].[SourceAmgCode] = [ClientAgreement].[OriginalClientAmgCode] " +
                     "AND [AmgOriginalClient].[SourceAmgCode] != [Client].[SourceAmgCode] " +
                     "AND [AmgOriginalClient].[Name] IS NOT NULL " +
                     "LEFT JOIN [Client] [FenixOriginalClient] " +
                     "ON [FenixOriginalClient].[SourceFenixCode] = [ClientAgreement].[OriginalClientFenixCode] " +
                     "AND [FenixOriginalClient].[SourceFenixCode] != [Client].[SourceFenixCode] " +
                     "AND [FenixOriginalClient].[Name] IS NOT NULL " +
                     "LEFT JOIN Agreement " +
                     "ON ClientAgreement.AgreementID = Agreement.ID " +
                     "LEFT JOIN Pricing AS [Agreement.Pricing] " +
                     "ON Agreement.PricingID = [Agreement.Pricing].ID " +
                     "LEFT OUTER JOIN PricingTranslation AS [Agreement.PricingTranslation] " +
                     "ON [Agreement.Pricing].ID = [Agreement.PricingTranslation].PricingID " +
                     "AND [Agreement.PricingTranslation].CultureCode = @Culture " +
                     "AND [Agreement.PricingTranslation].Deleted = 0 " +
                     "LEFT OUTER JOIN Currency AS [Agreement.Currency] " +
                     "ON Agreement.CurrencyID = [Agreement.Currency].ID " +
                     "LEFT OUTER JOIN CurrencyTranslation AS [Agreement.CurrencyTranslation] " +
                     "ON [Agreement.Currency].ID = [Agreement.CurrencyTranslation].CurrencyID " +
                     "AND [Agreement.CurrencyTranslation].CultureCode = @Culture " +
                     "AND [Agreement.CurrencyTranslation].Deleted = 0 " +
                     "LEFT OUTER JOIN Currency AS [Agreement.Pricing.Currency] " +
                     "ON [Agreement.Pricing].CurrencyID = [Agreement.Pricing.Currency].ID " +
                     "LEFT OUTER JOIN CurrencyTranslation AS [Agreement.Pricing.CurrencyTranslation] " +
                     "ON [Agreement.Pricing.Currency].ID = [Agreement.Pricing.CurrencyTranslation].CurrencyID " +
                     "AND [Agreement.Pricing.CurrencyTranslation].CultureCode = @Culture " +
                     "AND [Agreement.Pricing.CurrencyTranslation].Deleted = 0 " +
                     "LEFT OUTER JOIN ProviderPricing " +
                     "ON Agreement.ProviderPricingID = ProviderPricing.ID " +
                     "LEFT JOIN Pricing AS [ProviderPricing.BasePricing] " +
                     "ON ProviderPricing.BasePricingID = [ProviderPricing.BasePricing].ID " +
                     "LEFT JOIN PricingTranslation AS [ProviderPricing.BasePricingTranslation] " +
                     "ON [ProviderPricing.BasePricing].ID = [ProviderPricing.BasePricingTranslation].PricingID " +
                     "AND [ProviderPricing.BasePricingTranslation].CultureCode = @Culture " +
                     "AND [ProviderPricing.BasePricingTranslation].Deleted = 0 " +
                     "LEFT OUTER JOIN Currency AS [ProviderPricing.BasePricing.Currency] " +
                     "ON [ProviderPricing.BasePricing].CurrencyID = [ProviderPricing.BasePricing.Currency].ID " +
                     "LEFT OUTER JOIN CurrencyTranslation AS [ProviderPricing.BasePricing.CurrencyTranslation] " +
                     "ON [ProviderPricing.BasePricing.Currency].ID = [ProviderPricing.BasePricing.CurrencyTranslation].CurrencyID " +
                     "AND [ProviderPricing.BasePricing.CurrencyTranslation].CultureCode = @Culture " +
                     "AND [ProviderPricing.BasePricing.CurrencyTranslation].Deleted = 0 " +
                     "LEFT JOIN Organization " +
                     "ON Agreement.OrganizationID = Organization.ID " +
                     "LEFT OUTER JOIN OrganizationTranslation " +
                     "ON Organization.ID = OrganizationTranslation.OrganizationID " +
                     "AND OrganizationTranslation.CultureCode = @Culture " +
                     "AND OrganizationTranslation.Deleted = 0 " +
                     "LEFT JOIN RegionCode " +
                     "ON Client.RegionCodeID = RegionCode.ID " +
                     "LEFT JOIN Region " +
                     "ON RegionCode.RegionID = Region.ID " +
                     "LEFT JOIN Region AS ClientRegion " +
                     "ON Client.RegionID = ClientRegion.ID " +
                     "LEFT OUTER JOIN ClientSubClient " +
                     "ON Client.ID = ClientSubClient.RootClientID " +
                     "AND ClientSubClient.Deleted = 0 " +
                     "LEFT OUTER JOIN Client AS SubClient " +
                     "ON ClientSubClient.SubClientID = SubClient.ID " +
                     "LEFT OUTER JOIN ClientInDebt " +
                     "ON ClientInDebt.AgreementID = Agreement.ID " +
                     "AND ClientInDebt.Deleted = 0 " +
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
                     "ON ClientInDebt.DebtID = Debt.ID " +
                     "LEFT JOIN ServicePayer " +
                     "ON Client.ID = ServicePayer.ClientID " +
                     "AND ServicePayer.Deleted = 0 " +
                     "LEFT OUTER JOIN Country " +
                     "ON Country.ID = Client.CountryID " +
                     "AND Country.Deleted = 0 " +
                     "LEFT OUTER JOIN TermsOfDelivery " +
                     "ON TermsOfDelivery.ID = Client.TermsOfDeliveryID " +
                     "AND TermsOfDelivery.Deleted = 0 " +
                     "LEFT OUTER JOIN ClientBankDetails " +
                     "ON ClientBankDetails.ID = Client.ClientBankDetailsID " +
                     "AND ClientBankDetails.Deleted = 0 " +
                     "LEFT OUTER JOIN ClientBankDetailAccountNumber " +
                     "ON ClientBankDetailAccountNumber.ID = ClientBankDetails.AccountNumberID " +
                     "LEFT OUTER JOIN Currency AS [ClientBankDetailAccountNumber.Currency] " +
                     "ON [ClientBankDetailAccountNumber.Currency].ID = ClientBankDetailAccountNumber.CurrencyID " +
                     "LEFT OUTER JOIN CurrencyTranslation AS [ClientBankDetailAccountNumber.CurrencyTranslation] " +
                     "ON [ClientBankDetailAccountNumber.Currency].ID = [ClientBankDetailAccountNumber.CurrencyTranslation].CurrencyID " +
                     "AND [ClientBankDetailAccountNumber.CurrencyTranslation].CultureCode = @Culture " +
                     "AND [ClientBankDetailAccountNumber.CurrencyTranslation].Deleted = 0 " +
                     "LEFT OUTER JOIN ClientBankDetailIbanNo " +
                     "ON ClientBankDetailIbanNo.ID = ClientBankDetails.ClientBankDetailIbanNoID " +
                     "LEFT OUTER JOIN Currency AS [ClientBankDetailIbanNo.Currency] " +
                     "ON [ClientBankDetailIbanNo.Currency].ID = ClientBankDetailIbanNo.CurrencyID " +
                     "LEFT OUTER JOIN CurrencyTranslation AS [ClientBankDetailIbanNo.CurrencyTranslation] " +
                     "ON [ClientBankDetailIbanNo.Currency].ID = [ClientBankDetailIbanNo.CurrencyTranslation].CurrencyID " +
                     "AND [ClientBankDetailIbanNo.CurrencyTranslation].CultureCode = @Culture " +
                     "AND [ClientBankDetailIbanNo.CurrencyTranslation].Deleted = 0 " +
                     "LEFT OUTER JOIN PackingMarking " +
                     "ON PackingMarking.ID = Client.PackingMarkingID " +
                     "AND PackingMarking.Deleted = 0 " +
                     "LEFT OUTER JOIN PackingMarkingPayment " +
                     "ON PackingMarkingPayment.ID = Client.PackingMarkingPaymentID " +
                     "AND PackingMarkingPayment.Deleted = 0 " +
                     "LEFT OUTER JOIN ClientContractDocument " +
                     "ON ClientContractDocument.ClientID = Client.ID " +
                     "AND ClientContractDocument.Deleted = 0 " +
                     "LEFT JOIN [Pricing] AS [PromotionalPricing] " +
                     "ON [PromotionalPricing].[ID] = [Agreement].[PromotionalPricingID] " +
                     "LEFT JOIN [Currency] AS [PromotionalPricingCurrency] " +
                     "ON [PromotionalPricingCurrency].[ID] = [PromotionalPricing].[CurrencyID] " +
                     "WHERE Client.NetUID = @NetId ";
        if (isFromEcommerce) sql += "AND Agreement.ForReSale = 0 ";

        Type[] types = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeTranslation),
            typeof(ClientTypeRole),
            typeof(ClientTypeRoleTranslation),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Organization),
            typeof(OrganizationTranslation),
            typeof(RegionCode),
            typeof(Region),
            typeof(Region),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(ServicePayer),
            typeof(Country),
            typeof(TermsOfDelivery),
            typeof(ClientBankDetails),
            typeof(ClientBankDetailAccountNumber),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ClientBankDetailIbanNo),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(PackingMarking),
            typeof(PackingMarkingPayment),
            typeof(ClientContractDocument),
            typeof(Pricing),
            typeof(Currency)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeTranslation clientTypeTranslation = (ClientTypeTranslation)objects[3];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[4];
            ClientTypeRoleTranslation clientTypeRoleTranslation = (ClientTypeRoleTranslation)objects[5];
            ClientUserProfile clientUserProfile = (ClientUserProfile)objects[6];
            User user = (User)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Agreement agreement = (Agreement)objects[9];
            Pricing pricing = (Pricing)objects[10];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[11];
            Currency agreementCurrency = (Currency)objects[12];
            CurrencyTranslation agreementCurrencyTranslation = (CurrencyTranslation)objects[13];
            Currency pricingCurrency = (Currency)objects[14];
            CurrencyTranslation pricingCurrencyTranslation = (CurrencyTranslation)objects[15];
            ProviderPricing providerPricing = (ProviderPricing)objects[16];
            Pricing providerBasePricing = (Pricing)objects[17];
            PricingTranslation providerBasePricingTranslation = (PricingTranslation)objects[18];
            Currency providerPricingCurrency = (Currency)objects[19];
            CurrencyTranslation providerPricingCurrencyTranslation = (CurrencyTranslation)objects[20];
            Organization organization = (Organization)objects[21];
            OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[22];
            RegionCode regionCode = (RegionCode)objects[23];
            Region region = (Region)objects[24];
            Region clientRegion = (Region)objects[25];
            ClientSubClient clientSubClient = (ClientSubClient)objects[26];
            Client subClient = (Client)objects[27];
            ClientInDebt clientInDebt = (ClientInDebt)objects[28];
            Debt debt = (Debt)objects[29];
            ServicePayer servicePayer = (ServicePayer)objects[30];
            Country country = (Country)objects[31];
            TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[32];
            ClientBankDetails clientBankDetails = (ClientBankDetails)objects[33];
            ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[34];
            Currency clientBankDetailAccountNumberCurrency = (Currency)objects[35];
            CurrencyTranslation clientBankDetailAccountNumberCurrencyTranslation = (CurrencyTranslation)objects[36];
            ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[37];
            Currency clientBankDetailIbanNoCurrency = (Currency)objects[38];
            CurrencyTranslation clientBankDetailIbanNoCurrencyTranslation = (CurrencyTranslation)objects[39];
            PackingMarking packingMarking = (PackingMarking)objects[40];
            PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[41];
            ClientContractDocument clientContractDocument = (ClientContractDocument)objects[42];
            Pricing promotionalPricing = (Pricing)objects[43];
            Currency promotionalPricingCurrency = (Currency)objects[44];

            if (clientInRole != null) {
                if (clientTypeTranslation != null) clientType.Name = clientTypeTranslation.Name;

                if (clientTypeRoleTranslation != null) {
                    clientTypeRole.Name = clientTypeRoleTranslation.Name;
                    clientTypeRole.Description = clientTypeRoleTranslation.Description;
                }

                clientInRole.ClientType = clientType;
                clientInRole.ClientTypeRole = clientTypeRole;

                client.ClientInRole = clientInRole;
            }

            if (clientRegion != null) client.Region = clientRegion;

            if (regionCode != null) {
                regionCode.Region = region;

                client.RegionCode = regionCode;
            }

            if (country != null) client.Country = country;

            if (termsOfDelivery != null) client.TermsOfDelivery = termsOfDelivery;

            if (packingMarking != null) client.PackingMarking = packingMarking;

            if (packingMarkingPayment != null) client.PackingMarkingPayment = packingMarkingPayment;

            if (clientBankDetails != null) {
                if (clientBankDetailAccountNumber != null) {
                    if (clientBankDetailAccountNumberCurrencyTranslation != null)
                        clientBankDetailAccountNumberCurrency.Name = clientBankDetailAccountNumberCurrencyTranslation.Name;

                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;
                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                }

                if (clientBankDetailIbanNo != null) {
                    if (clientBankDetailIbanNoCurrencyTranslation != null) clientBankDetailIbanNoCurrency.Name = clientBankDetailIbanNoCurrencyTranslation.Name;

                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;
                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                }

                client.ClientBankDetails = clientBankDetails;
            }

            if (clientSubClient != null) {
                clientSubClient.SubClient = subClient;

                if (clientToReturn != null) {
                    if (!clientToReturn.SubClients.Any(c => c.Id.Equals(clientSubClient.Id))) clientToReturn.SubClients.Add(clientSubClient);
                } else {
                    client.SubClients.Add(clientSubClient);
                }
            }

            if (clientUserProfile != null) {
                clientUserProfile.UserProfile = user;

                if (clientToReturn == null) {
                    if (!client.ClientManagers.Any(c => c.Id.Equals(clientUserProfile.Id))) client.ClientManagers.Add(clientUserProfile);
                } else {
                    if (!clientToReturn.ClientManagers.Any(c => c.Id.Equals(clientUserProfile.Id))) clientToReturn.ClientManagers.Add(clientUserProfile);
                }
            }

            if (servicePayer != null) {
                if (clientToReturn != null) {
                    if (!clientToReturn.ServicePayers.Any(s => s.Id.Equals(servicePayer.Id))) clientToReturn.ServicePayers.Add(servicePayer);
                } else {
                    if (!client.ServicePayers.Any(s => s.Id.Equals(servicePayer.Id))) client.ServicePayers.Add(servicePayer);
                }
            }

            if (clientAgreement != null) {
                if (agreement != null && clientInDebt != null) {
                    clientInDebt.Debt = debt;

                    if (clientToReturn != null) {
                        if (clientToReturn.ClientAgreements.Any(c => c.Id.Equals(clientAgreement.Id))) {
                            if (!clientToReturn.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id)))
                                clientToReturn.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Add(clientInDebt);
                        } else {
                            if (!agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) agreement.ClientInDebts.Add(clientInDebt);
                        }
                    } else {
                        if (client.ClientAgreements.Any(c => c.Id.Equals(clientAgreement.Id))) {
                            if (!client.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id)))
                                client.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Add(clientInDebt);
                        } else {
                            if (!agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) agreement.ClientInDebts.Add(clientInDebt);
                        }
                    }
                }

                if (clientInDebt != null) {
                    clientInDebt.Debt = debt;

                    if (clientToReturn != null) {
                        if (!clientToReturn.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) clientToReturn.ClientInDebts.Add(clientInDebt);
                    } else {
                        client.ClientInDebts.Add(clientInDebt);
                    }
                }

                if (agreementCurrencyTranslation != null) agreementCurrency.Name = agreementCurrencyTranslation.Name;

                if (pricing != null) {
                    if (pricingTranslation != null) pricing.Name = pricingTranslation.Name;

                    if (pricingCurrencyTranslation != null) pricingCurrency.Name = pricingCurrencyTranslation.Name;

                    pricing.Currency = pricingCurrency;

                    agreement.Pricing = pricing;
                }

                if (promotionalPricing != null) {
                    promotionalPricing.Currency = promotionalPricingCurrency;

                    agreement.PromotionalPricing = promotionalPricing;
                }

                if (providerPricing != null) {
                    if (providerBasePricingTranslation != null) providerBasePricing.Name = providerBasePricingTranslation.Name;

                    if (providerPricingCurrencyTranslation != null) providerPricingCurrency.Name = providerPricingCurrencyTranslation.Name;

                    providerPricing.Currency = providerPricingCurrency;
                    providerPricing.Pricing = providerBasePricing;

                    agreement.ProviderPricing = providerPricing;
                }

                if (organizationTranslation != null) organization.Name = organizationTranslation.Name;

                agreement.Currency = agreementCurrency;
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                if (clientToReturn == null) {
                    client.ClientAgreements.Add(clientAgreement);
                } else {
                    if (!clientToReturn.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) clientToReturn.ClientAgreements.Add(clientAgreement);
                }
            }

            if (clientContractDocument != null) {
                if (clientToReturn == null) {
                    client.ClientContractDocuments.Add(clientContractDocument);
                } else {
                    if (!clientToReturn.ClientContractDocuments.Any(d => d.Id.Equals(clientContractDocument.Id)))
                        clientToReturn.ClientContractDocuments.Add(clientContractDocument);
                }
            }

            if (clientToReturn == null) clientToReturn = client;

            return client;
        };

        var props = new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sql, types, mapper, props);

        if (clientToReturn != null && clientToReturn.ClientAgreements.Any()) {
            string sqlQueryProductGroupsDiscounts = "SELECT * FROM ProductGroupDiscount " +
                                                    "LEFT OUTER JOIN ProductGroup " +
                                                    "ON ProductGroupDiscount.ProductGroupID = ProductGroup.ID " +
                                                    "LEFT OUTER JOIN ProductSubGroup " +
                                                    "ON ProductSubGroup.SubProductGroupID = ProductGroup.ID " +
                                                    "AND [ProductSubGroup].[Deleted] = 0  " +
                                                    "LEFT OUTER JOIN ProductGroup AS RootProductGroup " +
                                                    "ON RootProductGroup.ID = ProductSubGroup.RootProductGroupID " +
                                                    "LEFT OUTER JOIN ProductGroupDiscount AS RootProductGroupDiscount " +
                                                    "ON RootProductGroupDiscount.ProductGroupID = RootProductGroup.ID " +
                                                    "AND RootProductGroupDiscount.ClientAgreementID IN @Ids " +
                                                    "WHERE ProductGroupDiscount.ClientAgreementID IN @Ids " +
                                                    "AND ProductGroupDiscount.Deleted = 0 " +
                                                    "ORDER BY RootProductGroup.Name ASC, ProductGroup.Name ASC ";

            Type[] typeProductGroupDiscounts = {
                typeof(ProductGroupDiscount),
                typeof(ProductGroup),
                typeof(ProductSubGroup),
                typeof(ProductGroup),
                typeof(ProductGroupDiscount)
            };

            Func<object[], ProductGroupDiscount> mapperProductGroupDiscounts = objects => {
                ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[0];
                ProductGroup productGroup = (ProductGroup)objects[1];
                ProductSubGroup productSubGroup = (ProductSubGroup)objects[2];
                ProductGroup rootProductGroup = (ProductGroup)objects[3];
                ProductGroupDiscount rootProductGroupDiscount = (ProductGroupDiscount)objects[4];

                ClientAgreement clientAgreement = clientToReturn.ClientAgreements.First(x => x.Id.Equals(productGroupDiscount.ClientAgreementId));

                if (productSubGroup != null) {
                    if (clientToReturn != null) {
                        if (clientToReturn.ClientAgreements.Any(a =>
                                a.Id.Equals(clientAgreement.Id) && a.ProductGroupDiscounts.Any(d => d.Id.Equals(rootProductGroupDiscount.Id)))) {
                            if (!clientToReturn.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id))
                                    .ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroupDiscount.Id))
                                    .SubProductGroupDiscounts.Any(s => s.Id.Equals(productGroupDiscount.Id))) {
                                productGroupDiscount.ProductGroup = productGroup;

                                clientToReturn.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id))
                                    .ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroupDiscount.Id)).SubProductGroupDiscounts.Add(productGroupDiscount);
                            }
                        } else {
                            productGroupDiscount.ProductGroup = productGroup;
                            rootProductGroupDiscount.ProductGroup = rootProductGroup;
                            rootProductGroupDiscount.SubProductGroupDiscounts.Add(productGroupDiscount);

                            if (clientToReturn.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id)))
                                clientToReturn.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id)).ProductGroupDiscounts.Add(rootProductGroupDiscount);
                            else
                                clientAgreement.ProductGroupDiscounts.Add(rootProductGroupDiscount);
                        }
                    } else {
                        if (clientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(rootProductGroupDiscount.Id))) {
                            if (!clientAgreement.ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroupDiscount.Id)).SubProductGroupDiscounts
                                    .Any(a => a.Id.Equals(productGroupDiscount.Id))) {
                                productGroupDiscount.ProductGroup = productGroup;

                                clientAgreement.ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroup.Id)).SubProductGroupDiscounts.Add(productGroupDiscount);
                            }
                        } else {
                            productGroupDiscount.ProductGroup = productGroup;
                            rootProductGroupDiscount.ProductGroup = rootProductGroup;
                            rootProductGroupDiscount.SubProductGroupDiscounts.Add(productGroupDiscount);

                            clientAgreement.ProductGroupDiscounts.Add(rootProductGroupDiscount);
                        }
                    }
                } else {
                    productGroupDiscount.ProductGroup = productGroup;

                    if (clientToReturn == null) {
                        clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);
                    } else {
                        if (clientToReturn.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) {
                            if (!clientToReturn.ClientAgreements.Any(a =>
                                    a.Id.Equals(clientAgreement.Id) && a.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id))))
                                clientToReturn.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id)).ProductGroupDiscounts.Add(productGroupDiscount);
                        } else {
                            clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);
                        }
                    }
                }

                return productGroupDiscount;
            };

            _connection.Query(
                sqlQueryProductGroupsDiscounts,
                typeProductGroupDiscounts,
                mapperProductGroupDiscounts,
                new { Ids = clientToReturn.ClientAgreements.Select(x => x.Id) });
        }

        if (clientToReturn == null || clientToReturn.ClientInRole == null) return clientToReturn;

        List<PerfectClient> perfectClients = new();

        _connection.Query<PerfectClient, PerfectClientTranslation, PerfectClientValue, PerfectClientValueTranslation, ClientPerfectClient, PerfectClient>(
            "SELECT * FROM PerfectClient " +
            "LEFT JOIN PerfectClientTranslation " +
            "ON PerfectClient.ID = PerfectClientTranslation.PerfectClientID " +
            "AND PerfectClientTranslation.CultureCode = @Culture AND PerfectClientTranslation.Deleted = 0 " +
            "LEFT OUTER JOIN PerfectClientValue " +
            "ON PerfectClient.ID = PerfectClientValue.PerfectClientID AND PerfectClientValue.Deleted = 0 " +
            "LEFT JOIN PerfectClientValueTranslation " +
            "ON PerfectClientValue.ID = PerfectClientValueTranslation.PerfectClientValueID " +
            "AND PerfectClientValueTranslation.CultureCode = @Culture AND PerfectClientValueTranslation.Deleted = 0 " +
            "LEFT OUTER JOIN ClientPerfectClient " +
            "ON PerfectClient.ID = ClientPerfectClient.PerfectClientID " +
            "AND ClientPerfectClient.ClientID = @ClientId AND ClientPerfectClient.Deleted = 0 " +
            "WHERE PerfectClient.Deleted = 0 ",
            //"AND PerfectClient.ClientTypeRoleID = @ClientRoleId",
            (client, clientTranslation, clientValue, clientValueTranslation, junction) => {
                if (clientTranslation != null) {
                    client.Lable = clientTranslation.Name;
                    client.Description = clientTranslation.Description;
                }

                if (clientValue != null) {
                    if (junction != null && junction.PerfectClientValueId.Equals(clientValue.Id)) {
                        if (clientValueTranslation != null) clientValue.Value = clientValueTranslation.Value;

                        clientValue.IsSelected = junction.IsChecked;

                        if (perfectClients.Any(p => p.Id.Equals(client.Id))) {
                            perfectClients.First(p => p.Id.Equals(client.Id)).Values.Add(clientValue);
                            perfectClients.First(p => p.Id.Equals(client.Id)).Value = junction.Value;
                            perfectClients.First(p => p.Id.Equals(client.Id)).IsSelected = junction.IsChecked;
                        } else {
                            client.Value = junction.Value;
                            client.IsSelected = junction.IsChecked;
                            client.Values.Add(clientValue);

                            perfectClients.Add(client);
                        }
                    } else {
                        if (clientValueTranslation != null) clientValue.Value = clientValueTranslation.Value;

                        if (perfectClients.Any(p => p.Id.Equals(client.Id))) {
                            perfectClients.First(p => p.Id.Equals(client.Id)).Values.Add(clientValue);
                        } else {
                            client.Values.Add(clientValue);

                            perfectClients.Add(client);
                        }
                    }
                } else {
                    if (junction != null && junction.PerfectClientId.Equals(client.Id)) {
                        client.IsSelected = junction.IsChecked;
                        client.Value = junction.Value;

                        perfectClients.Add(client);
                    } else {
                        perfectClients.Add(client);
                    }
                }

                return client;
            },
            new {
                ClientId = clientToReturn.Id,
                ClientRoleId = clientToReturn.ClientInRole.ClientTypeRoleId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        clientToReturn.PerfectClients = perfectClients;

        return clientToReturn;
    }

    public Client GetByIdWithAllIncludes(long id) {
        Client clientToReturn = null;

        string sql =
            "SELECT * FROM Client " +
            "LEFT OUTER JOIN ClientInRole " +
            "ON Client.ID = ClientInRole.ClientID " +
            "LEFT JOIN ClientType " +
            "ON ClientInRole.ClientTypeID = ClientType.ID " +
            "LEFT JOIN ClientTypeTranslation " +
            "ON ClientType.ID = ClientTypeTranslation.ClientTypeID " +
            "AND ClientTypeTranslation.CultureCode = @Culture " +
            "AND ClientTypeTranslation.Deleted = 0 " +
            "LEFT JOIN ClientTypeRole " +
            "ON ClientInRole.ClientTypeRoleID = ClientTypeRole.ID " +
            "LEFT JOIN ClientTypeRoleTranslation " +
            "ON ClientTypeRole.ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
            "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
            "AND ClientTypeRoleTranslation.Deleted = 0 " +
            "LEFT OUTER JOIN ClientUserProfile " +
            "ON Client.ID = ClientUserProfile.ClientID " +
            "AND ClientUserProfile.Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON ClientUserProfile.UserProfileID = [User].ID " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON Client.ID = ClientAgreement.ClientID " +
            "AND ClientAgreement.Deleted = 0 " +
            "LEFT JOIN Agreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN Pricing AS [Agreement.Pricing] " +
            "ON Agreement.PricingID = [Agreement.Pricing].ID " +
            "LEFT OUTER JOIN PricingTranslation AS [Agreement.PricingTranslation] " +
            "ON [Agreement.Pricing].ID = [Agreement.PricingTranslation].PricingID " +
            "AND [Agreement.PricingTranslation].CultureCode = @Culture " +
            "AND [Agreement.PricingTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN Currency AS [Agreement.Currency] " +
            "ON Agreement.CurrencyID = [Agreement.Currency].ID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [Agreement.CurrencyTranslation] " +
            "ON [Agreement.Currency].ID = [Agreement.CurrencyTranslation].CurrencyID " +
            "AND [Agreement.CurrencyTranslation].CultureCode = @Culture " +
            "AND [Agreement.CurrencyTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN Currency AS [Agreement.Pricing.Currency] " +
            "ON [Agreement.Pricing].CurrencyID = [Agreement.Pricing.Currency].ID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [Agreement.Pricing.CurrencyTranslation] " +
            "ON [Agreement.Pricing.Currency].ID = [Agreement.Pricing.CurrencyTranslation].CurrencyID " +
            "AND [Agreement.Pricing.CurrencyTranslation].CultureCode = @Culture " +
            "AND [Agreement.Pricing.CurrencyTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN ProviderPricing " +
            "ON Agreement.ProviderPricingID = ProviderPricing.ID " +
            "LEFT JOIN Pricing AS [ProviderPricing.BasePricing] " +
            "ON ProviderPricing.BasePricingID = [ProviderPricing.BasePricing].ID " +
            "LEFT JOIN PricingTranslation AS [ProviderPricing.BasePricingTranslation] " +
            "ON [ProviderPricing.BasePricing].ID = [ProviderPricing.BasePricingTranslation].PricingID " +
            "AND [ProviderPricing.BasePricingTranslation].CultureCode = @Culture " +
            "AND [ProviderPricing.BasePricingTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN Currency AS [ProviderPricing.BasePricing.Currency] " +
            "ON [ProviderPricing.BasePricing].CurrencyID = [ProviderPricing.BasePricing.Currency].ID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [ProviderPricing.BasePricing.CurrencyTranslation] " +
            "ON [ProviderPricing.BasePricing.Currency].ID = [ProviderPricing.BasePricing.CurrencyTranslation].CurrencyID " +
            "AND [ProviderPricing.BasePricing.CurrencyTranslation].CultureCode = @Culture " +
            "AND [ProviderPricing.BasePricing.CurrencyTranslation].Deleted = 0 " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "LEFT OUTER JOIN OrganizationTranslation " +
            "ON Organization.ID = OrganizationTranslation.OrganizationID " +
            "AND OrganizationTranslation.CultureCode = @Culture " +
            "AND OrganizationTranslation.Deleted = 0 " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "LEFT JOIN Region " +
            "ON RegionCode.RegionID = Region.ID " +
            "LEFT JOIN Region AS ClientRegion " +
            "ON Client.RegionID = ClientRegion.ID " +
            "LEFT OUTER JOIN ClientSubClient " +
            "ON Client.ID = ClientSubClient.RootClientID " +
            "AND ClientSubClient.Deleted = 0 " +
            "LEFT OUTER JOIN Client AS SubClient " +
            "ON ClientSubClient.SubClientID = SubClient.ID " +
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
            "LEFT OUTER JOIN ClientInDebt " +
            "ON ClientInDebt.AgreementID = Agreement.ID " +
            "AND ClientInDebt.Deleted = 0 " +
            "LEFT OUTER JOIN Debt " +
            "ON ClientInDebt.DebtID = Debt.ID " +
            "LEFT JOIN ServicePayer " +
            "ON Client.ID = ServicePayer.ClientID " +
            "AND ServicePayer.Deleted = 0 " +
            "LEFT OUTER JOIN Country " +
            "ON Country.ID = Client.CountryID " +
            "AND Country.Deleted = 0 " +
            "LEFT OUTER JOIN TermsOfDelivery " +
            "ON TermsOfDelivery.ID = Client.TermsOfDeliveryID " +
            "AND TermsOfDelivery.Deleted = 0 " +
            "LEFT OUTER JOIN ClientBankDetails " +
            "ON ClientBankDetails.ID = Client.ClientBankDetailsID " +
            "AND ClientBankDetails.Deleted = 0 " +
            "LEFT OUTER JOIN ClientBankDetailAccountNumber " +
            "ON ClientBankDetailAccountNumber.ID = ClientBankDetails.AccountNumberID " +
            "LEFT OUTER JOIN Currency AS [ClientBankDetailAccountNumber.Currency] " +
            "ON [ClientBankDetailAccountNumber.Currency].ID = ClientBankDetailAccountNumber.CurrencyID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [ClientBankDetailAccountNumber.CurrencyTranslation] " +
            "ON [ClientBankDetailAccountNumber.Currency].ID = [ClientBankDetailAccountNumber.CurrencyTranslation].CurrencyID " +
            "AND [ClientBankDetailAccountNumber.CurrencyTranslation].CultureCode = @Culture " +
            "AND [ClientBankDetailAccountNumber.CurrencyTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN ClientBankDetailIbanNo " +
            "ON ClientBankDetailIbanNo.ID = ClientBankDetails.ClientBankDetailIbanNoID " +
            "LEFT OUTER JOIN Currency AS [ClientBankDetailIbanNo.Currency] " +
            "ON [ClientBankDetailIbanNo.Currency].ID = ClientBankDetailIbanNo.CurrencyID " +
            "LEFT OUTER JOIN CurrencyTranslation AS [ClientBankDetailIbanNo.CurrencyTranslation] " +
            "ON [ClientBankDetailIbanNo.Currency].ID = [ClientBankDetailIbanNo.CurrencyTranslation].CurrencyID " +
            "AND [ClientBankDetailIbanNo.CurrencyTranslation].CultureCode = @Culture " +
            "AND [ClientBankDetailIbanNo.CurrencyTranslation].Deleted = 0 " +
            "LEFT OUTER JOIN PackingMarking " +
            "ON PackingMarking.ID = Client.PackingMarkingID " +
            "AND PackingMarking.Deleted = 0 " +
            "LEFT OUTER JOIN PackingMarkingPayment " +
            "ON PackingMarkingPayment.ID = Client.PackingMarkingPaymentID " +
            "AND PackingMarkingPayment.Deleted = 0 " +
            "LEFT OUTER JOIN ClientContractDocument " +
            "ON ClientContractDocument.ClientID = Client.ID " +
            "AND ClientContractDocument.Deleted = 0 " +
            "WHERE Client.ID = @Id " +
            "ORDER BY RootProductGroup.Name ASC, ProductGroup.Name ASC";

        Type[] types = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeTranslation),
            typeof(ClientTypeRole),
            typeof(ClientTypeRoleTranslation),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Organization),
            typeof(OrganizationTranslation),
            typeof(RegionCode),
            typeof(Region),
            typeof(Region),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(ProductGroupDiscount),
            typeof(ProductGroup),
            typeof(ProductSubGroup),
            typeof(ProductGroup),
            typeof(ProductGroupDiscount),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(ServicePayer),
            typeof(Country),
            typeof(TermsOfDelivery),
            typeof(ClientBankDetails),
            typeof(ClientBankDetailAccountNumber),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(ClientBankDetailIbanNo),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(PackingMarking),
            typeof(PackingMarkingPayment),
            typeof(ClientContractDocument)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeTranslation clientTypeTranslation = (ClientTypeTranslation)objects[3];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[4];
            ClientTypeRoleTranslation clientTypeRoleTranslation = (ClientTypeRoleTranslation)objects[5];
            ClientUserProfile clientUserProfile = (ClientUserProfile)objects[6];
            User user = (User)objects[7];
            ClientAgreement clientAgreement = (ClientAgreement)objects[8];
            Agreement agreement = (Agreement)objects[9];
            Pricing pricing = (Pricing)objects[10];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[11];
            Currency agreementCurrency = (Currency)objects[12];
            CurrencyTranslation agreementCurrencyTranslation = (CurrencyTranslation)objects[13];
            Currency pricingCurrency = (Currency)objects[14];
            CurrencyTranslation pricingCurrencyTranslation = (CurrencyTranslation)objects[15];
            ProviderPricing providerPricing = (ProviderPricing)objects[16];
            Pricing providerBasePricing = (Pricing)objects[17];
            PricingTranslation providerBasePricingTranslation = (PricingTranslation)objects[18];
            Currency providerPricingCurrency = (Currency)objects[19];
            CurrencyTranslation providerPricingCurrencyTranslation = (CurrencyTranslation)objects[20];
            Organization organization = (Organization)objects[21];
            OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[22];
            RegionCode regionCode = (RegionCode)objects[23];
            Region region = (Region)objects[24];
            Region clientRegion = (Region)objects[25];
            ClientSubClient clientSubClient = (ClientSubClient)objects[26];
            Client subClient = (Client)objects[27];
            ProductGroupDiscount productGroupDiscount = (ProductGroupDiscount)objects[28];
            ProductGroup productGroup = (ProductGroup)objects[29];
            ProductSubGroup productSubGroup = (ProductSubGroup)objects[30];
            ProductGroup rootProductGroup = (ProductGroup)objects[31];
            ProductGroupDiscount rootProductGroupDiscount = (ProductGroupDiscount)objects[32];
            ClientInDebt clientInDebt = (ClientInDebt)objects[33];
            Debt debt = (Debt)objects[34];
            ServicePayer servicePayer = (ServicePayer)objects[35];
            Country country = (Country)objects[36];
            TermsOfDelivery termsOfDelivery = (TermsOfDelivery)objects[37];
            ClientBankDetails clientBankDetails = (ClientBankDetails)objects[38];
            ClientBankDetailAccountNumber clientBankDetailAccountNumber = (ClientBankDetailAccountNumber)objects[39];
            Currency clientBankDetailAccountNumberCurrency = (Currency)objects[40];
            CurrencyTranslation clientBankDetailAccountNumberCurrencyTranslation = (CurrencyTranslation)objects[41];
            ClientBankDetailIbanNo clientBankDetailIbanNo = (ClientBankDetailIbanNo)objects[42];
            Currency clientBankDetailIbanNoCurrency = (Currency)objects[43];
            CurrencyTranslation clientBankDetailIbanNoCurrencyTranslation = (CurrencyTranslation)objects[44];
            PackingMarking packingMarking = (PackingMarking)objects[45];
            PackingMarkingPayment packingMarkingPayment = (PackingMarkingPayment)objects[46];
            ClientContractDocument clientContractDocument = (ClientContractDocument)objects[47];

            if (clientInRole != null) {
                if (clientTypeTranslation != null) clientType.Name = clientTypeTranslation.Name;

                if (clientTypeRoleTranslation != null) {
                    clientTypeRole.Name = clientTypeRoleTranslation.Name;
                    clientTypeRole.Description = clientTypeRoleTranslation.Description;
                }

                clientInRole.ClientType = clientType;
                clientInRole.ClientTypeRole = clientTypeRole;

                client.ClientInRole = clientInRole;
            }

            if (clientRegion != null) client.Region = clientRegion;

            if (regionCode != null) {
                regionCode.Region = region;

                client.RegionCode = regionCode;
            }

            if (country != null) client.Country = country;

            if (termsOfDelivery != null) client.TermsOfDelivery = termsOfDelivery;

            if (packingMarking != null) client.PackingMarking = packingMarking;

            if (packingMarkingPayment != null) client.PackingMarkingPayment = packingMarkingPayment;

            if (clientBankDetails != null) {
                if (clientBankDetailAccountNumber != null) {
                    if (clientBankDetailAccountNumberCurrencyTranslation != null)
                        clientBankDetailAccountNumberCurrency.Name = clientBankDetailAccountNumberCurrencyTranslation.Name;

                    clientBankDetailAccountNumber.Currency = clientBankDetailAccountNumberCurrency;
                    clientBankDetails.AccountNumber = clientBankDetailAccountNumber;
                }

                if (clientBankDetailIbanNo != null) {
                    if (clientBankDetailIbanNoCurrencyTranslation != null) clientBankDetailIbanNoCurrency.Name = clientBankDetailIbanNoCurrencyTranslation.Name;

                    clientBankDetailIbanNo.Currency = clientBankDetailIbanNoCurrency;
                    clientBankDetails.ClientBankDetailIbanNo = clientBankDetailIbanNo;
                }

                client.ClientBankDetails = clientBankDetails;
            }

            if (clientSubClient != null) {
                clientSubClient.SubClient = subClient;

                if (clientToReturn != null) {
                    if (!clientToReturn.SubClients.Any(c => c.Id.Equals(clientSubClient.Id))) clientToReturn.SubClients.Add(clientSubClient);
                } else {
                    client.SubClients.Add(clientSubClient);
                }
            }

            if (clientUserProfile != null) {
                clientUserProfile.UserProfile = user;

                if (clientToReturn == null) {
                    if (!client.ClientManagers.Any(c => c.Id.Equals(clientUserProfile.Id))) client.ClientManagers.Add(clientUserProfile);
                } else {
                    if (!clientToReturn.ClientManagers.Any(c => c.Id.Equals(clientUserProfile.Id))) clientToReturn.ClientManagers.Add(clientUserProfile);
                }
            }

            if (servicePayer != null) {
                if (clientToReturn != null) {
                    if (!clientToReturn.ServicePayers.Any(s => s.Id.Equals(servicePayer.Id))) clientToReturn.ServicePayers.Add(servicePayer);
                } else {
                    if (!client.ServicePayers.Any(s => s.Id.Equals(servicePayer.Id))) client.ServicePayers.Add(servicePayer);
                }
            }

            if (clientAgreement != null) {
                if (agreement != null && clientInDebt != null) {
                    clientInDebt.Debt = debt;

                    if (clientToReturn != null) {
                        if (clientToReturn.ClientAgreements.Any(c => c.Id.Equals(clientAgreement.Id))) {
                            if (!clientToReturn.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id)))
                                clientToReturn.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Add(clientInDebt);
                        } else {
                            if (!agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) agreement.ClientInDebts.Add(clientInDebt);
                        }
                    } else {
                        if (client.ClientAgreements.Any(c => c.Id.Equals(clientAgreement.Id))) {
                            if (!client.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id)))
                                client.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Add(clientInDebt);
                        } else {
                            if (!agreement.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) agreement.ClientInDebts.Add(clientInDebt);
                        }
                    }
                }

                if (clientInDebt != null) {
                    clientInDebt.Debt = debt;

                    if (clientToReturn != null) {
                        if (!clientToReturn.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) clientToReturn.ClientInDebts.Add(clientInDebt);
                    } else {
                        client.ClientInDebts.Add(clientInDebt);
                    }
                }

                if (productGroupDiscount != null) {
                    if (productSubGroup != null) {
                        if (clientToReturn != null) {
                            if (clientToReturn.ClientAgreements.Any(a =>
                                    a.Id.Equals(clientAgreement.Id) && a.ProductGroupDiscounts.Any(d => d.Id.Equals(rootProductGroupDiscount.Id)))) {
                                if (!clientToReturn.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id))
                                        .ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroupDiscount.Id))
                                        .SubProductGroupDiscounts.Any(s => s.Id.Equals(productGroupDiscount.Id))) {
                                    productGroupDiscount.ProductGroup = productGroup;

                                    clientToReturn.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id))
                                        .ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroupDiscount.Id)).SubProductGroupDiscounts.Add(productGroupDiscount);
                                }
                            } else {
                                productGroupDiscount.ProductGroup = productGroup;
                                rootProductGroupDiscount.ProductGroup = rootProductGroup;
                                rootProductGroupDiscount.SubProductGroupDiscounts.Add(productGroupDiscount);

                                if (clientToReturn.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id)))
                                    clientToReturn.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id)).ProductGroupDiscounts.Add(rootProductGroupDiscount);
                                else
                                    clientAgreement.ProductGroupDiscounts.Add(rootProductGroupDiscount);
                            }
                        } else {
                            if (clientAgreement.ProductGroupDiscounts.Any(d => d.Id.Equals(rootProductGroupDiscount.Id))) {
                                if (!clientAgreement.ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroupDiscount.Id)).SubProductGroupDiscounts
                                        .Any(a => a.Id.Equals(productGroupDiscount.Id))) {
                                    productGroupDiscount.ProductGroup = productGroup;

                                    clientAgreement.ProductGroupDiscounts.First(d => d.Id.Equals(rootProductGroup.Id)).SubProductGroupDiscounts.Add(productGroupDiscount);
                                }
                            } else {
                                productGroupDiscount.ProductGroup = productGroup;
                                rootProductGroupDiscount.ProductGroup = rootProductGroup;
                                rootProductGroupDiscount.SubProductGroupDiscounts.Add(productGroupDiscount);

                                clientAgreement.ProductGroupDiscounts.Add(rootProductGroupDiscount);
                            }
                        }
                    } else {
                        productGroupDiscount.ProductGroup = productGroup;

                        if (clientToReturn == null) {
                            clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);
                        } else {
                            if (clientToReturn.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) {
                                if (!clientToReturn.ClientAgreements.Any(a =>
                                        a.Id.Equals(clientAgreement.Id) && a.ProductGroupDiscounts.Any(d => d.Id.Equals(productGroupDiscount.Id))))
                                    clientToReturn.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id)).ProductGroupDiscounts.Add(productGroupDiscount);
                            } else {
                                clientAgreement.ProductGroupDiscounts.Add(productGroupDiscount);
                            }
                        }
                    }
                }

                if (agreementCurrencyTranslation != null) agreementCurrency.Name = agreementCurrencyTranslation.Name;

                if (pricing != null) {
                    if (pricingTranslation != null) pricing.Name = pricingTranslation.Name;

                    if (pricingCurrencyTranslation != null) pricingCurrency.Name = pricingCurrencyTranslation.Name;

                    pricing.Currency = pricingCurrency;

                    agreement.Pricing = pricing;
                }

                if (providerPricing != null) {
                    if (providerBasePricingTranslation != null) providerBasePricing.Name = providerBasePricingTranslation.Name;

                    if (providerPricingCurrencyTranslation != null) providerPricingCurrency.Name = providerPricingCurrencyTranslation.Name;

                    providerPricing.Currency = providerPricingCurrency;
                    providerPricing.Pricing = providerBasePricing;

                    agreement.ProviderPricing = providerPricing;
                }

                if (organizationTranslation != null) organization.Name = organizationTranslation.Name;

                agreement.Currency = agreementCurrency;
                agreement.Organization = organization;

                clientAgreement.Agreement = agreement;

                if (clientToReturn == null) {
                    client.ClientAgreements.Add(clientAgreement);
                } else {
                    if (!clientToReturn.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) clientToReturn.ClientAgreements.Add(clientAgreement);
                }
            }

            if (clientContractDocument != null) {
                if (clientToReturn == null) {
                    client.ClientContractDocuments.Add(clientContractDocument);
                } else {
                    if (!clientToReturn.ClientContractDocuments.Any(d => d.Id.Equals(clientContractDocument.Id)))
                        clientToReturn.ClientContractDocuments.Add(clientContractDocument);
                }
            }

            if (clientToReturn == null) clientToReturn = client;

            return client;
        };

        var props = new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sql, types, mapper, props);

        if (clientToReturn != null) {
            List<PerfectClient> perfectClients = new();

            _connection.Query<PerfectClient, PerfectClientTranslation, PerfectClientValue, PerfectClientValueTranslation, ClientPerfectClient, PerfectClient>(
                "SELECT * FROM PerfectClient " +
                "LEFT JOIN PerfectClientTranslation " +
                "ON PerfectClient.ID = PerfectClientTranslation.PerfectClientID " +
                "AND PerfectClientTranslation.CultureCode = @Culture AND PerfectClientTranslation.Deleted = 0 " +
                "LEFT OUTER JOIN PerfectClientValue " +
                "ON PerfectClient.ID = PerfectClientValue.PerfectClientID AND PerfectClientValue.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation " +
                "ON PerfectClientValue.ID = PerfectClientValueTranslation.PerfectClientValueID " +
                "AND PerfectClientValueTranslation.CultureCode = @Culture AND PerfectClientValueTranslation.Deleted = 0 " +
                "LEFT OUTER JOIN ClientPerfectClient " +
                "ON PerfectClient.ID = ClientPerfectClient.PerfectClientID " +
                "AND ClientPerfectClient.ClientID = @ClientId AND ClientPerfectClient.Deleted = 0 " +
                "WHERE PerfectClient.Deleted = 0 " +
                "AND PerfectClient.ClientTypeRoleID = @ClientRoleId",
                (client, clientTranslation, clientValue, clientValueTranslation, junction) => {
                    if (clientTranslation != null) {
                        client.Lable = clientTranslation.Name;
                        client.Description = clientTranslation.Description;
                    }

                    if (clientValue != null) {
                        if (junction != null && junction.PerfectClientValueId.Equals(clientValue.Id)) {
                            if (clientValueTranslation != null) clientValue.Value = clientValueTranslation.Value;

                            clientValue.IsSelected = junction.IsChecked;

                            if (perfectClients.Any(p => p.Id.Equals(client.Id))) {
                                perfectClients.First(p => p.Id.Equals(client.Id)).Values.Add(clientValue);
                                perfectClients.First(p => p.Id.Equals(client.Id)).Value = junction.Value;
                                perfectClients.First(p => p.Id.Equals(client.Id)).IsSelected = junction.IsChecked;
                            } else {
                                client.Value = junction.Value;
                                client.IsSelected = junction.IsChecked;
                                client.Values.Add(clientValue);

                                perfectClients.Add(client);
                            }
                        } else {
                            if (clientValueTranslation != null) clientValue.Value = clientValueTranslation.Value;

                            if (perfectClients.Any(p => p.Id.Equals(client.Id))) {
                                perfectClients.First(p => p.Id.Equals(client.Id)).Values.Add(clientValue);
                            } else {
                                client.Values.Add(clientValue);

                                perfectClients.Add(client);
                            }
                        }
                    } else {
                        if (junction != null && junction.PerfectClientId.Equals(client.Id)) {
                            client.IsSelected = junction.IsChecked;
                            client.Value = junction.Value;

                            perfectClients.Add(client);
                        } else {
                            perfectClients.Add(client);
                        }
                    }

                    return client;
                },
                new {
                    ClientId = clientToReturn.Id,
                    ClientRoleId = clientToReturn.ClientInRole.ClientTypeRoleId,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );

            clientToReturn.PerfectClients = perfectClients;
        }

        return clientToReturn;
    }

    public long GetFilteredCount(string sql, string value) {
        return _connection.Query<long>(
                "SELECT COUNT(Client.ID) FROM Client " +
                "LEFT OUTER JOIN ClientInRole " +
                "ON Client.ID = ClientInRole.ClientID " +
                "LEFT JOIN ClientType AS [ClientInRole.ClientType] " +
                "ON ClientInRole.ClientTypeID = [ClientInRole.ClientType].ID " +
                "LEFT JOIN ClientTypeTranslation " +
                "ON [ClientInRole.ClientType].ID = ClientTypeTranslation.ClientTypeID " +
                "AND ClientTypeTranslation.CultureCode = @Culture " +
                "AND ClientTypeTranslation.Deleted = 0 " +
                "LEFT JOIN ClientTypeRole AS [ClientInRole.ClientTypeRole] " +
                "ON [ClientInRole.ClientTypeRole].ID = ClientInRole.ClientTypeRoleID " +
                "LEFT JOIN ClientTypeRoleTranslation " +
                "ON [ClientInRole.ClientTypeRole].ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
                "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
                "AND ClientTypeRoleTranslation.Deleted = 0 " +
                "LEFT OUTER JOIN ClientAgreement " +
                "ON Client.ID = ClientAgreement.ClientID " +
                "AND ClientAgreement.Deleted = 0 " +
                "LEFT OUTER JOIN Agreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID AND Agreement.IsActive = 1 " +
                "LEFT JOIN Pricing AS [Agreement.Pricing] " +
                "ON Agreement.PricingID = [Agreement.Pricing].ID " +
                "LEFT JOIN PricingTranslation AS [Agreement.Pricing.Translation] " +
                "ON [Agreement.Pricing].ID = [Agreement.Pricing.Translation].PricingID " +
                "AND [Agreement.Pricing.Translation].CultureCode = @Culture " +
                "LEFT JOIN ProviderPricing AS [Agreement.ProviderPricing]  " +
                "ON Agreement.ProviderPricingID = [Agreement.ProviderPricing].ID " +
                "LEFT JOIN Pricing AS [ProviderPricing.Pricing] " +
                "ON [Agreement.ProviderPricing].BasePricingID = [ProviderPricing.Pricing].ID " +
                "LEFT JOIN PricingTranslation AS [ProviderPricing.Pricing.Translation] " +
                "ON [ProviderPricing.Pricing].ID = [ProviderPricing.Pricing.Translation].PricingID " +
                "AND [ProviderPricing.Pricing.Translation].CultureCode = @Culture " +
                sql,
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Value = value }
            )
            .Single();
    }

    public List<Client> GetAllNewClientsFromECommerce() {
        List<Client> toReturn = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [Client] " +
            "LEFT JOIN [Region] " +
            "ON [Region].ID = [Client].RegionID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "LEFT JOIN (" +
            "SELECT [ClientType].[ID] " +
            ",[ClientType].[Created] " +
            ",[ClientType].[Deleted] " +
            ",(CASE WHEN [ClientTypeTranslation].[Name] IS NOT NULL THEN [ClientTypeTranslation].[Name] ELSE [ClientType].[Name] END) AS [Name] " +
            ",[ClientType].[NetUID] " +
            ",[ClientType].[Updated] " +
            ",[ClientType].[ClientTypeIcon] " +
            ",[ClientType].[AllowMultiple] " +
            ",[ClientType].[Type] " +
            "FROM [ClientType] " +
            "LEFT JOIN [ClientTypeTranslation] " +
            "ON [ClientTypeTranslation].ClientTypeID = [ClientType].ID " +
            "AND [ClientTypeTranslation].CultureCode = @Culture " +
            "AND [ClientTypeTranslation].Deleted = 0 " +
            ") AS [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN (" +
            "SELECT [ClientTypeRole].[ID] " +
            ",[ClientTypeRole].[ClientTypeID] " +
            ",[ClientTypeRole].[Created] " +
            ",[ClientTypeRole].[Deleted] " +
            ",(CASE WHEN [ClientTypeRoleTranslation].[Description] IS NOT NULL THEN [ClientTypeRoleTranslation].[Description] ELSE [ClientTypeRole].[Description] END) AS [Description] " +
            ",(CASE WHEN [ClientTypeRoleTranslation].[Name] IS NOT NULL THEN [ClientTypeRoleTranslation].[Name] ELSE [ClientTypeRole].[Name] END) AS [Name] " +
            ",[ClientTypeRole].[NetUID] " +
            ",[ClientTypeRole].[Updated] " +
            "FROM [ClientTypeRole] " +
            "LEFT JOIN [ClientTypeRoleTranslation] " +
            "ON [ClientTypeRoleTranslation].ClientTypeRoleID = [ClientTypeRole].ID " +
            "AND [ClientTypeRoleTranslation].CultureCode = @Culture " +
            "AND [ClientTypeRoleTranslation].Deleted = 0 " +
            ") AS [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
            "LEFT JOIN [ClientRegistrationTask] " +
            "ON [ClientRegistrationTask].ClientID = [Client].ID " +
            "WHERE [Client].Deleted = 0 " +
            "AND [ClientRegistrationTask].IsDone = 0 " +
            "ORDER BY [Client].[Created] DESC; ";

        Type[] types = {
            typeof(Client),
            typeof(Region),
            typeof(RegionCode),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeRole),
            typeof(ClientRegistrationTask)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            Region region = (Region)objects[1];
            RegionCode regionCode = (RegionCode)objects[2];
            ClientInRole clientInRole = (ClientInRole)objects[3];
            ClientType clientType = (ClientType)objects[4];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[5];
            ClientRegistrationTask clientRegistrationTask = (ClientRegistrationTask)objects[6];

            if (regionCode != null) regionCode.Region = region;

            if (clientInRole != null) {
                clientInRole.ClientType = clientType;
                clientInRole.ClientTypeRole = clientTypeRole;
            }

            if (clientRegistrationTask != null) client.ClientRegistrationTasks.Add(clientRegistrationTask);

            client.Region = region;
            client.RegionCode = regionCode;
            client.ClientInRole = clientInRole;

            toReturn.Add(client);

            return client;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public List<Client> GetAll(long offset, long limit) {
        List<Client> clients = new();

        string sqlExpression = ";WITH [Search_CTE] " +
                               "AS (" +
                               "SELECT ROW_NUMBER() OVER (ORDER BY [Client].ID) AS RowNumber " +
                               ", [Client].ID " +
                               "FROM [Client] " +
                               "WHERE [Client].Deleted = 0 AND [Client].IsSubClient = 0" +
                               ") " +
                               "SELECT * " +
                               "FROM Client " +
                               "LEFT OUTER JOIN ClientInRole " +
                               "ON Client.ID = ClientInRole.ClientID " +
                               "LEFT JOIN ClientType " +
                               "ON ClientInRole.ClientTypeID = ClientType.ID " +
                               "LEFT JOIN ClientTypeTranslation " +
                               "ON ClientType.ID = ClientTypeTranslation.ClientTypeID " +
                               "AND ClientTypeTranslation.CultureCode = @Culture " +
                               "AND ClientTypeTranslation.Deleted = 0 " +
                               "LEFT JOIN ClientTypeRole " +
                               "ON ClientTypeRole.ID = ClientInRole.ClientTypeRoleID " +
                               "LEFT JOIN ClientTypeRoleTranslation " +
                               "ON ClientTypeRole.ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
                               "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
                               "AND ClientTypeRoleTranslation.Deleted = 0 " +
                               "LEFT OUTER JOIN ClientAgreement " +
                               "ON Client.ID = ClientAgreement.ClientID " +
                               "AND ClientAgreement.Deleted = 0 " +
                               "LEFT OUTER JOIN Agreement " +
                               "ON ClientAgreement.AgreementID = Agreement.ID AND Agreement.IsActive = 1 " +
                               "LEFT JOIN Pricing AS [Agreement.Pricing] " +
                               "ON Agreement.PricingID = [Agreement.Pricing].ID " +
                               "LEFT JOIN PricingTranslation AS [Agreement.Pricing.Translation] " +
                               "ON [Agreement.Pricing].ID = [Agreement.Pricing.Translation].PricingID " +
                               "AND [Agreement.Pricing.Translation].CultureCode = @Culture " +
                               "LEFT JOIN ProviderPricing AS [Agreement.ProviderPricing]  " +
                               "ON Agreement.ProviderPricingID = [Agreement.ProviderPricing].ID " +
                               "LEFT JOIN Pricing AS [ProviderPricing.Pricing] " +
                               "ON [Agreement.ProviderPricing].BasePricingID = [ProviderPricing.Pricing].ID " +
                               "LEFT JOIN PricingTranslation AS [ProviderPricing.Pricing.Translation] " +
                               "ON [ProviderPricing.Pricing].ID = [ProviderPricing.Pricing.Translation].PricingID " +
                               "AND [ProviderPricing.Pricing.Translation].CultureCode = @Culture " +
                               "LEFT OUTER JOIN RegionCode " +
                               "ON RegionCode.ID = Client.RegionCodeID " +
                               "LEFT OUTER JOIN Region " +
                               "ON RegionCode.RegionID = Region.ID " +
                               "WHERE Client.ID IN (" +
                               "SELECT [Search_CTE].ID " +
                               "FROM [Search_CTE] " +
                               "WHERE [Search_CTE].RowNumber > @Offset " +
                               "AND [Search_CTE].RowNumber <= @Limit + @Offset" +
                               ")";

        Type[] types = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeTranslation),
            typeof(ClientTypeRole),
            typeof(ClientTypeRoleTranslation),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(RegionCode),
            typeof(Region)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeTranslation clientTypeTranslation = (ClientTypeTranslation)objects[3];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[4];
            ClientTypeRoleTranslation clientTypeRoleTranslation = (ClientTypeRoleTranslation)objects[5];
            ClientAgreement clientAgreement = (ClientAgreement)objects[6];
            Agreement agreement = (Agreement)objects[7];
            Pricing pricing = (Pricing)objects[8];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[9];
            ProviderPricing providerPricing = (ProviderPricing)objects[10];
            Pricing providerBasePricing = (Pricing)objects[11];
            PricingTranslation providerBasePricingTranslation = (PricingTranslation)objects[12];
            RegionCode regionCode = (RegionCode)objects[13];
            Region region = (Region)objects[14];

            if (clientInRole != null) {
                if (clientTypeTranslation != null) clientType.Name = clientTypeTranslation.Name;

                if (clientTypeRoleTranslation != null) {
                    clientTypeRole.Name = clientTypeRoleTranslation.Name;
                    clientTypeRole.Description = clientTypeRoleTranslation.Description;
                }

                clientInRole.ClientType = clientType;
                clientInRole.ClientTypeRole = clientTypeRole;

                client.ClientInRole = clientInRole;
            }

            if (regionCode != null) {
                regionCode.Region = region;

                if (clients.Any(c => c.Id.Equals(client.Id)))
                    clients.First(c => c.Id.Equals(client.Id)).RegionCode = regionCode;
                else
                    client.RegionCode = regionCode;
            }

            if (clientAgreement != null && agreement != null) {
                if (pricingTranslation != null) pricing.Name = pricingTranslation.Name;

                agreement.Pricing = pricing;

                if (providerPricing != null) {
                    if (providerBasePricingTranslation != null) providerBasePricing.Name = providerBasePricingTranslation.Name;

                    providerPricing.Pricing = providerBasePricing;
                    agreement.ProviderPricing = providerPricing;
                }

                clientAgreement.Agreement = agreement;
                client.ClientAgreements.Add(clientAgreement);

                if (clients.Any(c => c.Id.Equals(client.Id)))
                    clients.First(c => c.Id.Equals(client.Id)).ClientAgreements.Add(clientAgreement);
                else
                    clients.Add(client);
            } else {
                if (!clients.Any(c => c.Id.Equals(client.Id)))
                    clients.Add(client);
            }

            return client;
        };

        var props = new { Offset = offset, Limit = limit, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return clients;
    }

    public List<Client> GetAllSubClients(Guid clientNetId) {
        List<Client> subClients = new();

        string sqlExpression =
            "SELECT * FROM Client " +
            "LEFT OUTER JOIN ClientSubClient " +
            "ON ClientSubClient.SubClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
            "LEFT OUTER JOIN ClientAgreement " +
            "ON ClientAgreement.ClientID = Client.ID AND ClientAgreement.Deleted = 0 " +
            "LEFT OUTER JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID AND Agreement.Deleted = 0 " +
            "LEFT OUTER JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "LEFT OUTER JOIN OrganizationTranslation " +
            "ON Organization.ID = OrganizationTranslation.OrganizationID " +
            "AND OrganizationTranslation.CultureCode = @Culture " +
            "AND OrganizationTranslation.Deleted = 0 " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "LEFT OUTER JOIN Pricing " +
            "ON Pricing.ID = Agreement.PricingID " +
            "LEFT OUTER JOIN ClientInDebt " +
            "ON ClientInDebt.AgreementID = Agreement.ID AND ClientInDebt.Deleted = 0 " +
            "LEFT OUTER JOIN Debt " +
            "ON ClientInDebt.DebtID = Debt.ID AND Debt.Deleted = 0 " +
            "WHERE Client.Deleted = 0 " +
            "AND ClientSubClient.RootClientID = (SELECT ID FROM Client " +
            "WHERE NetUID = @ClientNetId " +
            ")";

        Type[] types = {
            typeof(Client),
            typeof(ClientSubClient),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization),
            typeof(OrganizationTranslation),
            typeof(RegionCode),
            typeof(Pricing),
            typeof(ClientInDebt),
            typeof(Debt)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Currency currency = (Currency)objects[4];
            Organization organization = (Organization)objects[5];
            OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[6];
            RegionCode regionCode = (RegionCode)objects[7];
            Pricing pricing = (Pricing)objects[8];
            ClientInDebt clientInDebt = (ClientInDebt)objects[9];
            Debt debt = (Debt)objects[10];

            if (regionCode != null) client.RegionCode = regionCode;

            if (clientAgreement != null && agreement != null) {
                if (pricing != null) agreement.Pricing = pricing;

                if (organization != null) {
                    if (organizationTranslation != null) organization.Name = organizationTranslation.Name;

                    agreement.Organization = organization;
                }

                if (currency != null) agreement.Currency = currency;

                if (clientInDebt != null) {
                    if (debt != null) clientInDebt.Debt = debt;

                    agreement.ClientInDebts.Add(clientInDebt);
                }

                clientAgreement.Agreement = agreement;
                client.ClientAgreements.Add(clientAgreement);

                if (subClients.Any(c => c.Id.Equals(client.Id))) {
                    Client subClientFromList = subClients.First(c => c.Id.Equals(client.Id));

                    if (!subClientFromList.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) subClientFromList.ClientAgreements.Add(clientAgreement);

                    if (subClientFromList.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id)))
                        if (clientInDebt != null)
                            subClientFromList.ClientAgreements.First(a => a.Id.Equals(clientAgreement.Id)).Agreement.ClientInDebts.Add(clientInDebt);
                } else {
                    subClients.Add(client);
                }
            } else {
                if (!subClients.Any(c => c.Id.Equals(client.Id)))
                    subClients.Add(client);
            }

            return client;
        };

        var props = new { ClientNetId = clientNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return subClients;
    }

    public List<Client> GetAllRootClientsWithoutIncludes() {
        return _connection.Query<Client, ClientUserProfile, User, Client>(
            "SELECT * " +
            "FROM [Client] " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].ID = (" +
            "SELECT TOP(1) [ClientUserProfile].ID " +
            "FROM [ClientUserProfile] " +
            "WHERE [ClientUserProfile].ClientID = [Client].ID " +
            "AND [ClientUserProfile].Deleted = 0 " +
            ") " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ClientUserProfile].UserProfileID " +
            "WHERE [Client].IsSubClient = 0 " +
            "AND [Client].IsTradePoint = 0 " +
            "AND [Client].Deleted = 0",
            (client, clientManager, manager) => {
                if (clientManager != null && manager != null) {
                    clientManager.UserProfile = manager;

                    client.ClientManagers.Add(clientManager);
                }

                return client;
            }
        ).ToList();
    }

    public List<Client> GetAllByManagerId(long managerId) {
        List<Client> clients = new();

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, ManagerId = managerId };

        string sqlExpression = "SELECT Client.ID FROM Client " +
                               "LEFT OUTER JOIN ClientInRole " +
                               "ON Client.ID = ClientInRole.ClientID " +
                               "LEFT JOIN ClientType AS [ClientInRole.ClientType] " +
                               "ON ClientInRole.ClientTypeID = [ClientInRole.ClientType].ID " +
                               "LEFT JOIN ClientTypeTranslation " +
                               "ON [ClientInRole.ClientType].ID = ClientTypeTranslation.ClientTypeID " +
                               "AND ClientTypeTranslation.CultureCode = @Culture " +
                               "AND ClientTypeTranslation.Deleted = 0 " +
                               "LEFT JOIN ClientTypeRole AS [ClientInRole.ClientTypeRole] " +
                               "ON [ClientInRole.ClientTypeRole].ID = ClientInRole.ClientTypeRoleID " +
                               "LEFT JOIN ClientTypeRoleTranslation " +
                               "ON [ClientInRole.ClientTypeRole].ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
                               "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
                               "AND ClientTypeRoleTranslation.Deleted = 0 " +
                               "LEFT OUTER JOIN ClientAgreement " +
                               "ON Client.ID = ClientAgreement.ClientID " +
                               "AND ClientAgreement.Deleted = 0 " +
                               "LEFT OUTER JOIN Agreement " +
                               "ON ClientAgreement.AgreementID = Agreement.ID AND Agreement.IsActive = 1 " +
                               "LEFT JOIN Pricing AS [Agreement.Pricing] " +
                               "ON Agreement.PricingID = [Agreement.Pricing].ID " +
                               "LEFT JOIN PricingTranslation AS [Agreement.Pricing.Translation] " +
                               "ON [Agreement.Pricing].ID = [Agreement.Pricing.Translation].PricingID " +
                               "AND [Agreement.Pricing.Translation].CultureCode = @Culture " +
                               "LEFT JOIN ProviderPricing AS [Agreement.ProviderPricing]  " +
                               "ON Agreement.ProviderPricingID = [Agreement.ProviderPricing].ID " +
                               "LEFT JOIN Pricing AS [ProviderPricing.Pricing] " +
                               "ON [Agreement.ProviderPricing].BasePricingID = [ProviderPricing.Pricing].ID " +
                               "LEFT JOIN PricingTranslation AS [ProviderPricing.Pricing.Translation] " +
                               "ON [ProviderPricing.Pricing].ID = [ProviderPricing.Pricing.Translation].PricingID " +
                               "AND [ProviderPricing.Pricing.Translation].CultureCode = @Culture " +
                               "LEFT OUTER JOIN RegionCode " +
                               "ON RegionCode.ID = Client.RegionCodeID " +
                               "LEFT OUTER JOIN Region " +
                               "ON RegionCode.RegionID = Region.ID " +
                               "LEFT JOIN ClientUserProfile " +
                               "ON ClientUserProfile.ClientID = Client.ID " +
                               "LEFT JOIN ClientInDebt " +
                               "ON ClientInDebt.ClientID = Client.ID " +
                               "AND ClientInDebt.Deleted = 0 " +
                               "WHERE ClientUserProfile.UserProfileID = @ManagerId " +
                               "AND ClientInDebt.ID IS NOT NULL " +
                               "GROUP BY Client.ID, " +
                               "Client.FullName, " +
                               "Client.ICQ, " +
                               "Client.MobileNumber, " +
                               "Client.EmailAddress, " +
                               "RegionCode.[Value], " +
                               "[ClientInRole.ClientTypeRole].[Name] " +
                               "ORDER BY Client.FullName";

        IEnumerable<long> clientIds = _connection.Query<long>(
            sqlExpression,
            props
        );

        string fullSqlExpression = "SELECT * FROM Client " +
                                   "LEFT OUTER JOIN ClientInRole " +
                                   "ON Client.ID = ClientInRole.ClientID " +
                                   "LEFT JOIN ClientType AS [ClientInRole.ClientType] " +
                                   "ON ClientInRole.ClientTypeID = [ClientInRole.ClientType].ID " +
                                   "LEFT JOIN ClientTypeTranslation " +
                                   "ON [ClientInRole.ClientType].ID = ClientTypeTranslation.ClientTypeID " +
                                   "AND ClientTypeTranslation.CultureCode = @Culture " +
                                   "AND ClientTypeTranslation.Deleted = 0 " +
                                   "LEFT JOIN ClientTypeRole AS [ClientInRole.ClientTypeRole] " +
                                   "ON [ClientInRole.ClientTypeRole].ID = ClientInRole.ClientTypeRoleID " +
                                   "LEFT JOIN ClientTypeRoleTranslation " +
                                   "ON [ClientInRole.ClientTypeRole].ID = ClientTypeRoleTranslation.ClientTypeRoleID " +
                                   "AND ClientTypeRoleTranslation.CultureCode = @Culture " +
                                   "AND ClientTypeRoleTranslation.Deleted = 0 " +
                                   "LEFT OUTER JOIN ClientAgreement " +
                                   "ON Client.ID = ClientAgreement.ClientID " +
                                   "AND ClientAgreement.Deleted = 0 " +
                                   "LEFT OUTER JOIN Agreement " +
                                   "ON ClientAgreement.AgreementID = Agreement.ID AND Agreement.IsActive = 1 " +
                                   "LEFT JOIN Pricing AS [Agreement.Pricing] " +
                                   "ON Agreement.PricingID = [Agreement.Pricing].ID " +
                                   "LEFT JOIN PricingTranslation AS [Agreement.Pricing.Translation] " +
                                   "ON [Agreement.Pricing].ID = [Agreement.Pricing.Translation].PricingID " +
                                   "AND [Agreement.Pricing.Translation].CultureCode = @Culture " +
                                   "LEFT JOIN ProviderPricing AS [Agreement.ProviderPricing]  " +
                                   "ON Agreement.ProviderPricingID = [Agreement.ProviderPricing].ID " +
                                   "LEFT JOIN Pricing AS [ProviderPricing.Pricing] " +
                                   "ON [Agreement.ProviderPricing].BasePricingID = [ProviderPricing.Pricing].ID " +
                                   "LEFT JOIN PricingTranslation AS [ProviderPricing.Pricing.Translation] " +
                                   "ON [ProviderPricing.Pricing].ID = [ProviderPricing.Pricing.Translation].PricingID " +
                                   "AND [ProviderPricing.Pricing.Translation].CultureCode = @Culture " +
                                   "LEFT OUTER JOIN RegionCode " +
                                   "ON RegionCode.ID = Client.RegionCodeID " +
                                   "LEFT OUTER JOIN Region " +
                                   "ON RegionCode.RegionID = Region.ID " +
                                   "LEFT OUTER JOIN ClientSubClient " +
                                   "ON Client.ID = ClientSubClient.RootClientID " +
                                   "AND ClientSubClient.Deleted = 0 " +
                                   "LEFT OUTER JOIN Client AS SubClient " +
                                   "ON ClientSubClient.SubClientID = SubClient.ID " +
                                   "LEFT JOIN RegionCode AS SubRegionCode " +
                                   "ON SubClient.RegionCodeID = SubRegionCode.ID " +
                                   "LEFT JOIN Region AS SubRegion " +
                                   "ON SubRegionCode.RegionID = SubRegion.ID " +
                                   "LEFT JOIN ClientInDebt " +
                                   "ON ClientInDebt.ClientID = Client.ID " +
                                   "AND ClientInDebt.Deleted = 0 " +
                                   "LEFT JOIN Debt " +
                                   "ON ClientInDebt.DebtID = Debt.ID " +
                                   "LEFT JOIN Agreement AS [ClientInDebt.Agreement] " +
                                   "ON [ClientInDebt.Agreement].ID = ClientInDebt.AgreementID " +
                                   "LEFT JOIN Currency AS [Agreement.Currency] " +
                                   "ON [ClientInDebt.Agreement].CurrencyID = [Agreement.Currency].ID " +
                                   "LEFT JOIN CurrencyTranslation AS [Agreement.CurrencyTranslation] " +
                                   "ON [Agreement.CurrencyTranslation].CurrencyID = [Agreement.Currency].ID " +
                                   "AND [Agreement.CurrencyTranslation].CultureCode = @Culture " +
                                   "LEFT JOIN Pricing AS [ClientInDebt.Agreement.Pricing] " +
                                   "ON [ClientInDebt.Agreement.Pricing].ID = [ClientInDebt.Agreement].PricingID " +
                                   "LEFT JOIN PricingTranslation AS [ClientInDebt.Agreement.PricingTranslation] " +
                                   "ON [ClientInDebt.Agreement.Pricing].ID = [ClientInDebt.Agreement.PricingTranslation].PricingID " +
                                   "AND [ClientInDebt.Agreement.PricingTranslation].CultureCode = @Culture " +
                                   "LEFT JOIN Currency AS [ClientInDebt.Agreement.Pricing.Currency] " +
                                   "ON [ClientInDebt.Agreement.Pricing.Currency].ID = [ClientInDebt.Agreement.Pricing].CurrencyID " +
                                   "LEFT JOIN CurrencyTranslation AS  [ClientInDebt.Agreement.Pricing.CurrencyTranslation] " +
                                   "ON [ClientInDebt.Agreement.Pricing.CurrencyTranslation].CurrencyID = [ClientInDebt.Agreement.Pricing.Currency].ID " +
                                   "AND [ClientInDebt.Agreement.Pricing.CurrencyTranslation].CultureCode = @Culture " +
                                   "WHERE Client.ID IN @Ids ";

        Type[] types = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeTranslation),
            typeof(ClientTypeRole),
            typeof(ClientTypeRoleTranslation),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(RegionCode),
            typeof(Region),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(Region),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Agreement),
            typeof(Currency),
            typeof(CurrencyTranslation),
            typeof(Pricing),
            typeof(PricingTranslation),
            typeof(Currency),
            typeof(CurrencyTranslation)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeTranslation clientTypeTranslation = (ClientTypeTranslation)objects[3];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[4];
            ClientTypeRoleTranslation clientTypeRoleTranslation = (ClientTypeRoleTranslation)objects[5];
            ClientAgreement clientAgreement = (ClientAgreement)objects[6];
            Agreement agreement = (Agreement)objects[7];
            Pricing pricing = (Pricing)objects[8];
            PricingTranslation pricingTranslation = (PricingTranslation)objects[9];
            ProviderPricing providerPricing = (ProviderPricing)objects[10];
            Pricing providerBasePricing = (Pricing)objects[11];
            PricingTranslation providerBasePricingTranslation = (PricingTranslation)objects[12];
            RegionCode regionCode = (RegionCode)objects[13];
            Region region = (Region)objects[14];
            ClientSubClient clientSubClient = (ClientSubClient)objects[15];
            Client subClient = (Client)objects[16];
            RegionCode subRegionCode = (RegionCode)objects[17];
            Region subRegion = (Region)objects[18];
            ClientInDebt clientInDebt = (ClientInDebt)objects[19];
            Debt debt = (Debt)objects[20];
            Agreement clientInDebtAgreement = (Agreement)objects[21];
            Currency clientInDebtAgreementCurrency = (Currency)objects[22];
            CurrencyTranslation clientInDebtAgreementCurrencyTranslation = (CurrencyTranslation)objects[23];
            Pricing clientInDebtAgreementPricing = (Pricing)objects[24];
            PricingTranslation clientInDebtAgreementPricingTranslation = (PricingTranslation)objects[25];
            Currency clientInDebtAgreementPricingCurrency = (Currency)objects[26];
            CurrencyTranslation clientInDebtAgreementPricingCurrencyTranslation = (CurrencyTranslation)objects[27];

            if (clientInRole != null) {
                if (clientTypeTranslation != null) clientType.Name = clientTypeTranslation.Name;

                if (clientTypeRoleTranslation != null) {
                    clientTypeRole.Name = clientTypeRoleTranslation.Name;
                    clientTypeRole.Description = clientTypeRoleTranslation.Description;
                }

                clientInRole.ClientType = clientType;
                clientInRole.ClientTypeRole = clientTypeRole;

                client.ClientInRole = clientInRole;
            }

            if (clientSubClient != null) {
                if (subRegionCode != null) {
                    subRegionCode.Region = subRegion;

                    subClient.RegionCode = subRegionCode;
                }

                clientSubClient.SubClient = subClient;

                if (clients.Any(c => c.Id.Equals(client.Id))) {
                    if (!clients.First(c => c.Id.Equals(client.Id)).SubClients.Any(s => s.Id.Equals(clientSubClient.Id)))
                        clients.First(c => c.Id.Equals(client.Id)).SubClients.Add(clientSubClient);
                } else {
                    client.SubClients.Add(clientSubClient);
                }
            }

            if (regionCode != null) {
                regionCode.Region = region;

                if (clients.Any(c => c.Id.Equals(client.Id)))
                    clients.First(c => c.Id.Equals(client.Id)).RegionCode = regionCode;
                else
                    client.RegionCode = regionCode;
            }

            if (clientInDebt != null) {
                if (clientInDebtAgreement != null) {
                    if (clientInDebtAgreementPricingTranslation != null) clientInDebtAgreementPricing.Name = clientInDebtAgreementPricingTranslation.Name;

                    if (clientInDebtAgreementCurrencyTranslation != null) clientInDebtAgreementCurrency.Name = clientInDebtAgreementCurrencyTranslation.Name;

                    if (clientInDebtAgreementPricingCurrencyTranslation != null) clientInDebtAgreementPricingCurrency.Name = clientInDebtAgreementPricingCurrencyTranslation.Name;

                    clientInDebtAgreement.Currency = clientInDebtAgreementCurrency;
                    clientInDebtAgreementPricing.Currency = clientInDebtAgreementPricingCurrency;
                    clientInDebtAgreement.Pricing = clientInDebtAgreementPricing;
                }

                clientInDebt.Debt = debt;
                clientInDebt.Agreement = clientInDebtAgreement;

                if (clients.Any(c => c.Id.Equals(client.Id))) {
                    if (!clients.First(c => c.Id.Equals(client.Id)).ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id)))
                        clients.First(c => c.Id.Equals(client.Id)).ClientInDebts.Add(clientInDebt);
                } else {
                    if (!client.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) client.ClientInDebts.Add(clientInDebt);
                }
            }

            if (clientAgreement != null && agreement != null) {
                if (pricingTranslation != null) pricing.Name = pricingTranslation.Name;

                agreement.Pricing = pricing;

                if (providerPricing != null) {
                    if (providerBasePricingTranslation != null) providerBasePricing.Name = providerBasePricingTranslation.Name;

                    providerPricing.Pricing = providerBasePricing;
                    agreement.ProviderPricing = providerPricing;
                }

                clientAgreement.Agreement = agreement;
                client.ClientAgreements.Add(clientAgreement);

                if (clients.Any(c => c.Id.Equals(client.Id))) {
                    if (!clients.First(c => c.Id.Equals(client.Id)).ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id)))
                        clients.First(c => c.Id.Equals(client.Id)).ClientAgreements.Add(clientAgreement);
                } else {
                    clients.Add(client);
                }
            } else {
                if (!clients.Any(c => c.Id.Equals(client.Id)))
                    clients.Add(client);
            }

            return client;
        };

        var fullProps = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, ManagerId = managerId, Ids = clientIds };

        _connection.Query(fullSqlExpression, types, mapper, fullProps);

        return clients;
    }

    public List<Client> GetAllFromSearchByServicePayers(string value, long limit, long offset) {
        List<Client> clients = new();

        IEnumerable<long> ids = _connection.Query<long>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ClientID) AS RowNumber " +
            ",ClientID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [ServicePayer].ClientID " +
            "FROM [ServicePayer] " +
            "WHERE [ServicePayer].Deleted = 0 " +
            "AND ( " +
            "[ServicePayer].FirstName like '%' + @Value + '%' " +
            "OR " +
            "[ServicePayer].LastName like '%' + @Value + '%' " +
            "OR " +
            "[ServicePayer].MiddleName like '%' + @Value + '%' " +
            "OR " +
            "[ServicePayer].MobilePhone like '%' + @Value + '%' " +
            "OR " +
            "[ServicePayer].Comment like '%' + @Value + '%' " +
            "OR " +
            "[ServicePayer].PaymentAddress like '%' + @Value + '%' " +
            "OR " +
            "[ServicePayer].PaymentCard like '%' + @Value + '%' " +
            ") " +
            ") [Distincts] " +
            ") " +
            "SELECT [Search_CTE].ClientID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset ",
            new { Value = value, Limit = limit, Offset = offset }
        );

        var clientsProps = new { Ids = ids, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        Type[] clientsTypes = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeRole),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Pricing),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(RegionCode),
            typeof(Region),
            typeof(Currency)
        };

        Func<object[], Client> clientsMapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Pricing pricing = (Pricing)objects[6];
            ProviderPricing providerPricing = (ProviderPricing)objects[7];
            Pricing providerPricingPricing = (Pricing)objects[8];
            RegionCode regionCode = (RegionCode)objects[9];
            Region region = (Region)objects[10];
            Currency currency = (Currency)objects[11];

            if (!clients.Any(c => c.Id.Equals(client.Id))) {
                if (clientInRole != null) {
                    clientInRole.ClientType = clientType;
                    clientInRole.ClientTypeRole = clientTypeRole;

                    client.ClientInRole = clientInRole;
                }

                if (clientAgreement != null) {
                    if (providerPricing != null) {
                        providerPricing.Pricing = providerPricingPricing;

                        agreement.ProviderPricing = providerPricing;
                    }

                    agreement.Pricing = pricing;
                    agreement.Currency = currency;
                    clientAgreement.Agreement = agreement;

                    client.ClientAgreements.Add(clientAgreement);
                }

                if (regionCode != null) {
                    regionCode.Region = region;

                    client.RegionCode = regionCode;
                }

                clients.Add(client);
            } else if (clientAgreement != null) {
                Client fromList = clients.First(c => c.Id.Equals(client.Id));

                if (!fromList.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) {
                    if (providerPricing != null) {
                        providerPricing.Pricing = providerPricingPricing;

                        agreement.ProviderPricing = providerPricing;
                    }

                    agreement.Pricing = pricing;

                    clientAgreement.Agreement = agreement;

                    fromList.ClientAgreements.Add(clientAgreement);
                }
            }

            return client;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [Client] " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT [ClientType].ID " +
            ",[ClientType].ClientTypeIcon " +
            ",[ClientType].Created " +
            ",[ClientType].Deleted " +
            ",[ClientType].AllowMultiple " +
            ",[ClientType].NetUID " +
            ",[ClientType].Type " +
            ",[ClientType].Updated " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeTranslation].Name IS NOT NULL " +
            "THEN [ClientTypeTranslation].Name " +
            "ELSE [ClientType].Name " +
            "END " +
            ") AS [Name] " +
            "FROM [ClientType] " +
            "LEFT JOIN [ClientTypeTranslation] " +
            "ON [ClientTypeTranslation].ClientTypeID = [ClientType].ID " +
            "AND [ClientTypeTranslation].CultureCode = @Culture " +
            "AND [ClientTypeTranslation].Deleted = 0 " +
            ") AS [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN ( " +
            "SELECT [ClientTypeRole].ID " +
            ",[ClientTypeRole].ClientTypeID " +
            ",[ClientTypeRole].Created " +
            ",[ClientTypeRole].Deleted " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeRoleTranslation].Name IS NOT NULL " +
            "THEN [ClientTypeRoleTranslation].Name " +
            "ELSE [ClientTypeRole].Name " +
            "END " +
            ") AS [Name] " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeRoleTranslation].Description IS NOT NULL " +
            "THEN [ClientTypeRoleTranslation].Description " +
            "ELSE [ClientTypeRole].Description " +
            "END " +
            ") AS [Description] " +
            ",[ClientTypeRole].NetUID " +
            ",[ClientTypeRole].Updated " +
            "FROM [ClientTypeRole] " +
            "LEFT JOIN [ClientTypeRoleTranslation] " +
            "ON [ClientTypeRoleTranslation].ClientTypeRoleID = [ClientTypeRole].ID " +
            "AND [ClientTypeRoleTranslation].CultureCode = @Culture " +
            "AND [ClientTypeRoleTranslation].Deleted = 0 " +
            ") AS [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [views].[PricingView] AS [ProviderPricing.Pricing] " +
            "ON [ProviderPricing.Pricing].ID = [ProviderPricing].BasePricingID " +
            "AND [ProviderPricing.Pricing].CultureCode = @Culture " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Region] " +
            "ON [Region].ID = [RegionCode].RegionID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [Client].ID IN @Ids",
            clientsTypes,
            clientsMapper,
            clientsProps
        );

        Type[] debtsTypes = {
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Agreement),
            typeof(Currency),
            typeof(Pricing),
            typeof(Currency),
            typeof(Sale),
            typeof(ReSale),
            typeof(SaleNumber),
            typeof(User),
            typeof(Transporter),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus)
        };

        Func<object[], ClientInDebt> debtsMapper = objects => {
            ClientInDebt clientInDebt = (ClientInDebt)objects[0];
            Debt debt = (Debt)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Currency currency = (Currency)objects[3];
            Pricing pricing = (Pricing)objects[4];
            Currency pricingCurrency = (Currency)objects[5];
            Sale sale = (Sale)objects[6];
            ReSale reSale = (ReSale)objects[7];
            SaleNumber saleNumber = (SaleNumber)objects[8];
            User user = (User)objects[9];
            Transporter transporter = (Transporter)objects[10];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[11];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[12];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[13];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[14];

            Client fromList = clients.First(c => c.Id.Equals(clientInDebt.ClientId));

            if (!fromList.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) {
                if (pricing != null) {
                    pricing.Currency = pricingCurrency;

                    agreement.Pricing = pricing;
                }

                debt.Total = Math.Round(debt.Total, 2);
                debt.Days = Convert.ToInt32((DateTime.UtcNow - debt.Created).TotalDays);

                if (sale != null) {
                    sale.SaleNumber = saleNumber;
                    sale.User = user;
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

                agreement.Currency = currency;

                clientInDebt.Debt = debt;
                clientInDebt.Agreement = agreement;
                clientInDebt.Sale = sale;
                clientInDebt.ReSale = reSale;

                fromList.ClientInDebts.Add(clientInDebt);
            }


            return clientInDebt;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ClientInDebt] " +
            "LEFT JOIN [Debt] " +
            "ON [Debt].ID = [ClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientInDebt].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [PricingCurrency] " +
            "ON [PricingCurrency].ID = [Pricing].CurrencyID " +
            "AND [PricingCurrency].CultureCode = @Culture " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ClientInDebt].SaleID " +
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
            "WHERE [ClientInDebt].Deleted = 0 " +
            "AND [Debt].Total > 0 " +
            "AND [ClientInDebt].ClientID IN @Ids",
            debtsTypes,
            debtsMapper,
            clientsProps
        );

        Type[] subClientTypes = {
            typeof(ClientSubClient),
            typeof(Client),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Agreement),
            typeof(Currency),
            typeof(Pricing),
            typeof(Currency),
            typeof(Client),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Agreement),
            typeof(Currency),
            typeof(Pricing),
            typeof(Currency),
            typeof(RegionCode),
            typeof(Region)
        };

        Func<object[], ClientSubClient> subClientsMapper = objects => {
            ClientSubClient clientSubClient = (ClientSubClient)objects[0];
            Client rootClient = (Client)objects[1];
            ClientInDebt rootClientInDebt = (ClientInDebt)objects[2];
            Debt rootDebt = (Debt)objects[3];
            Agreement rootAgreement = (Agreement)objects[4];
            Currency rootCurrency = (Currency)objects[5];
            Pricing rootPricing = (Pricing)objects[6];
            Currency rootPricingCurrency = (Currency)objects[7];
            Client subClient = (Client)objects[8];
            ClientInDebt subClientInDebt = (ClientInDebt)objects[9];
            Debt subDebt = (Debt)objects[10];
            Agreement subAgreement = (Agreement)objects[11];
            Currency subCurrency = (Currency)objects[12];
            Pricing subPricing = (Pricing)objects[13];
            Currency subPricingCurrency = (Currency)objects[14];
            RegionCode regionCode = (RegionCode)objects[15];
            Region region = (Region)objects[16];

            Client fromList = clients.First(c => c.Id.Equals(clientSubClient.RootClientId));

            if (!fromList.SubClients.Any(s => s.Id.Equals(clientSubClient.Id))) {
                if (rootClientInDebt != null) {
                    if (rootPricing != null) {
                        rootPricing.Currency = rootPricingCurrency;

                        rootAgreement.Pricing = rootPricing;
                    }

                    rootAgreement.Currency = rootCurrency;

                    rootClientInDebt.Agreement = rootAgreement;
                    rootClientInDebt.Debt = rootDebt;

                    rootClient.ClientInDebts.Add(rootClientInDebt);
                }

                if (subClientInDebt != null) {
                    if (subPricing != null) {
                        subPricing.Currency = subPricingCurrency;

                        subAgreement.Pricing = subPricing;
                    }

                    subAgreement.Currency = subCurrency;

                    subClientInDebt.Agreement = subAgreement;
                    subClientInDebt.Debt = subDebt;

                    subClient.ClientInDebts.Add(subClientInDebt);
                }

                if (regionCode != null) {
                    regionCode.Region = region;

                    subClient.RegionCode = regionCode;
                }

                clientSubClient.RootClient = rootClient;
                clientSubClient.SubClient = subClient;

                fromList.SubClients.Add(clientSubClient);
            } else {
                ClientSubClient subClientFromList = fromList.SubClients.First(s => s.Id.Equals(clientSubClient.Id));

                if (rootClientInDebt != null && !subClientFromList.RootClient.ClientInDebts.Any(d => d.Id.Equals(rootClientInDebt.Id))) {
                    if (rootPricing != null) {
                        rootPricing.Currency = rootPricingCurrency;

                        rootAgreement.Pricing = rootPricing;
                    }

                    rootAgreement.Currency = rootCurrency;

                    rootClientInDebt.Agreement = rootAgreement;
                    rootClientInDebt.Debt = rootDebt;

                    subClientFromList.RootClient.ClientInDebts.Add(rootClientInDebt);
                }

                if (subClientInDebt != null && !subClientFromList.SubClient.ClientInDebts.Any(d => d.Id.Equals(subClientInDebt.Id))) {
                    if (subPricing != null) {
                        subPricing.Currency = subPricingCurrency;

                        subAgreement.Pricing = subPricing;
                    }

                    subAgreement.Currency = subCurrency;

                    subClientInDebt.Agreement = subAgreement;
                    subClientInDebt.Debt = subDebt;

                    subClientFromList.SubClient.ClientInDebts.Add(subClientInDebt);
                }
            }

            return clientSubClient;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ClientSubClient] " +
            "LEFT JOIN [Client] AS [RootClient] " +
            "ON [RootClient].ID = [ClientSubClient].RootClientID " +
            "LEFT JOIN [ClientInDebt] AS [RootClientInDebt] " +
            "ON [RootClientInDebt].ClientID = [RootClient].ID " +
            "AND [RootClientInDebt].Deleted = 0 " +
            "LEFT JOIN [Debt] AS [RootDebt] " +
            "ON [RootDebt].ID = [RootClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] AS [RootAgreement] " +
            "ON [RootAgreement].ID = [RootClientInDebt].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [RootCurrency] " +
            "ON [RootCurrency].ID = [RootAgreement].CurrencyID " +
            "AND [RootCurrency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [RootPricing] " +
            "ON [RootPricing].ID = [RootAgreement].PricingID " +
            "AND [RootPricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [RootPricingCurrency] " +
            "ON [RootPricingCurrency].ID = [RootPricing].CurrencyID " +
            "AND [RootPricingCurrency].CultureCode = @Culture " +
            "LEFT JOIN [Client] AS [SubClient] " +
            "ON [SubClient].ID = [ClientSubClient].SubClientID " +
            "LEFT JOIN [ClientInDebt] AS [SubClientInDebt] " +
            "ON [SubClientInDebt].ClientID = [SubClient].ID " +
            "AND [SubClientInDebt].Deleted = 0 " +
            "LEFT JOIN [Debt] AS [SubDebt] " +
            "ON [SubDebt].ID = [SubClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] AS [SubAgreement] " +
            "ON [SubAgreement].ID = [SubClientInDebt].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [SubCurrency] " +
            "ON [SubCurrency].ID = [SubAgreement].CurrencyID " +
            "AND [SubCurrency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [SubPricing] " +
            "ON [SubPricing].ID = [SubAgreement].PricingID " +
            "AND [SubPricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [SubPricingCurrency] " +
            "ON [SubPricingCurrency].ID = [SubPricing].CurrencyID " +
            "AND [SubPricingCurrency].CultureCode = @Culture " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [SubClient].RegionCodeID " +
            "LEFT JOIN [Region] " +
            "ON [Region].ID = [RegionCode].RegionID " +
            "WHERE [ClientSubClient].Deleted = 0 " +
            "AND [ClientSubClient].RootClientID IN @Ids",
            subClientTypes,
            subClientsMapper,
            clientsProps
        );

        return clients;
    }

    public List<Client> GetAllManufacturerClients() {
        List<Client> manufacturers = new();

        string sqlExpression =
            "SELECT * " +
            "FROM Client " +
            "LEFT JOIN ClientInRole " +
            "ON ClientInRole.ClientID = Client.ID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.ClientID = Client.ID AND ClientAgreement.Deleted = 0 " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID AND Agreement.Deleted = 0 " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = Agreement.CurrencyID " +
            "LEFT JOIN Organization " +
            "ON Agreement.OrganizationID = Organization.ID " +
            "LEFT JOIN OrganizationTranslation " +
            "ON Organization.ID = OrganizationTranslation.OrganizationID " +
            "AND OrganizationTranslation.CultureCode = @Culture " +
            "AND OrganizationTranslation.Deleted = 0 " +
            "LEFT JOIN RegionCode " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "LEFT OUTER JOIN Pricing " +
            "ON Pricing.ID = Agreement.PricingID " +
            "WHERE Client.Deleted = 0 " +
            "AND ClientInRole.ClientTypeRoleID = @ManufactureTypeRoleId " +
            "AND [Client].[IsActive] = 1";

        Type[] types = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Currency),
            typeof(Organization),
            typeof(OrganizationTranslation),
            typeof(RegionCode),
            typeof(Pricing)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Currency currency = (Currency)objects[4];
            Organization organization = (Organization)objects[5];
            OrganizationTranslation organizationTranslation = (OrganizationTranslation)objects[6];
            RegionCode regionCode = (RegionCode)objects[7];
            Pricing pricing = (Pricing)objects[8];

            if (regionCode != null) client.RegionCode = regionCode;

            if (clientAgreement != null && agreement != null) {
                if (pricing != null) agreement.Pricing = pricing;

                if (organization != null) {
                    if (organizationTranslation != null) organization.Name = organizationTranslation.Name;

                    agreement.Organization = organization;
                }

                if (currency != null) {
                    agreement.Currency = currency;
                    agreement.Name = $"{agreement.Name} ({currency.Code})";
                }

                clientAgreement.Agreement = agreement;
                client.ClientAgreements.Add(clientAgreement);

                if (manufacturers.Any(c => c.Id.Equals(client.Id))) {
                    Client manufactureFromList = manufacturers.First(c => c.Id.Equals(client.Id));

                    if (!manufactureFromList.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) manufactureFromList.ClientAgreements.Add(clientAgreement);
                } else {
                    manufacturers.Add(client);
                }
            } else {
                if (!manufacturers.Any(c => c.Id.Equals(client.Id)))
                    manufacturers.Add(client);
            }

            return client;
        };

        var props = new { ManufactureTypeRoleId = 4, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return manufacturers;
    }

    public List<Client> GetAllForExport(
        string orderBy,
        string booleanFilter,
        string roleTypeFilter,
        bool searchForClients = true,
        bool? forReSale = null) {
        List<Client> clients = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Value]) AS RowNumber " +
            ", ID " +
            "FROM ( " +
            "SELECT [Client].ID, [RegionCode].[Value] " +
            "FROM [Client] " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeID = [RegionCode].ID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] AS [ClientInRole.ClientTypeRole] " +
            "ON [ClientInRole.ClientTypeRole].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "LEFT JOIN [PricingTranslation] " +
            "ON [PricingTranslation].PricingID = [Pricing].ID " +
            "AND [PricingTranslation].CultureCode = @Culture " +
            "AND [PricingTranslation].Deleted = 0 " +
            "LEFT JOIN [Pricing] AS [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [PricingTranslation] AS [ProviderPricingTranslation] " +
            "ON [ProviderPricingTranslation].PricingID = [ProviderPricing].ID " +
            "AND [ProviderPricingTranslation].CultureCode = @Culture " +
            "AND [ProviderPricingTranslation].Deleted = 0 " +
            "WHERE [Client].Deleted = 0 " +
            "AND Client.IsSubClient = 0 ";

        if (!string.IsNullOrEmpty(booleanFilter)) sqlExpression += booleanFilter;

        if (!string.IsNullOrEmpty(roleTypeFilter)) sqlExpression += roleTypeFilter;

        if (searchForClients)
            sqlExpression += "AND ([ClientInRole.ClientTypeRole].Type = 0 OR [Client].IsTemporaryClient = 1) ";
        else
            sqlExpression += "AND [ClientInRole.ClientTypeRole].Type = 1 ";

        if (forReSale.HasValue && forReSale.Value) sqlExpression += "AND [Agreement].[ForReSale] = 1 ";

        sqlExpression += "GROUP BY [Client].ID, [RegionCode].[Value] " +
                         ") [Distincts] " +
                         ") " +
                         "SELECT [Search_CTE].ID " +
                         "FROM [Search_CTE] ";

        sqlExpression += "ORDER BY RowNumber ";

        var props = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        //Search on root clients
        IEnumerable<long> ids = _connection.Query<long>(
            sqlExpression,
            props
        );

        if (!ids.Any()) {
            //Search on sub clients
            sqlExpression = sqlExpression.Replace("AND Client.IsSubClient = 0", "AND Client.IsSubClient = 1");

            ids = _connection.Query<long>(
                sqlExpression,
                props
            );

            if (!ids.Any()) return clients;

            //Retrieving root client ids of found sub clients 
            ids = _connection.Query<long>(
                "SELECT [ClientSubClient].RootClientID " +
                "FROM [ClientSubClient] " +
                "WHERE [ClientSubClient].Deleted = 0 " +
                "AND [ClientSubClient].SubClientID IN @Ids",
                new { Ids = ids }
            );

            if (!ids.Any()) return clients;
        }

        Type[] clientsTypes = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeRole),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Pricing),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(Pricing),
            typeof(RegionCode),
            typeof(Region),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(Currency)
        };

        Func<object[], Client> clientsMapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Organization organization = (Organization)objects[6];
            Pricing pricing = (Pricing)objects[7];
            ProviderPricing providerPricing = (ProviderPricing)objects[8];
            Pricing providerPricingPricing = (Pricing)objects[9];
            Pricing promotionalPricing = (Pricing)objects[10];
            RegionCode regionCode = (RegionCode)objects[11];
            Region region = (Region)objects[12];
            ClientUserProfile clientUserProfile = (ClientUserProfile)objects[13];
            User user = (User)objects[14];
            Currency currency = (Currency)objects[15];

            if (!clients.Any(c => c.Id.Equals(client.Id))) {
                if (clientInRole != null) {
                    clientInRole.ClientType = clientType;
                    clientInRole.ClientTypeRole = clientTypeRole;

                    client.ClientInRole = clientInRole;
                }

                if (clientAgreement != null) {
                    if (providerPricing != null) {
                        providerPricing.Pricing = providerPricingPricing;

                        agreement.ProviderPricing = providerPricing;
                    }

                    if (promotionalPricing != null) {
                        agreement.PromotionalPricing = promotionalPricing;
                        agreement.HasPromotionalPricing = true;
                    }

                    agreement.Currency = currency;
                    agreement.Pricing = pricing;
                    agreement.Organization = organization;

                    agreement.IsControlNumberDaysDebt = agreement.NumberDaysDebt > 0;

                    clientAgreement.Agreement = agreement;

                    client.ClientAgreements.Add(clientAgreement);
                }

                if (regionCode != null) {
                    regionCode.Region = region;

                    client.RegionCode = regionCode;
                }

                if (clientUserProfile != null) {
                    clientUserProfile.UserProfile = user;

                    client.ClientManagers.Add(clientUserProfile);
                }

                clients.Add(client);
            } else {
                Client fromList = clients.First(c => c.Id.Equals(client.Id));

                if (clientAgreement != null && !fromList.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) {
                    if (providerPricing != null) {
                        providerPricing.Pricing = providerPricingPricing;

                        agreement.ProviderPricing = providerPricing;
                    }

                    if (promotionalPricing != null) agreement.PromotionalPricing = promotionalPricing;

                    agreement.Currency = currency;
                    agreement.Pricing = pricing;
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    fromList.ClientAgreements.Add(clientAgreement);
                }

                if (clientUserProfile != null && !client.ClientManagers.Any(p => p.Id.Equals(clientUserProfile.Id))) {
                    clientUserProfile.UserProfile = user;

                    client.ClientManagers.Add(clientUserProfile);
                }
            }

            return client;
        };

        string clientsExpression =
            "SELECT " +
            "[Client].* " +
            ", [ClientInRole].* " +
            ", [ClientType].* " +
            ", [ClientTypeRole].* " +
            ", [ClientAgreement].* " +
            ", CASE WHEN [ClientAgreement].OriginalClientAmgCode IS NULL THEN 0 ELSE 1 END [FromAmg] " +
            ", CASE WHEN [AmgOriginalClient].[Name] IS NULL THEN [FenixOriginalClient].[Name] ELSE [AmgOriginalClient].[Name] END [OriginalClientName] " +
            ", [Agreement].* " +
            ", [Organization].* " +
            ", [Pricing].* " +
            ", [ProviderPricing].* " +
            ", [ProviderPricing.Pricing].* " +
            ", [PromotionalPricing].* " +
            ", [RegionCode].* " +
            ", [Region].* " +
            ", [ClientUserProfile].* " +
            ", [Manager].* " +
            ", [Currency].* " +
            "FROM [Client] " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT [ClientType].ID " +
            ",[ClientType].ClientTypeIcon " +
            ",[ClientType].Created " +
            ",[ClientType].Deleted " +
            ",[ClientType].AllowMultiple " +
            ",[ClientType].NetUID " +
            ",[ClientType].Type " +
            ",[ClientType].Updated " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeTranslation].Name IS NOT NULL " +
            "THEN [ClientTypeTranslation].Name " +
            "ELSE [ClientType].Name " +
            "END " +
            ") AS [Name] " +
            "FROM [ClientType] " +
            "LEFT JOIN [ClientTypeTranslation] " +
            "ON [ClientTypeTranslation].ClientTypeID = [ClientType].ID " +
            "AND [ClientTypeTranslation].CultureCode = @Culture " +
            "AND [ClientTypeTranslation].Deleted = 0 " +
            ") AS [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN ( " +
            "SELECT [ClientTypeRole].ID " +
            ",[ClientTypeRole].ClientTypeID " +
            ",[ClientTypeRole].Created " +
            ",[ClientTypeRole].Deleted " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeRoleTranslation].Name IS NOT NULL " +
            "THEN [ClientTypeRoleTranslation].Name " +
            "ELSE [ClientTypeRole].Name " +
            "END " +
            ") AS [Name] " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeRoleTranslation].Description IS NOT NULL " +
            "THEN [ClientTypeRoleTranslation].Description " +
            "ELSE [ClientTypeRole].Description " +
            "END " +
            ") AS [Description] " +
            ",[ClientTypeRole].NetUID " +
            ",[ClientTypeRole].Updated " +
            "FROM [ClientTypeRole] " +
            "LEFT JOIN [ClientTypeRoleTranslation] " +
            "ON [ClientTypeRoleTranslation].ClientTypeRoleID = [ClientTypeRole].ID " +
            "AND [ClientTypeRoleTranslation].CultureCode = @Culture " +
            "AND [ClientTypeRoleTranslation].Deleted = 0 " +
            ") AS [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Client] [AmgOriginalClient] " +
            "ON [AmgOriginalClient].[SourceAmgCode] = [ClientAgreement].[OriginalClientAmgCode] " +
            "AND [AmgOriginalClient].[SourceAmgCode] != [Client].[SourceAmgCode] " +
            "AND [AmgOriginalClient].[Name] IS NOT NULL " +
            "LEFT JOIN [Client] [FenixOriginalClient] " +
            "ON [FenixOriginalClient].[SourceFenixCode] = [ClientAgreement].[OriginalClientFenixCode] " +
            "AND [FenixOriginalClient].[SourceFenixCode] != [Client].[SourceFenixCode] " +
            "AND [FenixOriginalClient].[Name] IS NOT NULL " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [views].[PricingView] AS [ProviderPricing.Pricing] " +
            "ON [ProviderPricing.Pricing].ID = [ProviderPricing].BasePricingID " +
            "AND [ProviderPricing.Pricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [PromotionalPricing] " +
            "ON [PromotionalPricing].[ID] = [Agreement].[PromotionalPricingID] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Region] " +
            "ON [Region].ID = [RegionCode].RegionID " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].ClientID = [Client].ID " +
            "AND [ClientUserProfile].Deleted = 0 " +
            "LEFT JOIN [User] AS [Manager] " +
            "ON [Manager].ID = [ClientUserProfile].UserProfileID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [Client].ID IN @Ids ";


        foreach (long[] chunk in ids.Chunk(2000)) {
            var clientsProps = new { Ids = chunk, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

            _connection.Query(
                clientsExpression,
                clientsTypes,
                clientsMapper,
                clientsProps
            );
        }

        return clients;
    }

    public List<Client> GetAllFromSearch(
        long limit,
        long offset,
        string value,
        string orderBy,
        string booleanFilter,
        string roleTypeFilter,
        bool searchForClients = true,
        bool? forReSale = null) {
        List<Client> clients = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Value]) AS RowNumber " +
            ", ID " +
            "FROM ( " +
            "SELECT [Client].ID, [RegionCode].[Value] " +
            "FROM [Client] " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeID = [RegionCode].ID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN [ClientType] AS [ClientInRole.ClientTypeRole] " +
            "ON [ClientInRole.ClientTypeRole].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "LEFT JOIN [PricingTranslation] " +
            "ON [PricingTranslation].PricingID = [Pricing].ID " +
            "AND [PricingTranslation].CultureCode = @Culture " +
            "AND [PricingTranslation].Deleted = 0 " +
            "LEFT JOIN [Pricing] AS [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [PricingTranslation] AS [ProviderPricingTranslation] " +
            "ON [ProviderPricingTranslation].PricingID = [ProviderPricing].ID " +
            "AND [ProviderPricingTranslation].CultureCode = @Culture " +
            "AND [ProviderPricingTranslation].Deleted = 0 " +
            "WHERE [Client].Deleted = 0 " +
            "AND " +
            "( " +
            "[Client].Name like '%' + @Value + '%' " +
            "OR " +
            "[Client].FullName like '%' + @Value + '%' " +
            "OR " +
            "[Client].MobileNumber like '%' + @Value + '%' " +
            "OR " +
            "[Client].ICQ like '%' + @Value + '%' " +
            "OR " +
            "[Client].EmailAddress like '%' + @Value + '%' " +
            "OR " +
            "[Client].USREOU like '%' + @Value + '%' " +
            "OR " +
            "[Client].TIN like '%' + @Value + '%' " +
            "OR " +
            "[Agreement].Name like '%' + @Value + '%' " +
            "OR " +
            "[Pricing].Name like '%' + @Value + '%' " +
            "OR " +
            "[PricingTranslation].Name like '%' + @Value + '%' " +
            "OR " +
            "[ProviderPricing].Name like '%' + @Value + '%' " +
            "OR " +
            "[ProviderPricingTranslation].Name like '%' + @Value + '%' " +
            "OR " +
            "[RegionCode].Value like '%' + @Value + '%' " +
            ") " +
            "AND Client.IsSubClient = 0 ";

        if (!string.IsNullOrEmpty(booleanFilter)) sqlExpression += booleanFilter;

        if (!string.IsNullOrEmpty(roleTypeFilter)) sqlExpression += roleTypeFilter;

        if (searchForClients)
            sqlExpression += "AND ([ClientInRole.ClientTypeRole].Type = 0 OR [Client].IsTemporaryClient = 1) ";
        else
            sqlExpression += "AND [ClientInRole.ClientTypeRole].Type = 1 ";

        if (forReSale.HasValue && forReSale.Value) sqlExpression += "AND [Agreement].[ForReSale] = 1 ";

        sqlExpression += "GROUP BY [Client].ID, [RegionCode].[Value] " +
                         ") [Distincts] " +
                         ") " +
                         "SELECT [Search_CTE].ID " +
                         "FROM [Search_CTE] ";
        if (limit != 0)
            sqlExpression += "WHERE [Search_CTE].RowNumber > @Offset " +
                             "AND [Search_CTE].RowNumber <= @Limit + @Offset ";

        sqlExpression += "ORDER BY RowNumber ";

        var props = new { Value = value, Limit = limit, Offset = offset, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        //Search on root clients
        IEnumerable<long> ids = _connection.Query<long>(
            sqlExpression,
            props
        );

        if (!ids.Any()) {
            //Search on sub clients
            sqlExpression = sqlExpression.Replace("AND Client.IsSubClient = 0", "AND Client.IsSubClient = 1");

            ids = _connection.Query<long>(
                sqlExpression,
                props
            );

            if (!ids.Any()) return clients;

            //Retrieving root client ids of found sub clients 
            ids = _connection.Query<long>(
                "SELECT [ClientSubClient].RootClientID " +
                "FROM [ClientSubClient] " +
                "WHERE [ClientSubClient].Deleted = 0 " +
                "AND [ClientSubClient].SubClientID IN @Ids",
                new { Ids = ids }
            );

            if (!ids.Any()) return clients;
        }

        var clientsProps = new { Ids = ids, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        Type[] clientsTypes = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeRole),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Pricing),
            typeof(ProviderPricing),
            typeof(Pricing),
            typeof(RegionCode),
            typeof(Region),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(Currency)
        };

        Func<object[], Client> clientsMapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[3];
            ClientAgreement clientAgreement = (ClientAgreement)objects[4];
            Agreement agreement = (Agreement)objects[5];
            Organization organization = (Organization)objects[6];
            Pricing pricing = (Pricing)objects[7];
            ProviderPricing providerPricing = (ProviderPricing)objects[8];
            Pricing providerPricingPricing = (Pricing)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];
            Region region = (Region)objects[11];
            ClientUserProfile clientUserProfile = (ClientUserProfile)objects[12];
            User user = (User)objects[13];
            Currency currency = (Currency)objects[14];

            if (!clients.Any(c => c.Id.Equals(client.Id))) {
                if (clientInRole != null) {
                    clientInRole.ClientType = clientType;
                    clientInRole.ClientTypeRole = clientTypeRole;

                    client.ClientInRole = clientInRole;
                }

                if (clientAgreement != null) {
                    if (providerPricing != null) {
                        providerPricing.Pricing = providerPricingPricing;

                        agreement.ProviderPricing = providerPricing;
                    }

                    agreement.Currency = currency;
                    agreement.Pricing = pricing;
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    client.ClientAgreements.Add(clientAgreement);
                }

                if (regionCode != null) {
                    regionCode.Region = region;

                    client.RegionCode = regionCode;
                }

                if (clientUserProfile != null) {
                    clientUserProfile.UserProfile = user;

                    client.ClientManagers.Add(clientUserProfile);
                }

                clients.Add(client);
            } else {
                Client fromList = clients.First(c => c.Id.Equals(client.Id));

                if (clientAgreement != null && !fromList.ClientAgreements.Any(a => a.Id.Equals(clientAgreement.Id))) {
                    if (providerPricing != null) {
                        providerPricing.Pricing = providerPricingPricing;

                        agreement.ProviderPricing = providerPricing;
                    }

                    agreement.Currency = currency;
                    agreement.Pricing = pricing;
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    fromList.ClientAgreements.Add(clientAgreement);
                }

                if (clientUserProfile != null && !client.ClientManagers.Any(p => p.Id.Equals(clientUserProfile.Id))) {
                    clientUserProfile.UserProfile = user;

                    client.ClientManagers.Add(clientUserProfile);
                }
            }

            return client;
        };

        _connection.Query(
            ";WITH [AccountingCashFlow_CTE] " +
            "AS " +
            "( " +
            "SELECT " +
            "[ClientAgreement].ClientID [ClientID] " +
            ",SUM([dbo].[GetExchangedToEuroValue]( " +
            "[OutcomePaymentOrder].[Amount] * -1, " +
            "[Currency].ID, " +
            "GETUTCDATE() " +
            ")) AS [GrossPrice] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [OutcomePaymentOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
            "WHERE [OutcomePaymentOrder].Deleted = 0 " +
            "AND [OutcomePaymentOrder].IsCanceled = 0 " +
            "AND ([OutcomePaymentOrder].SupplyOrganizationAgreementID IS NOT NULL " +
            "OR [OutcomePaymentOrder].ClientAgreementID IS NOT NULL) " +
            "GROUP BY [ClientAgreement].ClientID " +
            "UNION " +
            "SELECT " +
            "[ClientAgreement].ClientID [ClientID] " +
            ", SUM(( " +
            "ISNULL( " +
            "[dbo].GetExchangedToEuroValue( " +
            "[PackingListPackageOrderItem].UnitPrice * " +
            "CONVERT(money, [ProductIncomeItem].Qty) " +
            "+ " +
            "[PackingListPackageOrderItem].[VatAmount] " +
            ", [Currency].[ID] " +
            ", GETUTCDATE()) " +
            ", 0))) AS [GrossPrice] " +
            "FROM [ProductIncome] AS [CalcProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [CalcProductIncome].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [PackingListPackageOrderItem].ID = [ProductIncomeItem].PackingListPackageOrderItemID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [SupplyOrder].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE (CalcProductIncome.ProductIncomeType = 0 " +
            "OR CalcProductIncome.ProductIncomeType = 1) " +
            "AND [ClientAgreement].ClientID IS NOT NULL " +
            "AND [CalcProductIncome].[IsHide] = 0 " +
            "GROUP BY [ClientAgreement].ClientID " +
            "UNION " +
            "SELECT [IncomePaymentOrder].ClientID " +
            ", ISNULL( " +
            "SUM([IncomePaymentOrder].EuroAmount) " +
            ", 0 " +
            ") AS [GrossPrice] " +
            "FROM [IncomePaymentOrder] " +
            "WHERE [IncomePaymentOrder].ClientAgreementID IS NOT NULL " +
            "AND [IncomePaymentOrder].Deleted = 0 " +
            "AND [IncomePaymentOrder].IsCanceled = 0 " +
            "GROUP BY [IncomePaymentOrder].ClientID " +
            "UNION " +
            "SELECT " +
            "[SupplyOrderUkraine].SupplierID [ClientID] " +
            ", SUM( " +
            "[dbo].GetExchangedToEuroValue( " +
            "( " +
            "ISNULL([SupplyOrderUkraineItem].UnitPriceLocal* CONVERT(money, [ProductIncomeItem].Qty) " +
            "+ [SupplyOrderUkraineItem].[VatAmountLocal], 0) " +
            ") " +
            ", [Currency].[ID] " +
            ", GETUTCDATE()) " +
            ") AS [GrossPrice] " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "AND [ProductIncomeItem].Deleted = 0 " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ID = [ProductIncomeItem].SupplyOrderUkraineItemID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [SupplyOrderUkraine].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [ProductIncome].Deleted = 0 " +
            "AND [ProductIncome].ProductIncomeType = 1 " +
            "AND [ProductIncome].[IsHide] = 0 " +
            "GROUP BY [SupplyOrderUkraine].SupplierID " +
            "UNION " +
            "SELECT [ClientAgreement].ClientID " +
            ", ISNULL( " +
            "SUM( " +
            "CASE WHEN [Sale].IsImported = 1 " +
            "THEN " +
            "[dbo].GetExchangedToEuroValue([OrderItem].PricePerItem * ( " +
            "CASE " +
            "WHEN [Currency].[Code] = 'EUR' " +
            "THEN 1 " +
            "ELSE [OrderItem].[ExchangeRateAmount] " +
            "END " +
            ") * [OrderItem].Qty, [Currency].[ID], GETUTCDATE()) " +
            "ELSE " +
            "[dbo].GetExchangedToEuroValue([OrderItem].PricePerItem * ( " +
            "CASE " +
            "WHEN [Currency].[Code] = 'EUR' " +
            "THEN 1 " +
            "ELSE [OrderItem].[ExchangeRateAmount] " +
            "END " +
            ") * [OrderItem].Qty, [Currency].[ID], GETUTCDATE()) " +
            "END " +
            ") " +
            ", 0) * -1 AS [GrossPrice] " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
            "WHERE [OrderItem].Qty > 0 " +
            "AND ( " +
            "[SaleBaseShiftStatus].ShiftStatus IS NULL " +
            "OR " +
            "[SaleBaseShiftStatus].ShiftStatus = 1 " +
            ") " +
            "AND [ClientAgreement].[Deleted] = 0 " +
            "GROUP BY [ClientAgreement].ClientID " +
            "UNION " +
            "SELECT ClientAgreement.ClientID " +
            ", ISNULL( " +
            "SUM( " +
            "[dbo].GetExchangedToEuroValue( " +
            "[SaleInvoiceDocument].ShippingAmount " +
            ", [Currency].[ID] " +
            ", GETUTCDATE() " +
            ") " +
            ") " +
            ", 0) * -1 AS [GrossPrice] " +
            "FROM [Sale] " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [SaleInvoiceDocument] " +
            "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[ID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "LEFT JOIN [SaleBaseShiftStatus] " +
            "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
            "WHERE " +
            "( " +
            "[SaleBaseShiftStatus].ShiftStatus IS NULL " +
            "OR " +
            "[SaleBaseShiftStatus].ShiftStatus = 1 " +
            ") " +
            "AND [ClientAgreement].[Deleted] = 0 " +
            "GROUP BY [ClientAgreement].ClientID " +
            "UNION " +
            "SELECT [ClientAgreement].ClientID " +
            ", ISNULL( " +
            "( " +
            "SUM(([ReSaleItem].PricePerItem * CONVERT(money, [ReSaleItem].Qty)) / " +
            "ISNULL([dbo].GetExchangeRateByCurrencyIdAndCode(null, 'EUR', GETUTCDATE()), 1) " +
            ") " +
            "), 0) * -1 AS [GrossPrice] " +
            "FROM [ReSale] " +
            "LEFT JOIN [ReSaleItem] " +
            "ON [ReSaleItem].ReSaleID = [ReSale].ID " +
            "AND [ReSaleItem].[Deleted] = 0 " +
            "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "WHERE [ReSale].[IsCompleted] = 1 " +
            "GROUP BY [ClientAgreement].ClientID " +
            "UNION " +
            "SELECT [SaleReturn].ClientID " +
            ", ROUND( " +
            "ISNULL(SUM([SaleReturnItem].Amount), 0) " +
            ", 2) AS [GrossPrice] " +
            "FROM [SaleReturn] " +
            "LEFT JOIN [SaleReturnItem] " +
            "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
            "WHERE [SaleReturn].Deleted = 0 " +
            "AND [SaleReturn].IsCanceled = 0 " +
            "GROUP BY [SaleReturn].ClientID " +
            "), " +
            "[TOTAL_CTE] AS ( " +
            "SELECT [AccountingCashFlow_CTE].[ClientID] " +
            ", SUM([AccountingCashFlow_CTE].GrossPrice) [TotalCurrentAmount] " +
            "FROM [AccountingCashFlow_CTE] " +
            "GROUP BY [AccountingCashFlow_CTE].[ClientID]) " +
            "SELECT " +
            "[Client].* " +
            ", ROUND(ISNULL([TOTAL_CTE].[TotalCurrentAmount], 0), 2) [TotalCurrentAmount] " +
            ", [ClientInRole].* " +
            ", [ClientType].* " +
            ", [ClientTypeRole].* " +
            ", [ClientAgreement].* " +
            ", CASE WHEN [ClientAgreement].OriginalClientAmgCode IS NULL THEN 0 ELSE 1 END [FromAmg] " +
            ", CASE WHEN [AmgOriginalClient].[Name] IS NULL THEN [FenixOriginalClient].[Name] ELSE [AmgOriginalClient].[Name] END [OriginalClientName] " +
            ", [Agreement].* " +
            ", [Organization].* " +
            ", [Pricing].* " +
            ", [ProviderPricing].* " +
            ", [ProviderPricing.Pricing].* " +
            ", [RegionCode].* " +
            ", [Region].* " +
            ", [ClientUserProfile].* " +
            ", [Manager].* " +
            ", [Currency].* " +
            "FROM [Client] " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "AND [ClientInRole].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT [ClientType].ID " +
            ",[ClientType].ClientTypeIcon " +
            ",[ClientType].Created " +
            ",[ClientType].Deleted " +
            ",[ClientType].AllowMultiple " +
            ",[ClientType].NetUID " +
            ",[ClientType].Type " +
            ",[ClientType].Updated " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeTranslation].Name IS NOT NULL " +
            "THEN [ClientTypeTranslation].Name " +
            "ELSE [ClientType].Name " +
            "END " +
            ") AS [Name] " +
            "FROM [ClientType] " +
            "LEFT JOIN [ClientTypeTranslation] " +
            "ON [ClientTypeTranslation].ClientTypeID = [ClientType].ID " +
            "AND [ClientTypeTranslation].CultureCode = @Culture " +
            "AND [ClientTypeTranslation].Deleted = 0 " +
            ") AS [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN ( " +
            "SELECT [ClientTypeRole].ID " +
            ",[ClientTypeRole].ClientTypeID " +
            ",[ClientTypeRole].Created " +
            ",[ClientTypeRole].Deleted " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeRoleTranslation].Name IS NOT NULL " +
            "THEN [ClientTypeRoleTranslation].Name " +
            "ELSE [ClientTypeRole].Name " +
            "END " +
            ") AS [Name] " +
            ",( " +
            "CASE " +
            "WHEN [ClientTypeRoleTranslation].Description IS NOT NULL " +
            "THEN [ClientTypeRoleTranslation].Description " +
            "ELSE [ClientTypeRole].Description " +
            "END " +
            ") AS [Description] " +
            ",[ClientTypeRole].NetUID " +
            ",[ClientTypeRole].Updated " +
            "FROM [ClientTypeRole] " +
            "LEFT JOIN [ClientTypeRoleTranslation] " +
            "ON [ClientTypeRoleTranslation].ClientTypeRoleID = [ClientTypeRole].ID " +
            "AND [ClientTypeRoleTranslation].CultureCode = @Culture " +
            "AND [ClientTypeRoleTranslation].Deleted = 0 " +
            ") AS [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Client] [AmgOriginalClient] " +
            "ON [AmgOriginalClient].[SourceAmgCode] = [ClientAgreement].[OriginalClientAmgCode] " +
            "AND [AmgOriginalClient].[SourceAmgCode] != [Client].[SourceAmgCode] " +
            "AND [AmgOriginalClient].[Name] IS NOT NULL " +
            "LEFT JOIN [Client] [FenixOriginalClient] " +
            "ON [FenixOriginalClient].[SourceFenixCode] = [ClientAgreement].[OriginalClientFenixCode] " +
            "AND [FenixOriginalClient].[SourceFenixCode] != [Client].[SourceFenixCode] " +
            "AND [FenixOriginalClient].[Name] IS NOT NULL " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [ProviderPricing] " +
            "ON [ProviderPricing].ID = [Agreement].ProviderPricingID " +
            "LEFT JOIN [views].[PricingView] AS [ProviderPricing.Pricing] " +
            "ON [ProviderPricing.Pricing].ID = [ProviderPricing].BasePricingID " +
            "AND [ProviderPricing.Pricing].CultureCode = @Culture " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Region] " +
            "ON [Region].ID = [RegionCode].RegionID " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].ClientID = [Client].ID " +
            "AND [ClientUserProfile].Deleted = 0 " +
            "LEFT JOIN [User] AS [Manager] " +
            "ON [Manager].ID = [ClientUserProfile].UserProfileID " +
            "LEFT JOIN [TOTAL_CTE] " +
            "ON [TOTAL_CTE].[ClientID] = [Client].[ID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
            "WHERE [Client].ID IN @Ids " +
            (forReSale.HasValue && forReSale.Value ? " AND [Agreement].[ForReSale] = 1 " : ""),
            clientsTypes,
            clientsMapper,
            clientsProps
        );

        Type[] debtsTypes = {
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Agreement),
            typeof(Currency),
            typeof(Pricing),
            typeof(Currency),
            typeof(Sale),
            typeof(ReSale),
            typeof(SaleNumber),
            typeof(User),
            typeof(Transporter),
            typeof(DeliveryRecipient),
            typeof(DeliveryRecipientAddress),
            typeof(BaseLifeCycleStatus),
            typeof(BaseSalePaymentStatus)
        };

        Func<object[], ClientInDebt> debtsMapper = objects => {
            ClientInDebt clientInDebt = (ClientInDebt)objects[0];
            Debt debt = (Debt)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Currency currency = (Currency)objects[3];
            Pricing pricing = (Pricing)objects[4];
            Currency pricingCurrency = (Currency)objects[5];
            Sale sale = (Sale)objects[6];
            ReSale reSale = (ReSale)objects[7];
            SaleNumber saleNumber = (SaleNumber)objects[8];
            User user = (User)objects[9];
            Transporter transporter = (Transporter)objects[10];
            DeliveryRecipient deliveryRecipient = (DeliveryRecipient)objects[11];
            DeliveryRecipientAddress deliveryRecipientAddress = (DeliveryRecipientAddress)objects[12];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[13];
            BaseSalePaymentStatus baseSalePaymentStatus = (BaseSalePaymentStatus)objects[14];

            Client fromList = clients.First(c => c.Id.Equals(clientInDebt.ClientId));

            if (!fromList.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) {
                if (pricing != null) {
                    pricing.Currency = pricingCurrency;

                    agreement.Pricing = pricing;
                }

                debt.Total = Math.Round(debt.Total, 2);
                debt.Days = Convert.ToInt32((DateTime.UtcNow - debt.Created).TotalDays);

                if (sale != null) {
                    sale.SaleNumber = saleNumber;
                    sale.User = user;
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

                agreement.Currency = currency;

                clientInDebt.Debt = debt;
                clientInDebt.Agreement = agreement;
                clientInDebt.Sale = sale;
                clientInDebt.ReSale = reSale;

                fromList.ClientInDebts.Add(clientInDebt);
            }


            return clientInDebt;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ClientInDebt] " +
            "LEFT JOIN [Debt] " +
            "ON [Debt].ID = [ClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientInDebt].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [Agreement].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [PricingCurrency] " +
            "ON [PricingCurrency].ID = [Pricing].CurrencyID " +
            "AND [PricingCurrency].CultureCode = @Culture " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ClientInDebt].[SaleID] " +
            "LEFT JOIN [ReSale] " +
            "ON [ReSale].[ID] = [ClientInDebt].[ReSaleID]  " +
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
            "WHERE [ClientInDebt].Deleted = 0 " +
            "AND [Debt].Total > 0 " +
            "AND [ClientInDebt].ClientID IN @Ids " +
            "ORDER BY [ClientInDebt].[Created] ",
            debtsTypes,
            debtsMapper,
            clientsProps
        );

        Type[] subClientTypes = {
            typeof(ClientSubClient),
            typeof(Client),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Agreement),
            typeof(Currency),
            typeof(Pricing),
            typeof(Currency),
            typeof(Client),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Agreement),
            typeof(Currency),
            typeof(Pricing),
            typeof(Currency),
            typeof(RegionCode),
            typeof(Region)
        };

        Func<object[], ClientSubClient> subClientsMapper = objects => {
            ClientSubClient clientSubClient = (ClientSubClient)objects[0];
            Client rootClient = (Client)objects[1];
            ClientInDebt rootClientInDebt = (ClientInDebt)objects[2];
            Debt rootDebt = (Debt)objects[3];
            Agreement rootAgreement = (Agreement)objects[4];
            Currency rootCurrency = (Currency)objects[5];
            Pricing rootPricing = (Pricing)objects[6];
            Currency rootPricingCurrency = (Currency)objects[7];
            Client subClient = (Client)objects[8];
            ClientInDebt subClientInDebt = (ClientInDebt)objects[9];
            Debt subDebt = (Debt)objects[10];
            Agreement subAgreement = (Agreement)objects[11];
            Currency subCurrency = (Currency)objects[12];
            Pricing subPricing = (Pricing)objects[13];
            Currency subPricingCurrency = (Currency)objects[14];
            RegionCode regionCode = (RegionCode)objects[15];
            Region region = (Region)objects[16];

            Client fromList = clients.First(c => c.Id.Equals(clientSubClient.RootClientId));

            if (!fromList.SubClients.Any(s => s.Id.Equals(clientSubClient.Id))) {
                if (rootClientInDebt != null) {
                    if (rootPricing != null) {
                        rootPricing.Currency = rootPricingCurrency;

                        rootAgreement.Pricing = rootPricing;
                    }

                    rootAgreement.Currency = rootCurrency;

                    rootClientInDebt.Agreement = rootAgreement;
                    rootClientInDebt.Debt = rootDebt;

                    rootClient.ClientInDebts.Add(rootClientInDebt);
                }

                if (subClientInDebt != null) {
                    if (subPricing != null) {
                        subPricing.Currency = subPricingCurrency;

                        subAgreement.Pricing = subPricing;
                    }

                    subAgreement.Currency = subCurrency;

                    subClientInDebt.Agreement = subAgreement;
                    subClientInDebt.Debt = subDebt;

                    subClient.ClientInDebts.Add(subClientInDebt);
                }

                if (regionCode != null) {
                    regionCode.Region = region;

                    subClient.RegionCode = regionCode;
                }

                clientSubClient.RootClient = rootClient;
                clientSubClient.SubClient = subClient;

                fromList.SubClients.Add(clientSubClient);
            } else {
                ClientSubClient subClientFromList = fromList.SubClients.First(s => s.Id.Equals(clientSubClient.Id));

                if (rootClientInDebt != null && !subClientFromList.RootClient.ClientInDebts.Any(d => d.Id.Equals(rootClientInDebt.Id))) {
                    if (rootPricing != null) {
                        rootPricing.Currency = rootPricingCurrency;

                        rootAgreement.Pricing = rootPricing;
                    }

                    rootAgreement.Currency = rootCurrency;

                    rootClientInDebt.Agreement = rootAgreement;
                    rootClientInDebt.Debt = rootDebt;

                    subClientFromList.RootClient.ClientInDebts.Add(rootClientInDebt);
                }

                if (subClientInDebt != null && !subClientFromList.SubClient.ClientInDebts.Any(d => d.Id.Equals(subClientInDebt.Id))) {
                    if (subPricing != null) {
                        subPricing.Currency = subPricingCurrency;

                        subAgreement.Pricing = subPricing;
                    }

                    subAgreement.Currency = subCurrency;

                    subClientInDebt.Agreement = subAgreement;
                    subClientInDebt.Debt = subDebt;

                    subClientFromList.SubClient.ClientInDebts.Add(subClientInDebt);
                }
            }

            return clientSubClient;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ClientSubClient] " +
            "LEFT JOIN [Client] AS [RootClient] " +
            "ON [RootClient].ID = [ClientSubClient].RootClientID " +
            "LEFT JOIN [ClientInDebt] AS [RootClientInDebt] " +
            "ON [RootClientInDebt].ClientID = [RootClient].ID " +
            "AND [RootClientInDebt].Deleted = 0 " +
            "LEFT JOIN [Debt] AS [RootDebt] " +
            "ON [RootDebt].ID = [RootClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] AS [RootAgreement] " +
            "ON [RootAgreement].ID = [RootClientInDebt].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [RootCurrency] " +
            "ON [RootCurrency].ID = [RootAgreement].CurrencyID " +
            "AND [RootCurrency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [RootPricing] " +
            "ON [RootPricing].ID = [RootAgreement].PricingID " +
            "AND [RootPricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [RootPricingCurrency] " +
            "ON [RootPricingCurrency].ID = [RootPricing].CurrencyID " +
            "AND [RootPricingCurrency].CultureCode = @Culture " +
            "LEFT JOIN [Client] AS [SubClient] " +
            "ON [SubClient].ID = [ClientSubClient].SubClientID " +
            "LEFT JOIN [ClientInDebt] AS [SubClientInDebt] " +
            "ON [SubClientInDebt].ClientID = [SubClient].ID " +
            "AND [SubClientInDebt].Deleted = 0 " +
            "LEFT JOIN [Debt] AS [SubDebt] " +
            "ON [SubDebt].ID = [SubClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] AS [SubAgreement] " +
            "ON [SubAgreement].ID = [SubClientInDebt].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [SubCurrency] " +
            "ON [SubCurrency].ID = [SubAgreement].CurrencyID " +
            "AND [SubCurrency].CultureCode = @Culture " +
            "LEFT JOIN [views].[PricingView] AS [SubPricing] " +
            "ON [SubPricing].ID = [SubAgreement].PricingID " +
            "AND [SubPricing].CultureCode = @Culture " +
            "LEFT JOIN [views].[CurrencyView] AS [SubPricingCurrency] " +
            "ON [SubPricingCurrency].ID = [SubPricing].CurrencyID " +
            "AND [SubPricingCurrency].CultureCode = @Culture " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [SubClient].RegionCodeID " +
            "LEFT JOIN [Region] " +
            "ON [Region].ID = [RegionCode].RegionID " +
            "WHERE [ClientSubClient].Deleted = 0 " +
            "AND [ClientSubClient].RootClientID IN @Ids",
            subClientTypes,
            subClientsMapper,
            clientsProps
        );

        foreach (Client client in clients) {
            var groupedGrossPrice = _connection.Query<DocumentValuesResult>(
                    "SELECT [OutcomePaymentOrder].ClientAgreementID AS [ID] " +
                    ", [OutcomePaymentOrder].AfterExchangeAmount * -1 AS [GrossPrice] " +
                    "FROM [OutcomePaymentOrder] " +
                    "WHERE [OutcomePaymentOrder].ClientID = @Id " +
                    "AND [OutcomePaymentOrder].Deleted = 0 " +
                    "AND [OutcomePaymentOrder].IsCanceled = 0 " +
                    "UNION ALL " +
                    "SELECT [IncomePaymentOrder].ClientAgreementID AS [ID] " +
                    ", ( " +
                    "SELECT ROUND(SUM(([IncomePaymentOrderSale].Amount + [IncomePaymentOrderSale].OverpaidAmount) * [IncomePaymentOrderSale].ExchangeRate), 2) " +
                    "FROM [IncomePaymentOrderSale] " +
                    "LEFT JOIN [Sale] " +
                    "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
                    "LEFT JOIN [ReSale] " +
                    "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
                    "LEFT JOIN [IncomePaymentOrder] " +
                    "ON [IncomePaymentOrder].ID = [IncomePaymentOrderSale].IncomePaymentOrderID " +
                    "LEFT JOIN [ClientAgreement] [SaleAgreement] " +
                    "ON [SaleAgreement].ID = [Sale].ClientAgreementID " +
                    "LEFT JOIN [ClientAgreement] [ReSaleAgreement] " +
                    "ON [ReSaleAgreement].ID = [ReSale].ClientAgreementID " +
                    "WHERE [IncomePaymentOrderSale].Deleted = 0 " +
                    "AND [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                    "AND CASE WHEN [Sale].[ID] IS NOT NULL THEN [SaleAgreement].ClientID ELSE [ReSaleAgreement].[ClientID] END = @Id " +
                    ") AS [GrossPrice] " +
                    "FROM [IncomePaymentOrder] " +
                    "LEFT JOIN [IncomePaymentOrderSale] " +
                    "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                    "AND [IncomePaymentOrderSale].Deleted = 0 " +
                    "LEFT JOIN [Sale] " +
                    "ON [Sale].ID = [IncomePaymentOrderSale].SaleID " +
                    "LEFT JOIN [ReSale] " +
                    "ON [ReSale].[ID] = [IncomePaymentOrderSale].[ReSaleID] " +
                    "LEFT JOIN [ClientAgreement] [SaleAgreement] " +
                    "ON [SaleAgreement].ID = [Sale].ClientAgreementID " +
                    "LEFT JOIN [ClientAgreement] [ReSaleAgreement] " +
                    "ON [ReSaleAgreement].ID = [ReSale].ClientAgreementID " +
                    "WHERE CASE WHEN [Sale].[ID] IS NOT NULL THEN [SaleAgreement].ClientID ELSE [ReSaleAgreement].[ClientID] END = @Id " +
                    "AND [IncomePaymentOrder].Deleted = 0 " +
                    "AND [IncomePaymentOrder].IsCanceled = 0 " +
                    "UNION ALL " +
                    "SELECT [IncomePaymentOrder].ClientAgreementID AS [ID] " +
                    ", CASE WHEN [IncomePaymentOrder].AgreementExchangedAmount <> 0 " +
                    "THEN [IncomePaymentOrder].AgreementExchangedAmount " +
                    "ELSE ROUND([IncomePaymentOrder].EuroAmount * [IncomePaymentOrder].AgreementEuroExchangeRate, 2) " +
                    "END AS [GrossPrice] " +
                    "FROM [IncomePaymentOrder] " +
                    "LEFT JOIN [IncomePaymentOrderSale] " +
                    "ON [IncomePaymentOrderSale].IncomePaymentOrderID = [IncomePaymentOrder].ID " +
                    "AND [IncomePaymentOrderSale].Deleted = 0 " +
                    "LEFT JOIN [ClientAgreement] " +
                    "ON [ClientAgreement].ID = [IncomePaymentOrder].ClientAgreementID " +
                    "WHERE [ClientAgreement].ClientID = @Id " +
                    "AND [IncomePaymentOrder].Deleted = 0 " +
                    "AND [IncomePaymentOrder].IsCanceled = 0 " +
                    "AND [IncomePaymentOrderSale].ID IS NULL " +
                    "UNION ALL " +
                    "SELECT [Sale].ClientAgreementID AS [ID] " +
                    ", ( " +
                    "ROUND( " +
                    "( " +
                    "SELECT ISNULL( " +
                    "CASE WHEN [Currency].Code = 'EUR' " +
                    "THEN " +
                    "SUM([OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty)) " +
                    "ELSE " +
                    "SUM( " +
                    "CASE WHEN [ForCalculationSale].IsImported = 1 " +
                    "THEN " +
                    "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty) " +
                    "ELSE " +
                    "[OrderItem].PricePerItem * CONVERT(money, [OrderItem].Qty) " +
                    "END " +
                    ") * MAX([OrderItem].ExchangeRateAmount) " +
                    "END " +
                    ", 0) " +
                    "FROM [Sale] AS [ForCalculationSale] " +
                    "LEFT JOIN [Order] " +
                    "ON [Order].ID = [ForCalculationSale].OrderID " +
                    "LEFT JOIN [OrderItem] " +
                    "ON [OrderItem].OrderID = [Order].ID " +
                    "AND [OrderItem].Deleted = 0 " +
                    "WHERE [ForCalculationSale].ID = [Sale].ID " +
                    "AND [OrderItem].Qty > 0 " +
                    ") " +
                    "+ " +
                    "( " +
                    "SELECT ISNULL( " +
                    "CASE WHEN [Currency].Code = 'EUR' " +
                    "THEN " +
                    "SUM([SaleInvoiceDocument].ShippingAmountEur) " +
                    "ELSE " +
                    "SUM([SaleInvoiceDocument].ShippingAmount) " +
                    "END " +
                    ", 0) " +
                    "FROM [Sale] AS [ForCalculationSale] " +
                    "LEFT JOIN [Order] " +
                    "ON [Order].ID = [ForCalculationSale].OrderID " +
                    "LEFT JOIN [OrderItem] " +
                    "ON [OrderItem].OrderID = [Order].ID " +
                    "LEFT JOIN [SaleInvoiceDocument] " +
                    "ON [SaleInvoiceDocument].ID = [Sale].SaleInvoiceDocumentID " +
                    "AND [OrderItem].Deleted = 0 " +
                    "WHERE [ForCalculationSale].ID = [Sale].ID " +
                    ") " +
                    ", 2) " +
                    ") * -1 AS [GrossPrice] " +
                    "FROM [Sale] " +
                    "LEFT JOIN [SaleBaseShiftStatus] " +
                    "ON [SaleBaseShiftStatus].ID = [Sale].ShiftStatusID " +
                    "LEFT JOIN [Workplace] " +
                    "ON [Workplace].ID = [Sale].WorkplaceID " +
                    "LEFT JOIN [ClientAgreement] " +
                    "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
                    "LEFT JOIN [Agreement] " +
                    "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [Agreement].CurrencyID " +
                    "WHERE [ClientAgreement].ClientID = @Id " +
                    "AND ( " +
                    "[SaleBaseShiftStatus].ShiftStatus IS NULL " +
                    "OR " +
                    "[SaleBaseShiftStatus].ShiftStatus = 1 " +
                    ") " +
                    "UNION ALL " +
                    "SELECT [SaleReturn].ClientAgreementID AS [ID] " +
                    ", ( " +
                    "SELECT ISNULL(SUM([SaleReturnItem].Amount * [SaleReturnItem].ExchangeRateAmount), 0) " +
                    "FROM [SaleReturn] AS [CalcReturn] " +
                    "LEFT JOIN [SaleReturnItem] " +
                    "ON [CalcReturn].ID = [SaleReturnItem].SaleReturnID " +
                    "LEFT JOIN [OrderItem] " +
                    "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
                    "LEFT JOIN [Order] " +
                    "ON [Order].ID = [OrderItem].OrderID " +
                    "WHERE [CalcReturn].ID = [SaleReturn].ID " +
                    "AND [CalcReturn].ClientID = @Id " +
                    ") " +
                    "AS [GrossPrice] " +
                    "FROM [SaleReturn] " +
                    "LEFT JOIN [SaleReturnItem] " +
                    "ON [SaleReturn].ID = [SaleReturnItem].SaleReturnID " +
                    "LEFT JOIN [OrderItem] " +
                    "ON [OrderItem].ID = [SaleReturnItem].OrderItemID " +
                    "LEFT JOIN [Order] " +
                    "ON [Order].ID = [OrderItem].OrderID " +
                    "WHERE [SaleReturn].Deleted = 0 " +
                    "AND [SaleReturn].IsCanceled = 0 " +
                    "AND [SaleReturn].ClientID = @Id " +
                    "UNION ALL " +
                    "SELECT [ClientAgreement].ID AS [ID] " +
                    ", ROUND( " +
                    "( " +
                    "SELECT ISNULL( " +
                    "( " +
                    "SUM(([ReSaleItem].PricePerItem * CONVERT(money, [ReSaleItem].Qty)) / " +
                    "ISNULL([dbo].GetExchangeRateByCurrencyIdAndCode(null, Currency.Code, GETUTCDATE()), 1) " +
                    ") " +
                    "), 0 " +
                    ") " +
                    "FROM [ReSale] AS [CalcReturn] " +
                    "LEFT JOIN [ReSaleItem] " +
                    "ON [ReSaleItem].ReSaleID = [CalcReturn].ID " +
                    "AND [ReSaleItem].[Deleted] = 0 " +
                    "AND [ReSaleItem].[ReSaleAvailabilityID] IS NOT NULL " +
                    "LEFT JOIN [ClientAgreement] " +
                    "ON [ClientAgreement].ID = [CalcReturn].ClientAgreementID " +
                    "LEFT JOIN [Agreement] " +
                    "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                    "LEFT JOIN [Currency] " +
                    "ON [Currency].ID = [Agreement].CurrencyID " +
                    "WHERE [CalcReturn].ID = [ReSale].ID " +
                    ") " +
                    ", 2) * -1 AS [GrossPrice] " +
                    "FROM [ReSale] " +
                    "LEFT JOIN [ClientAgreement] " +
                    "ON [ClientAgreement].ID = [ReSale].ClientAgreementID " +
                    "WHERE [ReSale].Deleted = 0 " +
                    "AND [ClientAgreement].ClientId = @Id " +
                    "AND [ReSale].[IsCompleted] = 1 ",
                    new { client.Id })
                .GroupBy(result => result.Id)
                .Select(group => new {
                    Id = group.Key,
                    SumGrossPrice = group.Sum(item => item.GrossPrice)
                });

            client.ClientAgreements.ToList().ForEach(clientAgreement =>
                clientAgreement.CurrentAmount = groupedGrossPrice
                    .Where(g => g.Id.Equals(clientAgreement.Id))
                    .Select(e => e.SumGrossPrice)
                    .FirstOrDefault());
        }

        return clients;
    }

    public List<Client> GetAllFiltered(string booleanFilter, string roleTypeFilter) {
        string sqlQuery =
            "SELECT * FROM [Client] " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "LEFT JOIN [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
            "LEFT JOIN [ClientTypeRoleTranslation] " +
            "ON [ClientTypeRoleTranslation].ClientTypeRoleID = [ClientTypeRole].ID " +
            "AND [ClientTypeRoleTranslation].CultureCode = @CultureCode " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [Client].Deleted = 0 ";

        if (!string.IsNullOrEmpty(booleanFilter)) sqlQuery += booleanFilter;

        if (!string.IsNullOrEmpty(roleTypeFilter)) sqlQuery += roleTypeFilter;

        return _connection.Query<Client, ClientInRole, ClientTypeRole, ClientTypeRoleTranslation, RegionCode, Client>(
            sqlQuery,
            (client, clientInRole, clientTypeRole, clientTypeRoleTranslation, regionCode) => {
                if (clientInRole != null)
                    if (clientTypeRole != null) {
                        clientTypeRole.ClientTypeRoleTranslations.Add(clientTypeRoleTranslation);
                        clientInRole.ClientTypeRole = clientTypeRole;
                    }

                client.ClientInRole = clientInRole;
                client.RegionCode = regionCode;

                return client;
            },
            new { CultureCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public List<Client> GetAllFromSearchWithDebtInfo(string value, bool allClients, Guid userNetId) {
        List<Client> toReturn = new();

        string sqlExpression =
            "SELECT [Client].* " +
            ",[ClientUserProfile].* " +
            ",[User].* " +
            ",[ClientInDebt].* " +
            ",[Debt].* " +
            ",( " +
            "CASE " +
            "WHEN [Debt].ID IS NOT NULL " +
            "THEN dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, @FromDate) " +
            "ELSE NULL " +
            "END " +
            ") AS [EuroTotal] " +
            ",[Agreement].* " +
            ",[RegionCode].* " +
            "FROM [Client] " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].ClientID = [Client].ID " +
            "AND [ClientUserProfile].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ClientUserProfile].UserProfileID " +
            "LEFT JOIN [ClientInDebt] " +
            "ON [ClientInDebt].ClientID = [Client].ID " +
            "AND [ClientInDebt].Deleted = 0 " +
            "LEFT JOIN [Debt] " +
            "ON [Debt].ID = [ClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientInDebt].AgreementID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [Client].Deleted = 0 " +
            "AND ([Debt].Deleted = 0 OR [Debt].ID IS NULL) " +
            "AND ([Client].FullName like '%' + @Value + '%' OR [RegionCode].[Value] like '%' + @Value + '%') ";

        if (!allClients) sqlExpression += "AND [User].NetUID = @UserNetId";

        Type[] types = {
            typeof(Client),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(ClientInDebt),
            typeof(Debt),
            typeof(Agreement),
            typeof(RegionCode)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientUserProfile clientUserProfile = (ClientUserProfile)objects[1];
            User user = (User)objects[2];
            ClientInDebt clientInDebt = (ClientInDebt)objects[3];
            Debt debt = (Debt)objects[4];
            Agreement agreement = (Agreement)objects[5];
            RegionCode regionCode = (RegionCode)objects[6];

            if (toReturn.Any(c => c.Id.Equals(client.Id))) {
                Client fromList = toReturn.First(c => c.Id.Equals(client.Id));

                if (clientInDebt != null && debt != null && !fromList.ClientInDebts.Any(c => c.Id.Equals(clientInDebt.Id))) {
                    clientInDebt.Debt = debt;
                    clientInDebt.Agreement = agreement;

                    fromList.ClientInDebts.Add(clientInDebt);
                }

                if (clientUserProfile != null && !fromList.ClientManagers.Any(m => m.Id.Equals(clientUserProfile.Id))) {
                    clientUserProfile.UserProfile = user;

                    fromList.ClientManagers.Add(clientUserProfile);
                }
            } else {
                if (clientInDebt != null && debt != null) {
                    clientInDebt.Debt = debt;
                    clientInDebt.Agreement = agreement;

                    client.ClientInDebts.Add(clientInDebt);
                }

                if (clientUserProfile != null) {
                    clientUserProfile.UserProfile = user;

                    client.ClientManagers.Add(clientUserProfile);
                }

                client.RegionCode = regionCode;

                toReturn.Add(client);
            }

            return client;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Value = value, UserNetId = userNetId, FromDate = DateTime.UtcNow }
        );

        return toReturn;
    }

    public IEnumerable<Client> GetAllFromSearch(string value) {
        return _connection.Query<Client>(
            "SELECT [Client].* " +
            "FROM [Client] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [Client].Deleted = 0 " +
            "AND (" +
            "[Client].FullName like N'%' + @Value + N'%' " +
            "OR " +
            "[Client].[Name] like N'%' + @Value + N'%'" +
            "OR " +
            "[RegionCode].[Value] like N'%' + @Value + N'%'" +
            ") " +
            "ORDER BY [Client].FullName, [Client].Name",
            new { Value = value }
        );
    }

    public IEnumerable<Client> GetAllFromSearchByNameOrRegionCode(string value) {
        return _connection.Query<Client, RegionCode, Client>(
            "SELECT TOP(44) * " +
            "FROM [Client] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [Client].Deleted = 0 " +
            "AND (" +
            "PATINDEX(@Value, [Client].FullName) > 0 " +
            "OR " +
            "PATINDEX(@Value, [Client].[Name]) > 0 " +
            "OR " +
            "PATINDEX(@Value, [RegionCode].[Value]) > 0 " +
            ") " +
            "ORDER BY [Client].FullName, [Client].Name",
            (client, regionCode) => {
                client.RegionCode = regionCode;

                return client;
            },
            new { Value = $"%{value}%" }
        );
    }

    public long SetIsForRetailByNetId(Guid netId) {
        return _connection.Execute(
            "UPDATE [Client] " +
            "SET [IsForRetail] = 1 " +
            "WHERE [NetUID] = @NetId ",
            new { NetId = netId }
        );
    }

    public long DeselectIsForRetailByNetId(Guid netId) {
        return _connection.Execute(
            "UPDATE [Client] " +
            "SET [IsForRetail] = 0 " +
            "WHERE [NetUID] = @NetId ",
            new { NetId = netId }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Client SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE Client SET " +
            "Deleted = 1 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void UpdateAbbreviation(Client client) {
        _connection.Execute(
            "UPDATE [Client] " +
            "SET Abbreviation = @Abbreviation " +
            "FROM [Client] " +
            "WHERE NetUID = @NetUid",
            client
        );
    }

    public dynamic GetDebtTotalsForClientStructureByClientNetId(Guid netId) {
        return _connection.Query<dynamic>(
                ";WITH " +
                "RootClientStructure_CTE " +
                "AS " +
                "( " +
                "SELECT SubClient.ID " +
                "FROM ClientSubClient " +
                "LEFT JOIN Client AS SubClient " +
                "ON ClientSubClient.SubClientID = SubClient.ID " +
                "LEFT JOIN Client AS RootClient " +
                "ON ClientSubClient.RootClientID = RootClient.ID " +
                "WHERE ClientSubClient.Deleted = 0 AND RootClient.NetUID = @NetId " +
                "), " +
                "AgreementEUR_CTE " +
                "AS " +
                "( " +
                "SELECT Agreement.ID " +
                "FROM ClientAgreement " +
                "LEFT JOIN Agreement " +
                "ON Agreement.ID = ClientAgreement.AgreementID " +
                "LEFT JOIN Currency " +
                "ON Agreement.CurrencyID = Currency.ID " +
                "WHERE ClientAgreement.ClientID IN (SELECT * FROM RootClientStructure_CTE) " +
                "AND Currency.Code = 'EUR' " +
                "), " +
                "AgreementLocal_CTE " +
                "AS " +
                "( " +
                "SELECT Agreement.ID " +
                "FROM ClientAgreement " +
                "LEFT JOIN Agreement " +
                "ON Agreement.ID = ClientAgreement.AgreementID " +
                "LEFT JOIN Currency " +
                "ON Agreement.CurrencyID = Currency.ID " +
                "WHERE ClientAgreement.ClientID IN (SELECT * FROM RootClientStructure_CTE) " +
                "AND Currency.Code != 'EUR' " +
                "), " +
                "ClientStructureDebtsEUR_CTE " +
                "AS " +
                "( " +
                "SELECT ROUND(SUM(Debt.Total), 2) AS TotalEuro " +
                "FROM ClientInDebt " +
                "LEFT JOIN Debt " +
                "ON Debt.ID = ClientInDebt.DebtID " +
                "WHERE ClientInDebt.AgreementID IN (SELECT * FROM AgreementEUR_CTE) " +
                "AND Debt.Deleted = 0 " +
                "AND ClientInDebt.Deleted = 0 " +
                "AND Debt.Total != 0 " +
                "), " +
                "ClientStructureDebtsLocal_CTE " +
                "AS " +
                "( " +
                "SELECT ROUND(SUM(Debt.Total), 2) AS TotalLocal " +
                "FROM ClientInDebt " +
                "LEFT JOIN Debt " +
                "ON Debt.ID = ClientInDebt.DebtID " +
                "WHERE ClientInDebt.AgreementID IN (SELECT * FROM AgreementLocal_CTE) " +
                "AND Debt.Deleted = 0 " +
                "AND ClientInDebt.Deleted = 0 " +
                ") " +
                "SELECT (SELECT * FROM ClientStructureDebtsEUR_CTE) AS TotalEuro " +
                ",(SELECT * FROM ClientStructureDebtsLocal_CTE) AS TotalLocal",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public dynamic GetDebtTotalsForClientStructureWithRootByClientNetId(Guid netId, bool isFromEcommerce = false) {
        string query =
            ";WITH " +
            "RootClientStructure_CTE " +
            "AS " +
            "( " +
            "SELECT SelectedClient.ID AS ClientID " +
            "FROM Client AS SelectedClient " +
            "WHERE SelectedClient.NetUID = @NetId " +
            "UNION ALL " +
            "SELECT SubClient.ID " +
            "FROM ClientSubClient " +
            "JOIN Client AS SubClient " +
            "ON ClientSubClient.SubClientID = SubClient.ID " +
            "JOIN RootClientStructure_CTE " +
            "ON ClientSubClient.RootClientID = RootClientStructure_CTE.ClientID " +
            "WHERE ClientSubClient.Deleted = 0 " +
            "), " +
            "AgreementEUR_CTE " +
            "AS " +
            "( " +
            "SELECT Agreement.ID " +
            "FROM ClientAgreement " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "WHERE ClientAgreement.ClientID IN (SELECT * FROM RootClientStructure_CTE) " +
            "AND Currency.Code = 'EUR' " +
            "), " +
            "AgreementLocal_CTE " +
            "AS " +
            "( " +
            "SELECT Agreement.ID " +
            "FROM ClientAgreement " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "WHERE ClientAgreement.ClientID IN (SELECT * FROM RootClientStructure_CTE) ";

        query +=
            isFromEcommerce
                ? "AND Agreement.ForReSale = 0 "
                : "";

        query +=
            "AND Currency.Code != 'EUR' " +
            "), " +
            "ClientStructureDebtsEUR_CTE " +
            "AS " +
            "( " +
            "SELECT ROUND(SUM(Debt.Total), 2) AS TotalEuro " +
            "FROM ClientInDebt " +
            "LEFT JOIN Debt " +
            "ON Debt.ID = ClientInDebt.DebtID " +
            "WHERE ClientInDebt.AgreementID IN (SELECT * FROM AgreementEUR_CTE) " +
            "AND Debt.Deleted = 0 " +
            "AND ClientInDebt.Deleted = 0 " +
            "AND Debt.Total != 0 " +
            "), " +
            "ClientStructureDebtsLocal_CTE " +
            "AS " +
            "( " +
            "SELECT ROUND(SUM(Debt.Total), 2) AS TotalLocal " +
            "FROM ClientInDebt " +
            "LEFT JOIN Debt " +
            "ON Debt.ID = ClientInDebt.DebtID " +
            "WHERE ClientInDebt.AgreementID IN (SELECT * FROM AgreementLocal_CTE) " +
            "AND Debt.Deleted = 0 " +
            "AND ClientInDebt.Deleted = 0 " +
            ") " +
            "SELECT (SELECT * FROM ClientStructureDebtsEUR_CTE) AS TotalEuro " +
            ",(SELECT * FROM ClientStructureDebtsLocal_CTE) AS TotalLocal";

        return _connection.Query<dynamic>(
                query,
                new { NetId = netId }
            )
            .SingleOrDefault();
    }


    public dynamic GetAvgByClientAndProduct(Guid clientNetId, Guid productNetId) {
        return _connection.Query<dynamic>(
                "WITH ProductAvg_CTE (SaleTotalCount, TotalQuantity) " +
                "AS " +
                "( " +
                "SELECT COUNT(DISTINCT Sale.ID) AS SaleTotalCount, SUM(OrderItem.Qty) AS TotalQuantity FROM Client " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.ClientID = Client.ID " +
                "LEFT JOIN Sale " +
                "ON Sale.ClientAgreementID = ClientAgreement.ID " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = Sale.OrderID " +
                "LEFT JOIN OrderItem " +
                "ON OrderItem.OrderID = [Order].ID " +
                "LEFT JOIN Product " +
                "ON OrderItem.ProductID = Product.ID " +
                "WHERE Client.NetUID = @ClientNetId " +
                "AND Product.NetUID = @ProductNetId " +
                ") " +
                "SELECT TotalQuantity / SaleTotalCount AS [Avg] " +
                "FROM  ProductAvg_CTE ",
                new { ClientNetId = clientNetId, ProductNetId = productNetId }
            )
            .Single();
    }

    public long GetAllTotalAmount(ClientTypeType type) {
        return _connection.Query<long>(
                "SELECT COUNT(DISTINCT [Client].ID) " +
                "FROM [Client] " +
                "LEFT JOIN [ClientInRole] " +
                "ON [ClientInRole].ClientID = [Client].ID " +
                "AND [ClientInRole].Deleted = 0 " +
                "LEFT JOIN [ClientType] " +
                "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
                "WHERE [Client].IsSubClient = 0 " +
                "AND [Client].Deleted = 0 " +
                "AND [ClientType].Type = @Type",
                new { Type = type }
            )
            .SingleOrDefault();
    }

    public List<dynamic> GetTopBySales() {
        return _connection.Query<dynamic>(
                "WITH SaleTop (RegionCode, SaleID, Price) " +
                "AS " +
                "( " +
                "SELECT RegionCode.Value AS RegionCode, " +
                "Sale.ID AS SaleID, " +
                "SUM( " +
                "(ProductPricing.Price + (ProductPricing.Price * (dbo.GetPricingExtraCharge(Pricing.NetUID) - ISNULL(ProductGroupDiscount.DiscountRate,0))) / 100) * OrderItem.Qty) " +
                "AS Price " +
                "FROM Sale " +
                "LEFT OUTER JOIN ClientAgreement " +
                "ON ClientAgreement.ID = Sale.ClientAgreementID AND ClientAgreement.Deleted = 0 " +
                "LEFT OUTER JOIN Agreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID AND Agreement.Deleted = 0 " +
                "LEFT OUTER JOIN Pricing " +
                "ON Pricing.ID = Agreement.PricingID AND Pricing.Deleted = 0 " +
                "LEFT OUTER JOIN Client " +
                "ON Client.ID = ClientAgreement.ClientID AND Client.Deleted = 0 " +
                "LEFT OUTER JOIN [Order] " +
                "ON [Order].ID = Sale.OrderId AND [Order].Deleted = 0 " +
                "LEFT OUTER JOIN OrderItem " +
                "ON OrderItem.OrderID = [Order].ID AND OrderItem.Deleted = 0 " +
                "LEFT OUTER JOIN Product " +
                "ON Product.ID = OrderItem.ProductID AND Product.Deleted = 0 " +
                "LEFT JOIN RegionCode " +
                "ON Client.RegionCodeId = RegionCode.ID " +
                "LEFT OUTER JOIN ProductProductGroup " +
                "ON ProductProductGroup.ID = ( " +
                "SELECT TOP (1) ProductProductGroup.ID " +
                "FROM ProductProductGroup " +
                "WHERE ProductProductGroup.ProductID = Product.ID " +
                "AND ProductProductGroup.Deleted = 0 " +
                ") " +
                "LEFT OUTER JOIN ProductGroupDiscount " +
                "ON ClientAgreement.ID = ProductGroupDiscount.ClientAgreementID " +
                "AND ProductProductGroup.ProductGroupID = ProductGroupDiscount.ProductGroupID " +
                "AND ProductGroupDiscount.IsActive = 1 " +
                "LEFT JOIN ProductPricing " +
                "ON ProductPricing.ID = ( " +
                "SELECT TOP (1) ProductPricing.ID " +
                "FROM ProductPricing " +
                "LEFT OUTER JOIN Pricing " +
                "ON ProductPricing.PricingId = Pricing.ID " +
                "WHERE ProductPricing.ProductID = Product.ID " +
                "AND ProductPricing.Deleted = 0 " +
                ") " +
                "GROUP BY Sale.ID, " +
                "RegionCode.Value " +
                ") " +
                "SELECT TOP(5) RegionCode, COUNT(SaleID) AS TotalSales, ROUND(SUM(Price), 2) AS TotalPrice FROM SaleTop " +
                "GROUP BY RegionCode " +
                "ORDER BY TotalSales DESC, RegionCode"
            )
            .ToList();
    }

    public List<dynamic> GetTopByOnlineOrders() {
        return _connection.Query<dynamic>(
                "WITH TopOnlineShopCients_CTE (RegionCode, SaleId, TotalPrice) " +
                "AS( " +
                "SELECT " +
                "RegionCode.Value AS RegionCode, " +
                "Sale.ID, " +
                "SUM(" +
                "(ProductPricing.Price + (ProductPricing.Price * (dbo.GetPricingExtraCharge(Pricing.NetUID) - ISNULL(ProductGroupDiscount.DiscountRate,0))) / 100) * OrderItem.Qty" +
                ") AS Price " +
                "FROM Sale " +
                "LEFT OUTER JOIN [Order] " +
                "ON [Order].ID = Sale.OrderID AND [Order].Deleted = 0 " +
                "LEFT OUTER JOIN OrderItem " +
                "ON OrderItem.OrderID = [Order].ID AND OrderItem.Deleted = 0 " +
                "LEFT OUTER JOIN Product " +
                "ON Product.ID = OrderItem.ProductID AND Product.Deleted = 0 " +
                "LEFT OUTER JOIN ProductProductGroup " +
                "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
                "LEFT JOIN ProductPricing " +
                "ON ProductPricing.ID = ( " +
                "SELECT TOP (1) ProductPricing.ID " +
                "FROM ProductPricing " +
                "WHERE ProductPricing.ProductID = Product.ID " +
                "AND ProductPricing.Deleted = 0 " +
                ") " +
                "LEFT OUTER JOIN ClientAgreement " +
                "ON ClientAgreement.ID = [Order].ClientAgreementID AND ClientAgreement.Deleted = 0 " +
                "LEFT OUTER JOIN Agreement " +
                "ON Agreement.ID = ClientAgreement.AgreementID AND Agreement.Deleted = 0 " +
                "LEFT OUTER JOIN Pricing " +
                "ON Pricing.ID = Agreement.PricingID AND Pricing.Deleted = 0 " +
                "LEFT OUTER JOIN Client " +
                "ON Client.ID = ClientAgreement.ClientID AND Client.Deleted = 0 " +
                "LEFT OUTER JOIN RegionCode " +
                "ON RegionCode.ID = Client.RegionCodeID " +
                "LEFT OUTER JOIN ProductGroupDiscount " +
                "ON ProductGroupDiscount.ID = ( " +
                "SELECT TOP(1) ID " +
                "FROM ProductGroupDiscount " +
                "WHERE ProductGroupId = ProductProductGroup.ProductGroupID " +
                "AND ClientAgreementId = ClientAgreement.ID " +
                "AND IsActive = 1 " +
                ") " +
                "WHERE [Order].OrderSource = 0 " +
                "GROUP BY RegionCode.Value, Sale.ID " +
                ") " +
                "SELECT TOP(5) RegionCode, COUNT(SaleId) AS TotalOrders, ROUND(SUM(TotalPrice), 2) AS TotalPrice " +
                "FROM TopOnlineShopCients_CTE " +
                "GROUP BY RegionCode " +
                "ORDER BY TotalPrice DESC, TotalOrders DESC "
            )
            .ToList();
    }

    public List<OrderItemByClientModel> GetOrderItemsByClientNetId(Guid clientNetId) {
        List<OrderItemByClientModel> clientOrderItems = new();

        string sqlQuery = "SELECT " +
                          "[OrderItem].*, " +
                          "[Product].* " +
                          "FROM [OrderItem] " +
                          "LEFT JOIN [Product] " +
                          "ON [Product].[ID] = [OrderItem].[ProductID] " +
                          "LEFT JOIN [Order] " +
                          "ON [Order].[ID] = [OrderItem].[OrderID] " +
                          "LEFT JOIN [Sale] " +
                          "ON [Sale].[OrderID] = [Order].[ID] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ID] = [Order].[ClientAgreementID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
                          "WHERE [OrderItem].[Deleted] = 0  " +
                          "AND [Sale].[ChangedToInvoice] IS NOT NULL " +
                          "AND [Client].[NetUID] = @netId " +
                          "ORDER BY [OrderItem].[Qty]-[OrderItem].[ReturnedQty] DESC ";

        Type[] types = {
            typeof(OrderItem),
            typeof(Product)
        };

        Func<object[], OrderItem> mapper = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            Product product = (Product)objects[1];

            clientOrderItems.Add(new OrderItemByClientModel {
                ProductVendorCode = product.VendorCode,
                ProductName = product.Name,
                Qty = orderItem.Qty - orderItem.ReturnedQty
            });

            return orderItem;
        };

        _connection.Query(
            sqlQuery,
            types,
            mapper,
            new { netId = clientNetId }
        );

        return clientOrderItems;
    }

    public List<ClientWithPurchaseActivityModel> GetAllWithPurchaseActivity(long limit, long offset, bool forMyClient, long userId) {
        List<ClientWithPurchaseActivityModel> clients = new();

        string cond = string.Empty;

        object parameters;

        if (forMyClient && userId != 0) {
            cond += "AND [User].[ID] = @Id ";
            parameters = new { Limit = limit, Offset = offset, Id = userId };
        } else {
            parameters = new { Limit = limit, Offset = offset };
        }

        string sqlQuery = ";WITH [Search_CTE] " +
                          "AS " +
                          "( " +
                          "SELECT " +
                          "ROW_NUMBER() OVER (ORDER BY [Client].[NetUID] DESC) AS RowNumber " +
                          ",[Client].[NetUID] " +
                          "FROM [Client] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ClientID] = [Client].[ID] " +
                          "LEFT JOIN [Sale] " +
                          "ON [Sale].[ClientAgreementID] = [ClientAgreement].[ID] " +
                          "LEFT JOIN [User] " +
                          "On [User].[ID] = [Sale].[UserID] " +
                          "WHERE [Client].[Deleted] = 0 " +
                          "AND [ClientAgreement].[Deleted] = 0 " +
                          "GROUP BY [Client].[NetUID] " +
                          cond +
                          ") " +
                          "SELECT " +
                          "[Client].[NetUID] AS [ClientNetID] " +
                          ",[Client].[Created] AS [ClientCreated] " +
                          ",CASE WHEN [Client].[Name] IS NULL OR [Client].[Name] = '' THEN [Client].[FullName] ELSE [Client].[Name] END [ClientName] " +
                          ",[User].[LastName] + ' ' + [User].[FirstName] + ' ' + [User].[MiddleName] AS [ManagerName] " +
                          ",[Sale].[Created] AS [SaleCreated] " +
                          ",CASE WHEN [Sale].[ChangedToInvoice] IS NULL THEN 0 ELSE 1 END [SaleChangeInvoice] " +
                          "FROM [Client] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [ClientAgreement].[ClientID] = [Client].[ID] " +
                          "LEFT JOIN [Sale] " +
                          "ON [Sale].[ClientAgreementID] = [ClientAgreement].[ID] " +
                          "LEFT JOIN [User] " +
                          "ON [User].[ID] = [Sale].[UserID] " +
                          "WHERE [Client].[NetUID] IN ( " +
                          "SELECT [Search_CTE].[NetUID] " +
                          "FROM [Search_CTE] " +
                          "WHERE [Search_CTE].RowNumber > @Offset " +
                          "AND [Search_CTE].RowNumber <= @Limit + @Offset ) " +
                          "AND [ClientAgreement].[Deleted] = 0 " +
                          cond +
                          "GROUP BY [Client].[NetUID] " +
                          ",[Client].[Created] " +
                          ",CASE WHEN [Client].[Name] IS NULL OR [Client].[Name] = '' THEN [Client].[FullName] ELSE [Client].[Name] END " +
                          ",[User].[LastName] + ' ' + [User].[FirstName] + ' ' + [User].[MiddleName] " +
                          ",[Sale].[Created] " +
                          ",CASE WHEN [Sale].[ChangedToInvoice] IS NULL THEN 0 ELSE 1 END";

        Type[] types = {
            typeof(Guid),
            typeof(DateTime),
            typeof(string),
            typeof(string),
            typeof(DateTime?),
            typeof(bool)
        };

        Func<object[], Guid> mapper = objects => {
            Guid clientNetId = (Guid)objects[0];
            DateTime clientCreated = (DateTime)objects[1];
            string clientName = (string)objects[2];
            string managerName = (string)objects[3];
            DateTime? lastSale = (DateTime?)objects[4];
            int saleChangeInvoice = (int)objects[5];

            ClientWithPurchaseActivityModel model;

            if (clients.Any(x => x.ClientNetId == clientNetId)) {
                model = clients.First(x => x.ClientNetId == clientNetId);

                if (lastSale.HasValue) {
                    if (!model.LastSale.HasValue || lastSale > model.LastSale.Value)
                        model.LastSale = lastSale;

                    model.QtyDayFromLastOrder = (DateTime.Today.Year - lastSale.Value.Year) * 365 + DateTime.Today.DayOfYear - lastSale.Value.DayOfYear;
                }

                if (!string.IsNullOrEmpty(managerName) && string.IsNullOrEmpty(model.ManagerName))
                    model.ManagerName = managerName;

                model.IsExistAccount = saleChangeInvoice == 1;
            } else {
                model = new ClientWithPurchaseActivityModel {
                    ClientName = clientName,
                    CreatedClient = clientCreated,
                    LastSale = lastSale,
                    ManagerName = managerName,
                    IsExistAccount = saleChangeInvoice == 1,
                    ClientNetId = clientNetId
                };

                clients.Add(model);

                if (lastSale.HasValue) model.QtyDayFromLastOrder = (DateTime.Today.Year - lastSale.Value.Year) * 365 + DateTime.Today.DayOfYear - lastSale.Value.DayOfYear;
            }

            return clientNetId;
        };

        _connection.Query(sqlQuery, types, mapper, parameters, splitOn: "ClientNetID,ClientCreated,ClientName,ManagerName,SaleCreated,SaleChangeInvoice");

        return clients;
    }

    public List<Client> GetAllShopClients() {
        return _connection.Query<Client>(
            "SELECT Client.* FROM Client " +
            "LEFT JOIN ClientInRole " +
            "ON ClientInRole.ClientID = Client.ID " +
            "LEFT JOIN ClientTypeRole " +
            "ON ClientTypeRole.ID = ClientInRole.ClientTypeRoleID " +
            "WHERE ClientTypeRole.Name = N'ShopClient' " +
            "AND Client.Deleted = 0 "
        ).ToList();
    }

    public long GetIdByNetId(Guid clientNetId) {
        return _connection.Query<long>(
            "SELECT [Client].[ID] " +
            "FROM [Client] " +
            "WHERE [Client].[NetUID] = @NetId",
            new { NetId = clientNetId }).SingleOrDefault();
    }

    public List<Client> GetClientsNotToBuyAnything(DateTime from, DateTime to, string value) {
        string sqlQuery = ";WITH CLIENTS_IDS_BOUGHT_SOMETHING AS ( " +
                          "SELECT [Client].[ID] FROM [Sale] " +
                          "LEFT JOIN [ClientAgreement] " +
                          "ON [Sale].[ClientAgreementID] =  [ClientAgreement].[ID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
                          "WHERE [Sale].[Deleted] = 0 " +
                          "AND [Sale].[ChangedToInvoice] IS NOT NULL " +
                          "AND [Sale].[Created] >= @From " +
                          "AND [Sale].[Created] <= @To " +
                          "GROUP BY [Client].[ID] " +
                          ") " +
                          "SELECT * FROM [Client] " +
                          "WHERE [Client].[ID] NOT IN (SELECT [CLIENTS_IDS_BOUGHT_SOMETHING].[ID] FROM [CLIENTS_IDS_BOUGHT_SOMETHING]) ";
        if (!string.IsNullOrEmpty(value))
            sqlQuery += "AND ([Client].[Name] LIKE N'%@Value%'  " +
                        "OR [Client].[FullName] LIKE N'%@Value%' " +
                        "OR [Client].[FirstName] LIKE N'%@Value%' " +
                        "OR [Client].[LastName] LIKE N'%@Value%' " +
                        "OR [Client].[MiddleName] LIKE N'%@Value%') ";

        return _connection.Query<Client>(sqlQuery, new { From = from, To = to, Value = value }).ToList();
    }

    public void UpdateNumbers(Client client) {
        _connection.Execute(
            "UPDATE Client " +
            "SET MobileNumber = @MobileNumber, ClientNumber = @ClientNumber " +
            "WHERE NetUID = @NetUid",
            client
        );
    }

    public void UpdateOrderExpireDays(Guid clientNetId, long expireDays) {
        _connection.Execute(
            "UPDATE [Client] " +
            "SET [OrderExpireDays] = @ExpireDays " +
            "WHERE [Client].NetUID = @ClientNetId ",
            new { ClientNetId = clientNetId, ExpireDays = expireDays });
    }

    public void UpdateOrderExpireDaysByType(Guid typeNetId, long expireDays) {
        _connection.Execute(
            "UPDATE [Client] " +
            "SET [Client].OrderExpireDays = @ExpireDays " +
            "FROM [Client] " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].[ClientID] = [Client].ID " +
            "LEFT JOIN [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].[ClientTypeRoleID] " +
            "WHERE [ClientTypeRole].NetUID = @ClientTypeNetId " +
            "AND [Client].OrderExpireDays <> [ClientTypeRole].OrderExpireDays",
            new { ClientTypeNetId = typeNetId, ExpireDays = expireDays });
    }

    public Client GetEmail(string email) {
        return _connection.Query<Client>(
            "SELECT * FROM Client " +
            "WHERE EmailAddress = @Email " +
            "AND [Client].Deleted = 0 ",
            new { Email = email }).FirstOrDefault();
    }

    public Client GetEmailDeleted(string email) {
        return _connection.Query<Client>(
            "SELECT * FROM Client " +
            "WHERE EmailAddress = @Email ",
            new { Email = email }).FirstOrDefault();
    }

    public Client GetOriginalRegionCode(string regionCode) {
        return _connection.Query<Client>(
            "SELECT * FROM Client " +
            "LEFT JOIN RegionCode " +
            "ON RegionCode.ID = Client.RegionCodeID " +
            "WHERE OriginalRegionCode = @RegionCode ",
            new { RegionCode = regionCode }).FirstOrDefault();
    }

    private class DocumentValuesResult {
        public long Id { get; set; }
        public decimal GrossPrice { get; set; }
    }
}