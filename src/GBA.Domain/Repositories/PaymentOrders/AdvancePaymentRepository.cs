using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class AdvancePaymentRepository : IAdvancePaymentRepository {
    private readonly IDbConnection _connection;

    public AdvancePaymentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(AdvancePayment advancePayment) {
        return _connection.Query<long>(
                "INSERT INTO [AdvancePayment] " +
                "(Amount, VatAmount, VatPercent, Comment, UserId, TaxFreeId, ClientAgreementId, OrganizationClientAgreementId, OrganizationId, " +
                "FromDate, Number, SadId, Updated) " +
                "VALUES " +
                "(@Amount, @VatAmount, @VatPercent, @Comment, @UserId, @TaxFreeId, @ClientAgreementId, @OrganizationClientAgreementId, @OrganizationId, " +
                "@FromDate, @Number, @SadId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                advancePayment
            )
            .Single();
    }

    public void Update(AdvancePayment advancePayment) {
        _connection.Execute(
            "UPDATE [AdvancePayment] " +
            "SET Amount = @Amount, VatAmount = @VatAmount, VatPercent = @VatPercent, Comment = @Comment, UserId = @UserId, FromDate = @FromDate, " +
            "ClientAgreementId = @ClientAgreementId, OrganizationClientAgreementId = @OrganizationClientAgreementId, OrganizationId = @OrganizationId, " +
            "Number = @Number, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            advancePayment
        );
    }

    public AdvancePayment GetLastRecord() {
        return _connection.Query<AdvancePayment>(
            "SELECT TOP(1) * " +
            "FROM [AdvancePayment] " +
            "WHERE Deleted = 0 " +
            "ORDER BY FromDate DESC"
        ).SingleOrDefault();
    }

    public AdvancePayment GetById(long id) {
        AdvancePayment toReturn = null;

        Type[] types = {
            typeof(AdvancePayment),
            typeof(User),
            typeof(TaxFree),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(RegionCode),
            typeof(Agreement),
            typeof(Currency),
            typeof(OrganizationClientAgreement),
            typeof(OrganizationClient),
            typeof(Currency),
            typeof(Organization),
            typeof(Sad)
        };

        Func<object[], AdvancePayment> mapper = objects => {
            AdvancePayment advancePayment = (AdvancePayment)objects[0];
            User user = (User)objects[1];
            TaxFree taxFree = (TaxFree)objects[2];
            ClientAgreement clientAgreement = (ClientAgreement)objects[3];
            Client client = (Client)objects[4];
            RegionCode regionCode = (RegionCode)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency clientAgreementCurrency = (Currency)objects[7];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[8];
            OrganizationClient organizationClient = (OrganizationClient)objects[9];
            Currency organizationClientAgreementCurrency = (Currency)objects[10];
            Organization organization = (Organization)objects[11];
            Sad sad = (Sad)objects[12];

            if (clientAgreement != null) {
                client.RegionCode = regionCode;

                agreement.Currency = clientAgreementCurrency;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;
            }

            if (organizationClientAgreement != null) {
                organizationClientAgreement.OrganizationClient = organizationClient;

                organizationClientAgreement.Currency = organizationClientAgreementCurrency;
            }

            advancePayment.User = user;
            advancePayment.TaxFree = taxFree;
            advancePayment.Sad = sad;
            advancePayment.Organization = organization;
            advancePayment.ClientAgreement = clientAgreement;
            advancePayment.OrganizationClientAgreement = organizationClientAgreement;

            toReturn = advancePayment;

            return advancePayment;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AdvancePayment] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [AdvancePayment].UserID " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [AdvancePayment].TaxFreeID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [AdvancePayment].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [AdvancePayment].OrganizationClientAgreementID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [OrganizationClientAgreement].OrganizationClientID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [AdvancePayment].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [AdvancePayment].SadID " +
            "WHERE [AdvancePayment].ID = @Id",
            types,
            mapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public AdvancePayment GetByNetId(Guid netId) {
        AdvancePayment toReturn = null;

        Type[] types = {
            typeof(AdvancePayment),
            typeof(User),
            typeof(TaxFree),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(RegionCode),
            typeof(Agreement),
            typeof(Currency),
            typeof(OrganizationClientAgreement),
            typeof(OrganizationClient),
            typeof(Currency),
            typeof(Organization),
            typeof(Sad)
        };

        Func<object[], AdvancePayment> mapper = objects => {
            AdvancePayment advancePayment = (AdvancePayment)objects[0];
            User user = (User)objects[1];
            TaxFree taxFree = (TaxFree)objects[2];
            ClientAgreement clientAgreement = (ClientAgreement)objects[3];
            Client client = (Client)objects[4];
            RegionCode regionCode = (RegionCode)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency clientAgreementCurrency = (Currency)objects[7];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[8];
            OrganizationClient organizationClient = (OrganizationClient)objects[9];
            Currency organizationClientAgreementCurrency = (Currency)objects[10];
            Organization organization = (Organization)objects[11];
            Sad sad = (Sad)objects[12];

            if (clientAgreement != null) {
                client.RegionCode = regionCode;

                agreement.Currency = clientAgreementCurrency;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;
            }

            if (organizationClientAgreement != null) {
                organizationClientAgreement.OrganizationClient = organizationClient;

                organizationClientAgreement.Currency = organizationClientAgreementCurrency;
            }

            advancePayment.User = user;
            advancePayment.TaxFree = taxFree;
            advancePayment.Sad = sad;
            advancePayment.Organization = organization;
            advancePayment.ClientAgreement = clientAgreement;
            advancePayment.OrganizationClientAgreement = organizationClientAgreement;

            toReturn = advancePayment;

            return advancePayment;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [AdvancePayment] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [AdvancePayment].UserID " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [AdvancePayment].TaxFreeID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [AdvancePayment].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [AdvancePayment].OrganizationClientAgreementID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [OrganizationClientAgreement].OrganizationClientID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [AdvancePayment].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [AdvancePayment].SadID " +
            "WHERE [AdvancePayment].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public IEnumerable<AdvancePayment> GetAllFiltered(
        DateTime fromDate,
        DateTime toDate) {
        Type[] types = {
            typeof(AdvancePayment),
            typeof(User),
            typeof(TaxFree),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(RegionCode),
            typeof(Agreement),
            typeof(Currency),
            typeof(OrganizationClientAgreement),
            typeof(OrganizationClient),
            typeof(Currency),
            typeof(Organization),
            typeof(Sad)
        };

        Func<object[], AdvancePayment> mapper = objects => {
            AdvancePayment advancePayment = (AdvancePayment)objects[0];
            User user = (User)objects[1];
            TaxFree taxFree = (TaxFree)objects[2];
            ClientAgreement clientAgreement = (ClientAgreement)objects[3];
            Client client = (Client)objects[4];
            RegionCode regionCode = (RegionCode)objects[5];
            Agreement agreement = (Agreement)objects[6];
            Currency clientAgreementCurrency = (Currency)objects[7];
            OrganizationClientAgreement organizationClientAgreement = (OrganizationClientAgreement)objects[8];
            OrganizationClient organizationClient = (OrganizationClient)objects[9];
            Currency organizationClientAgreementCurrency = (Currency)objects[10];
            Organization organization = (Organization)objects[11];
            Sad sad = (Sad)objects[12];

            if (clientAgreement != null) {
                client.RegionCode = regionCode;

                agreement.Currency = clientAgreementCurrency;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;
            }

            if (organizationClientAgreement != null) {
                organizationClientAgreement.OrganizationClient = organizationClient;

                organizationClientAgreement.Currency = organizationClientAgreementCurrency;
            }

            advancePayment.User = user;
            advancePayment.TaxFree = taxFree;
            advancePayment.Sad = sad;
            advancePayment.Organization = organization;
            advancePayment.ClientAgreement = clientAgreement;
            advancePayment.OrganizationClientAgreement = organizationClientAgreement;

            return advancePayment;
        };

        return _connection.Query(
            "SELECT * " +
            "FROM [AdvancePayment] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [AdvancePayment].UserID " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].ID = [AdvancePayment].TaxFreeID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [AdvancePayment].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [views].[CurrencyView] AS [AgreementCurrency] " +
            "ON [AgreementCurrency].ID = [Agreement].CurrencyID " +
            "AND [AgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [OrganizationClientAgreement] " +
            "ON [OrganizationClientAgreement].ID = [AdvancePayment].OrganizationClientAgreementID " +
            "LEFT JOIN [OrganizationClient] " +
            "ON [OrganizationClient].ID = [OrganizationClientAgreement].OrganizationClientID " +
            "LEFT JOIN [views].[CurrencyView] AS [OrganizationClientAgreementCurrency] " +
            "ON [OrganizationClientAgreementCurrency].ID = [OrganizationClientAgreement].CurrencyID " +
            "AND [OrganizationClientAgreementCurrency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [AdvancePayment].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].ID = [AdvancePayment].SadID " +
            "WHERE [AdvancePayment].Deleted = 0 " +
            "AND [AdvancePayment].FromDate >= @From " +
            "AND [AdvancePayment].FromDate <= @To",
            types,
            mapper,
            new { From = fromDate, To = toDate, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }
}