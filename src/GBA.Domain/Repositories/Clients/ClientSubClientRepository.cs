using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientSubClientRepository : IClientSubClientRepository {
    private readonly IDbConnection _connection;

    public ClientSubClientRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ClientSubClient clientSubClient) {
        _connection.Execute(
            "INSERT INTO ClientSubClient (RootClientId, SubClientId, Updated) " +
            "VALUES (@RootClientId, @SubClientId, getutcdate())",
            clientSubClient
        );
    }

    public List<ClientSubClient> GetAllClientSubClients(Guid clientNetId) {
        List<ClientSubClient> clientSubClients = new();

        string sqlExpression = "SELECT * FROM Client " +
                               "LEFT OUTER JOIN ClientSubClient " +
                               "ON ClientSubClient.RootClientID = Client.ID AND ClientSubClient.Deleted = 0 " +
                               "LEFT OUTER JOIN Client AS SubClient " +
                               "ON SubClient.ID = ClientSubClient.SubClientID " +
                               "LEFT OUTER JOIN ClientAgreement " +
                               "ON ClientAgreement.ClientID = SubClient.ID AND ClientAgreement.Deleted = 0 " +
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
                               "ON SubClient.RegionCodeID = RegionCode.ID " +
                               "LEFT OUTER JOIN Pricing " +
                               "ON Pricing.ID = Agreement.PricingID " +
                               "LEFT OUTER JOIN ClientInDebt " +
                               "ON ClientInDebt.AgreementID = Agreement.ID AND ClientInDebt.Deleted = 0 " +
                               "LEFT OUTER JOIN Debt " +
                               "ON ClientInDebt.DebtID = Debt.ID AND Debt.Deleted = 0 " +
                               "WHERE Client.NetUID = @ClientNetId";

        Type[] types = {
            typeof(Client),
            typeof(ClientSubClient),
            typeof(Client),
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
            ClientSubClient clientSubClient = (ClientSubClient)objects[1];
            Client subClient = (Client)objects[2];
            ClientAgreement subClientClientAgreement = (ClientAgreement)objects[3];
            Agreement subClientAgreement = (Agreement)objects[4];
            Currency subClientAgreementCurrency = (Currency)objects[5];
            Organization subClientAgreementOrganization = (Organization)objects[6];
            OrganizationTranslation subClientAgreementOrganizationTranslation = (OrganizationTranslation)objects[7];
            RegionCode subClientRegionCode = (RegionCode)objects[8];
            Pricing subClientAgreementPricing = (Pricing)objects[9];
            ClientInDebt subClientClientInDebt = (ClientInDebt)objects[10];
            Debt subClientDebt = (Debt)objects[11];

            if (clientSubClient != null) {
                if (subClientRegionCode != null) subClient.RegionCode = subClientRegionCode;

                if (subClientClientAgreement != null && subClientAgreement != null) {
                    if (subClientAgreementPricing != null) subClientAgreement.Pricing = subClientAgreementPricing;

                    if (subClientAgreementOrganization != null) {
                        if (subClientAgreementOrganizationTranslation != null) subClientAgreementOrganization.Name = subClientAgreementOrganizationTranslation.Name;
                        subClientAgreement.Organization = subClientAgreementOrganization;
                    }

                    if (subClientAgreementCurrency != null) subClientAgreement.Currency = subClientAgreementCurrency;

                    if (subClientClientInDebt != null) {
                        if (subClientDebt != null) subClientClientInDebt.Debt = subClientDebt;

                        subClientAgreement.ClientInDebts.Add(subClientClientInDebt);
                    }

                    subClientClientAgreement.Agreement = subClientAgreement;
                    subClient.ClientAgreements.Add(subClientClientAgreement);

                    clientSubClient.SubClient = subClient;

                    if (clientSubClients.Any(c => c.Id.Equals(clientSubClient.Id))) {
                        ClientSubClient clientSubClientFromList = clientSubClients.First(c => c.Id.Equals(clientSubClient.Id));

                        if (!clientSubClientFromList.SubClient.ClientAgreements.Any(a => a.Id.Equals(subClientClientAgreement.Id)))
                            clientSubClientFromList.SubClient.ClientAgreements.Add(subClientClientAgreement);

                        if (clientSubClientFromList.SubClient.ClientAgreements.Any(a => a.Id.Equals(subClientClientAgreement.Id)))
                            if (subClientClientInDebt != null)
                                clientSubClientFromList.SubClient.ClientAgreements.First(a => a.Id.Equals(subClientClientAgreement.Id)).Agreement.ClientInDebts
                                    .Add(subClientClientInDebt);
                    } else {
                        clientSubClients.Add(clientSubClient);
                    }
                } else {
                    if (!clientSubClients.Any(c => c.Id.Equals(client.Id)))
                        clientSubClients.Add(clientSubClient);
                }
            }

            return client;
        };

        var props = new { ClientNetId = clientNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, props);

        return clientSubClients;
    }

    public IEnumerable<ClientSubClient> GetAllByRootClientId(long clientId) {
        return _connection.Query<ClientSubClient>(
            "SELECT * " +
            "FROM [ClientSubClient] " +
            "WHERE [ClientSubClient].Deleted = 0 " +
            "AND [ClientSubClient].RootClientID = @ClientId",
            new { ClientId = clientId }
        );
    }

    public ClientSubClient GetRootBySubClientNetId(Guid subClientNetId) {
        ClientSubClient clientSubClientToReturn = null;

        string sqlExpression = "SELECT * FROM ClientSubClient " +
                               "LEFT OUTER JOIN Client AS RootClient " +
                               "ON RootClient.ID = ClientSubClient.RootClientID " +
                               "LEFT OUTER JOIN RegionCode AS RootRegionCode " +
                               "ON RootRegionCode.ID = RootClient.RegionCodeId " +
                               "LEFT OUTER JOIN ClientSubClient AS [RootClient.ClientSubClient] " +
                               "ON [RootClient.ClientSubClient].RootClientID = RootClient.ID AND [RootClient.ClientSubClient].Deleted = 0 " +
                               "LEFT OUTER JOIN Client AS [RootClient.SubClient] " +
                               "ON [RootClient.SubClient].ID = [RootClient.ClientSubClient].SubClientID " +
                               "LEFT OUTER JOIN RegionCode AS [RootClient.SubClient.RegionCode] " +
                               "ON [RootClient.SubClient.RegionCode].ID = [RootClient.SubClient].RegionCodeId " +
                               "WHERE ClientSubClient.ID = (SELECT ClientSubClient.ID " +
                               "FROM ClientSubClient " +
                               "LEFT OUTER JOIN Client " +
                               "ON Client.ID = ClientSubClient.SubClientID " +
                               "WHERE Client.NetUID = @SubClientNetId " +
                               ")";

        Type[] types = {
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientSubClient),
            typeof(Client),
            typeof(RegionCode)
        };

        Func<object[], ClientSubClient> mapper = objects => {
            ClientSubClient clientSubClient = (ClientSubClient)objects[0];
            Client client = (Client)objects[1];
            RegionCode clientRegionCode = (RegionCode)objects[2];

            ClientSubClient rootClientSubClient = (ClientSubClient)objects[3];
            Client rootSubClient = (Client)objects[4];
            RegionCode rootSubClientRegionCode = (RegionCode)objects[5];

            if (clientSubClient != null && client != null) {
                if (rootClientSubClient != null && rootSubClient != null) {
                    if (rootSubClientRegionCode != null) rootSubClient.RegionCode = rootSubClientRegionCode;

                    rootClientSubClient.SubClient = rootSubClient;

                    client.SubClients.Add(rootClientSubClient);
                }

                if (clientRegionCode != null) client.RegionCode = clientRegionCode;
                clientSubClient.RootClient = client;

                if (clientSubClientToReturn != null) {
                    if (!clientSubClientToReturn.RootClient.SubClients.Any(c => c.Id.Equals(rootClientSubClient.Id)))
                        clientSubClientToReturn.RootClient.SubClients.Add(rootClientSubClient);
                } else {
                    clientSubClientToReturn = clientSubClient;
                }
            }

            return clientSubClient;
        };

        var props = new { SubClientNetId = subClientNetId };

        _connection.Query(sqlExpression, types, mapper, props);

        return clientSubClientToReturn;
    }

    public ClientSubClient GetByClientIdIfExists(long clientId) {
        return _connection.Query<ClientSubClient, Client, Client, ClientSubClient>(
                "SELECT TOP(1) * " +
                "FROM [ClientSubClient] " +
                "LEFT JOIN [Client] AS [RootClient] " +
                "ON [ClientSubClient].RootClientID = [RootClient].ID " +
                "LEFT JOIN [Client] AS [SubClient] " +
                "ON [ClientSubClient].SubClientID = [SubClient].ID " +
                "WHERE [ClientSubClient].Deleted = 0 " +
                "AND (" +
                "[ClientSubClient].RootClientID = @ClientId " +
                "OR " +
                "[ClientSubClient].SubClientID = @ClientId" +
                ")",
                (clientSubClient, rootClient, subClient) => {
                    clientSubClient.RootClient = rootClient;
                    clientSubClient.SubClient = subClient;

                    return clientSubClient;
                },
                new { ClientId = clientId }
            )
            .SingleOrDefault();
    }
}