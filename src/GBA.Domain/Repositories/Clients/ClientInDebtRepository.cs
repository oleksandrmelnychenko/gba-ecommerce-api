using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.PaymentStatuses;
using GBA.Domain.EntityHelpers.DebtorModels;
using GBA.Domain.EntityHelpers.SalesModels.Models;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientInDebtRepository : IClientInDebtRepository {
    private readonly IDbConnection _connection;

    public ClientInDebtRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ClientInDebt clientInDebt) {
        _connection.Execute(
            "INSERT INTO ClientInDebt (ClientID, AgreementID, DebtID, SaleId, Updated, ReSaleID) " +
            "VALUES(@ClientId, @AgreementId, @DebtId, @SaleId, getutcdate(), @ReSaleID)",
            clientInDebt);
    }

    public void Update(ClientInDebt clientInDebt) {
        _connection.Execute(
            "UPDATE ClientInDebt SET " +
            "ClientID = @ClientId, AgreementID = @AgreementId, DebtId = @DebtId, SaleId = @SaleId, Updated = getutcdate(), ReSaleID = @ReSaleId " +
            "WHERE NetUID = @NetUid",
            clientInDebt
        );
    }

    public List<ClientInDebt> GetAllByClientIdGrouped(Guid netId) {
        List<ClientInDebt> clientInDebts = new();
        List<long> debtIds = new();

        _connection.Query<ClientInDebt, Debt, Client, Agreement, Currency, CurrencyTranslation, ClientInDebt>(
            "SELECT * " +
            "FROM ClientInDebt " +
            "LEFT JOIN Debt " +
            "ON ClientInDebt.DebtID = Debt.ID " +
            "LEFT JOIN Client " +
            "ON ClientInDebt.ClientID = Client.ID " +
            "LEFT JOIN Agreement " +
            "ON ClientInDebt.AgreementID = Agreement.ID " +
            "LEFT JOIN Currency " +
            "ON Agreement.CurrencyID = Currency.ID " +
            "LEFT JOIN CurrencyTranslation " +
            "ON CurrencyTranslation.CurrencyID = Currency.ID " +
            "AND CurrencyTranslation.CultureCode = @Culture " +
            "WHERE Client.NetUID = @NetId " +
            "AND Debt.Deleted = 0 " +
            "AND ClientInDebt.Deleted = 0 " +
            "AND Debt.Total != 0",
            (clientInDebt, debt, client, agreement, currency, currencyTranslation) => {
                if (!clientInDebts.Any(d => d.Agreement.Currency.Id.Equals(currency.Id))) {
                    currency.Name = currencyTranslation?.Name;

                    agreement.Currency = currency;

                    debt.Days = Convert.ToInt32((DateTime.UtcNow - debt.Created).TotalDays);
                    debt.Total = Math.Round(debt.Total, 2);

                    clientInDebt.Debt = debt;
                    clientInDebt.Agreement = agreement;
                    clientInDebt.Client = client;

                    clientInDebts.Add(clientInDebt);
                    debtIds.Add(clientInDebt.Id);
                } else {
                    if (!debtIds.Any(d => d.Equals(clientInDebt.Id))) {
                        ClientInDebt clientInDebtFromList = clientInDebts.First(d => d.Agreement.Currency.Id.Equals(currency.Id));

                        clientInDebtFromList.Debt.Days = clientInDebtFromList.Debt.Days < clientInDebtFromList.Agreement.NumberDaysDebt
                            ? 0
                            : clientInDebtFromList.Debt.Days - clientInDebtFromList.Agreement.NumberDaysDebt;
                        clientInDebtFromList.Debt.Total = Math.Round(clientInDebtFromList.Debt.Total + debt.Total, 2);

                        debtIds.Add(clientInDebt.Id);
                    }
                }

                return clientInDebt;
            },
            new { NetId = netId.ToString(), Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return clientInDebts;
    }

    public List<ClientInDebt> GetAllByClientId(long id) {
        return _connection.Query<ClientInDebt, Debt, Sale, ClientInDebt>(
            "SELECT * " +
            "FROM ClientInDebt " +
            "LEFT JOIN Debt " +
            "ON ClientInDebt.DebtID = Debt.ID " +
            "LEFT JOIN Sale " +
            "ON ClientInDebt.SaleID = Sale.ID " +
            "WHERE ClientInDebt.ClientID = @Id " +
            "AND Debt.Deleted = 0 " +
            "AND ClientInDebt.Deleted = 0",
            (clientInDebt, debt, sale) => {
                clientInDebt.Debt = debt;
                clientInDebt.Sale = sale;

                return clientInDebt;
            },
            new { Id = id }
        ).ToList();
    }

    public List<Debt> GetDebtByClientAgreementNetId(Guid netId) {
        return _connection.Query<Debt>(
            "SELECT Debt.* FROM ClientAgreement " +
            "LEFT JOIN Agreement " +
            "ON Agreement.ID = ClientAgreement.AgreementID " +
            "LEFT JOIN ClientInDebt " +
            "ON ClientInDebt.AgreementID = Agreement.ID " +
            "AND ClientInDebt.Deleted = 0 " +
            "LEFT JOIN Debt " +
            "ON Debt.ID = ClientInDebt.DebtID " +
            "AND Debt.Deleted = 0 " +
            "WHERE ClientAgreement.NetUID = @NetId " +
            "AND ClientAgreement.Deleted = 0 " +
            "ORDER BY Debt.Created ASC ",
            new { NetId = netId }
        ).ToList();
    }


    public ClientInDebt GetBySaleAndClientAgreementIds(long saleId, long clientAgreementId) {
        return _connection.Query<ClientInDebt, Debt, Sale, ClientInDebt>(
                "SELECT ClientInDebt.* " +
                ",Debt.* " +
                ",Sale.* " +
                "FROM ClientInDebt " +
                "LEFT JOIN Debt " +
                "ON ClientInDebt.DebtID = Debt.ID " +
                "LEFT JOIN Agreement " +
                "ON ClientInDebt.AgreementID = Agreement.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID " +
                "LEFT JOIN Sale " +
                "ON Sale.ID = ClientInDebt.SaleID " +
                "WHERE (ClientInDebt.SaleID = @SaleId " +
                "OR ClientInDebt.[ReSaleID] = @SaleId) " +
                "AND ClientAgreement.ID = @ClientAgreementId " +
                "AND ClientInDebt.Deleted = 0 " +
                "AND Debt.Deleted = 0 ",
                (clientInDebt, debt, sale) => {
                    if (clientInDebt != null) {
                        clientInDebt.Debt = debt;
                        clientInDebt.Sale = sale;
                    }

                    return clientInDebt;
                },
                new { SaleId = saleId, ClientAgreementId = clientAgreementId }
            )
            .SingleOrDefault();
    }

    public ClientInDebt GetBySaleAndClientAgreementIdsWithDeleted(long saleId, long clientAgreementId) {
        return _connection.Query<ClientInDebt, Debt, ClientInDebt>(
                "SELECT ClientInDebt.* " +
                ",Debt.* " +
                "FROM ClientInDebt " +
                "LEFT JOIN Debt " +
                "ON ClientInDebt.DebtID = Debt.ID " +
                "LEFT JOIN Agreement " +
                "ON ClientInDebt.AgreementID = Agreement.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID " +
                "WHERE ClientInDebt.SaleID = @SaleId " +
                "AND ClientAgreement.ID = @ClientAgreementId",
                (clientIdDebt, debt) => {
                    if (clientIdDebt != null) clientIdDebt.Debt = debt;

                    return clientIdDebt;
                },
                new { SaleId = saleId, ClientAgreementId = clientAgreementId }
            )
            .SingleOrDefault();
    }

    public ClientInDebt GetActiveByClientAgreementId(long clientAgreementId) {
        return _connection.Query<ClientInDebt, Debt, Sale, ClientInDebt>(
                "SELECT TOP(1) ClientInDebt.* " +
                ",Debt.* " +
                ", [Sale].* " +
                "FROM ClientInDebt " +
                "LEFT JOIN Debt " +
                "ON ClientInDebt.DebtID = Debt.ID " +
                "LEFT JOIN Agreement " +
                "ON ClientInDebt.AgreementID = Agreement.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].ID = [ClientInDebt].SaleID " +
                "WHERE ClientAgreement.ID = @ClientAgreementId " +
                "AND ClientInDebt.Deleted = 0 " +
                "AND Debt.Deleted = 0 ",
                (clientIdDebt, debt, sale) => {
                    if (clientIdDebt != null) {
                        clientIdDebt.Debt = debt;
                        clientIdDebt.Sale = sale;
                    }

                    return clientIdDebt;
                },
                new { ClientAgreementId = clientAgreementId }
            )
            .SingleOrDefault();
    }

    public List<ClientInDebt> GetAllActiveByClientAgreementId(long clientAgreementId) {
        return _connection.Query<ClientInDebt, Debt, Sale, ClientInDebt>(
            "SELECT TOP(1) ClientInDebt.* " +
            ",Debt.* " +
            ", [Sale].* " +
            "FROM ClientInDebt " +
            "LEFT JOIN Debt " +
            "ON ClientInDebt.DebtID = Debt.ID " +
            "LEFT JOIN Agreement " +
            "ON ClientInDebt.AgreementID = Agreement.ID " +
            "LEFT JOIN ClientAgreement " +
            "ON ClientAgreement.AgreementID = Agreement.ID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].ID = [ClientInDebt].SaleID " +
            "WHERE ClientAgreement.ID = @ClientAgreementId " +
            "AND ClientInDebt.Deleted = 0 " +
            "AND Debt.Deleted = 0 ",
            (clientIdDebt, debt, sale) => {
                if (clientIdDebt != null) {
                    clientIdDebt.Debt = debt;
                    clientIdDebt.Sale = sale;
                }

                return clientIdDebt;
            },
            new { ClientAgreementId = clientAgreementId }
        ).ToList();
    }

    public ClientInDebt GetExpiredDebtByClientAgreementId(long clientAgreementId) {
        return _connection.Query<ClientInDebt, Debt, ClientInDebt>(
                "SELECT TOP 1 " +
                "ClientInDebt.* " +
                ", Debt.* " +
                "FROM ClientInDebt " +
                "LEFT JOIN Debt " +
                "ON ClientInDebt.DebtID = Debt.ID " +
                "LEFT JOIN Agreement " +
                "ON ClientInDebt.AgreementID = Agreement.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID " +
                "WHERE ClientAgreement.ID = @ClientAgreementId " +
                "AND ClientInDebt.Deleted = 0 " +
                "AND Debt.Deleted = 0 " +
                "AND Debt.Total <> 0 " +
                "AND Agreement.NumberDaysDebt < DATEDIFF(day, Debt.Created, GETUTCDATE()) ",
                (clientIdDebt, debt) => {
                    if (clientIdDebt != null) clientIdDebt.Debt = debt;

                    return clientIdDebt;
                },
                new { ClientAgreementId = clientAgreementId }
            )
            .SingleOrDefault();
    }

    public ClientInDebt GetBySaleAndAgreementIdWithDeleted(long saleId, long agreementId) {
        return _connection.Query<ClientInDebt, Debt, ClientInDebt>(
                "SELECT * " +
                "FROM [ClientInDebt] " +
                "LEFT JOIN [Debt] " +
                "ON [Debt].ID = [ClientInDebt].DebtID " +
                "WHERE [ClientInDebt].SaleID = @SaleId " +
                "AND [ClientInDebt].AgreementID = @AgreementId",
                (clientInDebt, debt) => {
                    clientInDebt.Debt = debt;

                    return clientInDebt;
                },
                new { SaleId = saleId, AgreementId = agreementId }
            )
            .SingleOrDefault();
    }

    public void Remove(Guid netid) {
        _connection.Execute(
            "UPDATE ClientInDebt SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netid.ToString() }
        );
    }

    public void Restore(Guid netId) {
        _connection.Execute(
            "UPDATE ClientInDebt SET " +
            "Deleted = 0 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void Restore(long id) {
        _connection.Execute(
            "UPDATE ClientInDebt SET " +
            "Deleted = 0 " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ClientInDebt] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ClientInDebt].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RestoreAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ClientInDebt] " +
            "SET Deleted = 0, Updated = getutcdate() " +
            "WHERE [ClientInDebt].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public List<ClientInDebt> GetAllBySaleIds(IEnumerable<long> saleIds) {
        return _connection.Query<ClientInDebt, Sale, ReSale, SaleNumber, Debt, Agreement, Currency, ClientInDebt>(
                "SELECT * " +
                "FROM [ClientInDebt] " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].ID = [ClientInDebt].SaleID " +
                "LEFT JOIN [ReSale] " +
                "ON [ReSale].[ID] = [ClientInDebt].[ReSaleID] " +
                "LEFT JOIN [SaleNumber] " +
                "ON CASE WHEN [Sale].[ID] IS NOT NULL THEN [Sale].[SaleNumberID] ELSE [ReSale].[SaleNumberID] END = [SaleNumber].[ID] " +
                "LEFT JOIN [Debt] " +
                "ON [Debt].ID = [ClientInDebt].DebtID " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientInDebt].AgreementID " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].ID = [Agreement].CurrencyID " +
                "WHERE ([ClientInDebt].SaleID IN @SaleIds " +
                "OR [ClientInDebt].[ReSaleID] IN @SaleIds) " +
                "AND [ClientInDebt].Deleted = 0 " +
                "AND [Debt].Deleted = 0 " +
                "ORDER BY [SaleNumber].Value  DESC",
                (clientInDebt, sale, reSale, saleNumber, debt, agreement, currency) => {
                    if (sale != null)
                        sale.SaleNumber = saleNumber;
                    else if (reSale != null) reSale.SaleNumber = saleNumber;

                    agreement.Currency = currency;

                    clientInDebt.Sale = sale;
                    clientInDebt.ReSale = reSale;
                    clientInDebt.Debt = debt;
                    clientInDebt.Agreement = agreement;

                    return clientInDebt;
                },
                new { SaleIds = saleIds }
            )
            .ToList();
    }

    public List<ClientInDebt> GetAllBySaleIdsWithDeleted(IEnumerable<long> saleIds) {
        return _connection.Query<ClientInDebt, Sale, SaleNumber, Debt, BaseSalePaymentStatus, ClientInDebt>(
                "SELECT * " +
                "FROM [ClientInDebt] " +
                "LEFT JOIN [Sale] " +
                "ON [Sale].ID = [ClientInDebt].SaleID " +
                "LEFT JOIN [SaleNumber] " +
                "ON [SaleNumber].ID = [Sale].SaleNumberID " +
                "LEFT JOIN [Debt] " +
                "ON [Debt].ID = [ClientInDebt].DebtID " +
                "LEFT JOIN [BaseSalePaymentStatus] " +
                "ON [BaseSalePaymentStatus].ID = [Sale].BaseSalePaymentStatusID " +
                "WHERE [ClientInDebt].SaleID IN @SaleIds " +
                "ORDER BY [SaleNumber].Value DESC",
                (clientInDebt, sale, saleNumber, debt, paymentStatus) => {
                    sale.SaleNumber = saleNumber;
                    sale.BaseSalePaymentStatus = paymentStatus;

                    clientInDebt.Sale = sale;
                    clientInDebt.Debt = debt;

                    return clientInDebt;
                },
                new { SaleIds = saleIds }
            )
            .ToList();
    }

    public dynamic GetDebtInfo(Guid clientNetId) {
        return _connection.Query<dynamic>("WITH SubClientDebts_CTE(TotaSubClientsDebt, RootClientID) " +
                                          "AS ( " +
                                          "SELECT " +
                                          "SUM(Debt.Total) AS TotalSubClientsDebt, " +
                                          "ClientSubClient.RootClientID AS RootClientID " +
                                          "FROM ClientInDebt " +
                                          "LEFT OUTER JOIN Debt " +
                                          "ON Debt.ID = ClientInDebt.DebtID " +
                                          "LEFT OUTER JOIN ClientSubClient " +
                                          "ON ClientSubClient.RootClientID = (SELECT ID FROM Client WHERE NetUID = @ClientNetId) " +
                                          "AND ClientSubClient.Deleted = 0 " +
                                          "WHERE ClientInDebt.ClientID = ClientSubClient.SubClientID " +
                                          "AND ClientInDebt.Deleted = 0 " +
                                          "GROUP BY ClientSubClient.RootClientID " +
                                          ") " +
                                          "SELECT ROUND(SubClientDebts_CTE.TotaSubClientsDebt + SUM(Debt.Total), 2) AS TotalDebt, " +
                                          "ROUND(SubClientDebts_CTE.TotaSubClientsDebt, 2) AS TotaSubClientsDebt " +
                                          "FROM ClientInDebt " +
                                          "LEFT OUTER JOIN Debt " +
                                          "ON Debt.ID = ClientInDebt.DebtID " +
                                          "LEFT OUTER JOIN Client " +
                                          "ON ClientInDebt.ClientID = Client.ID " +
                                          "LEFT OUTER JOIN SubClientDebts_CTE " +
                                          "ON SubClientDebts_CTE.RootClientID = Client.ID " +
                                          "WHERE Client.NetUID = @ClientNetId " +
                                          "AND ClientInDebt.Deleted = 0 " +
                                          "GROUP BY SubClientDebts_CTE.TotaSubClientsDebt ",
                new { ClientNetId = clientNetId }
            )
            .SingleOrDefault();
    }

    public ClientDebtorsModel GetFilteredDebtorsByClientForPrintingDocument(
        Guid? userNetId,
        Guid? organizationNetId,
        TypeOfClientAgreement typeAgreement,
        TypeOfCurrencyOfAgreement typeCurrency) {
        ClientDebtorsModel toReturn = new();

        string generalPartOfQuery = "FROM [ClientInDebt] " +
                                    "LEFT JOIN [Agreement] " +
                                    "ON [Agreement] .[Deleted] = 0 AND " +
                                    "[Agreement].[ID] = [ClientInDebt].[AgreementID] " +
                                    "LEFT JOIN [Client] " +
                                    "ON [Client].[ID] = [ClientInDebt].[ClientID] " +
                                    "LEFT JOIN [RegionCode] " +
                                    "ON [RegionCode].[ID] = [Client].[RegionCodeID] " +
                                    "LEFT JOIN [ClientUserProfile] " +
                                    "ON [ClientUserProfile].[ClientID] = [Client].[ID] " +
                                    "LEFT JOIN [User] " +
                                    "ON [User].[ID] = [ClientUserProfile].[UserProfileID] " +
                                    "LEFT JOIN [Debt] " +
                                    "ON [Debt].[ID] = [ClientInDebt].[DebtID] " +
                                    "LEFT JOIN [Sale] " +
                                    "ON [Sale].[ID] = [ClientInDebt].[SaleID] " +
                                    "LEFT JOIN [Organization] " +
                                    "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
                                    "LEFT JOIN [Currency] " +
                                    "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                                    "WHERE [ClientInDebt].[Deleted] = 0 ";

        toReturn.TotalRemainderDebtorsValue = GetTotalRemainderDebtorsValue(generalPartOfQuery);

        toReturn.TotalOverdueDebtorsValue = GetTotalOverdueDebtorsValue();

        toReturn.TotalMissedDays = GetTotalMissedDays(generalPartOfQuery);

        if (!typeCurrency.Equals(TypeOfCurrencyOfAgreement.None))
            generalPartOfQuery += "AND [Currency].[Code] = @CurrencyCode ";

        if (organizationNetId.HasValue)
            generalPartOfQuery += "AND [Organization].[NetUID] = @OrganizationNetId ";

        if (userNetId.HasValue)
            generalPartOfQuery += "AND [User].[NetUID] = @UserNetId ";

        if (typeAgreement == TypeOfClientAgreement.VAT) generalPartOfQuery += "AND [Agreement].[WithVATAccounting] = 1 ";
        else if (typeAgreement == TypeOfClientAgreement.WithoutVAT) generalPartOfQuery += "AND [Agreement].[WithVATAccounting] = 0 ";

        string sqlQuery = "SELECT * " +
                          ", dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, GETDATE()) AS [EuroTotal] " +
                          generalPartOfQuery;

        Type[] types = {
            typeof(ClientInDebt),
            typeof(Agreement),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(Debt),
            typeof(Sale),
            typeof(Organization),
            typeof(Currency),
            typeof(decimal)
        };

        Func<object[], ClientInDebt> mapper = objects => {
            ClientInDebt clientInDebt = (ClientInDebt)objects[0];
            Agreement agreement = (Agreement)objects[1];
            Client client = (Client)objects[2];
            RegionCode regionCode = (RegionCode)objects[3];
            User user = (User)objects[5];
            Debt debt = (Debt)objects[6];
            decimal totalEuro = (decimal)objects[10];

            ClientInDebtModel clientDebt;

            int missedDays = 0;

            if (debt != null && agreement != null) {
                int today = DateTime.Today.DayOfYear + (DateTime.Today.Year - debt.Created.Year) * 365;

                int overdueDays = today - (debt.Created.DayOfYear + agreement.NumberDaysDebt);

                missedDays -= overdueDays;
            }

            if (toReturn.ClientInDebtors.Any(x => x.ClientNetId == client.NetUid)) {
                clientDebt = toReturn.ClientInDebtors.FirstOrDefault(x => x.ClientNetId == client.NetUid);
            } else {
                clientDebt = new ClientInDebtModel();
                toReturn.ClientInDebtors.Add(clientDebt);

                clientDebt.RegionCode = regionCode?.Value ?? "";

                clientDebt.UserName = user != null ? $"{user.LastName} {user.FirstName} {user.MiddleName}" : "";

                clientDebt.ClientNetId = client.NetUid;

                clientDebt.ClientName = string.IsNullOrEmpty(client.FullName) ? client.Name : client.FullName;
            }

            if (clientDebt == null) return clientInDebt;

            if (debt != null && clientDebt.CreatedDebt != debt.Created) {
                clientDebt.RemainderDebt += totalEuro;
                if (clientDebt.MissedDays <= 0 && missedDays < 0) {
                    clientDebt.OverdueDebt += totalEuro;
                    clientDebt.MissedDays += missedDays;
                } else if (clientDebt.MissedDays >= 0 && missedDays > 0) {
                    clientDebt.MissedDays += missedDays;
                }
            } else {
                clientDebt.RemainderDebt = totalEuro;
                if (clientDebt.MissedDays <= 0 && missedDays < 0) {
                    clientDebt.OverdueDebt = totalEuro;
                    clientDebt.MissedDays = missedDays;
                } else if (clientDebt.MissedDays >= 0 && missedDays > 0) {
                    clientDebt.MissedDays = missedDays;
                }
            }

            if (clientDebt.MissedDays > 0 && missedDays < 0) {
                clientDebt.MissedDays = missedDays;
                clientDebt.OverdueDebt = totalEuro;
            }

            if (debt != null) clientDebt.CreatedDebt = debt.Created;

            return clientInDebt;
        };

        _connection.Query(
            sqlQuery,
            types,
            mapper,
            new { CurrencyCode = typeCurrency.ToString(), UserNetId = userNetId, OrganizationNetId = organizationNetId },
            splitOn: "ID,EuroTotal"
        );

        return toReturn;
    }

    public ClientDebtorsModel GetFilteredDebtorsByClientInfo(
        Guid? userNetId,
        Guid? organizationNetId,
        TypeOfClientAgreement typeAgreement,
        TypeOfCurrencyOfAgreement typeCurrency,
        long limit,
        long offset) {
        ClientDebtorsModel toReturn = new();

        ClientDebtorsModelClient clientQtyTotal = GetTotalQtyClients(userNetId, organizationNetId, typeAgreement, typeCurrency, limit, offset);
        toReturn.TotalQtyClients = clientQtyTotal.TotalRowsQty;

        string generalPartOfQuery = "FROM [ClientInDebt] " +
                                    "LEFT JOIN [Agreement] " +
                                    "ON [Agreement] .[Deleted] = 0 AND " +
                                    "[Agreement].[ID] = [ClientInDebt].[AgreementID] " +
                                    "LEFT JOIN [Client] " +
                                    "ON [Client].[ID] = [ClientInDebt].[ClientID] " +
                                    "LEFT JOIN [RegionCode] " +
                                    "ON [RegionCode].[ID] = [Client].[RegionCodeID] " +
                                    "LEFT JOIN [ClientUserProfile] " +
                                    "ON [ClientUserProfile].[ClientID] = [Client].[ID] " +
                                    "LEFT JOIN [User] " +
                                    "ON [User].[ID] = [ClientUserProfile].[UserProfileID] " +
                                    "LEFT JOIN [Debt] " +
                                    "ON [Debt].[ID] = [ClientInDebt].[DebtID] " +
                                    "LEFT JOIN [Sale] " +
                                    "ON [Sale].[ID] = [ClientInDebt].[SaleID] " +
                                    "LEFT JOIN [Organization] " +
                                    "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
                                    "LEFT JOIN [Currency] " +
                                    "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                                    "WHERE [ClientInDebt].[Deleted] = 0 ";

        toReturn.TotalRemainderDebtorsValue = GetTotalRemainderDebtorsValue(generalPartOfQuery);

        toReturn.TotalOverdueDebtorsValue = GetTotalOverdueDebtorsValue();

        toReturn.TotalMissedDays = GetTotalMissedDays(generalPartOfQuery);

        if (!typeCurrency.Equals(TypeOfCurrencyOfAgreement.None))
            generalPartOfQuery += "AND [Currency].[Code] = @CurrencyCode ";

        if (organizationNetId.HasValue)
            generalPartOfQuery += "AND [Organization].[NetUID] = @OrganizationNetId ";

        if (userNetId.HasValue)
            generalPartOfQuery += "AND [User].[NetUID] = @UserNetId ";

        if (typeAgreement == TypeOfClientAgreement.VAT) generalPartOfQuery += "AND [Agreement].[WithVATAccounting] = 1 ";
        else if (typeAgreement == TypeOfClientAgreement.WithoutVAT) generalPartOfQuery += "AND [Agreement].[WithVATAccounting] = 0 ";

        string sqlQuery = ";WITH [RowNumbers_CTE] " +
                          "AS " +
                          "( " +
                          "SELECT " +
                          "DISTINCT ROW_NUMBER() OVER (ORDER BY [ClientInDebt].[ClientID] DESC) AS [RowNumber] " +
                          ",[ClientInDebt].[ClientID] " +
                          "FROM [ClientInDebt] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [ClientInDebt].[ClientID] " +
                          "WHERE [ClientInDebt].[Deleted] = 0 " +
                          "AND [Client].[Deleted] = 0 " +
                          "GROUP BY [ClientInDebt].[ClientID] " +
                          ") " +
                          "SELECT * " +
                          ", dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, GETDATE()) AS [EuroTotal] " +
                          generalPartOfQuery +
                          "AND [ClientInDebt].[ClientID] IN @Ids " +
                          "ORDER BY [Debt].[Created] DESC ";

        Type[] types = {
            typeof(ClientInDebt),
            typeof(Agreement),
            typeof(Client),
            typeof(RegionCode),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(Debt),
            typeof(Sale),
            typeof(Organization),
            typeof(Currency),
            typeof(decimal)
        };

        Func<object[], ClientInDebt> mapper = objects => {
            ClientInDebt clientInDebt = (ClientInDebt)objects[0];
            Agreement agreement = (Agreement)objects[1];
            Client client = (Client)objects[2];
            RegionCode regionCode = (RegionCode)objects[3];
            User user = (User)objects[5];
            Debt debt = (Debt)objects[6];
            decimal totalEuro = (decimal)objects[10];

            ClientInDebtModel clientDebt;

            int missedDays = 0;

            if (debt != null && agreement != null) {
                int today = DateTime.Today.DayOfYear + (DateTime.Today.Year - debt.Created.Year) * 365;

                int overdueDays = today - (debt.Created.DayOfYear + agreement.NumberDaysDebt);

                missedDays -= overdueDays;
            }

            if (toReturn.ClientInDebtors.Any(x => x.ClientNetId == client.NetUid)) {
                clientDebt = toReturn.ClientInDebtors.FirstOrDefault(x => x.ClientNetId == client.NetUid);
            } else {
                clientDebt = new ClientInDebtModel();
                toReturn.ClientInDebtors.Add(clientDebt);

                clientDebt.RegionCode = regionCode?.Value ?? "";

                clientDebt.UserName = user != null ? $"{user.LastName} {user.FirstName} {user.MiddleName}" : "";

                clientDebt.ClientNetId = client.NetUid;
                clientDebt.ClientId = client.Id.ToString();

                clientDebt.ClientName = string.IsNullOrEmpty(client.FullName) ? client.Name : client.FullName;
            }

            if (clientDebt == null) return clientInDebt;

            if (debt != null && clientDebt.CreatedDebt != debt.Created) {
                clientDebt.RemainderDebt += totalEuro;
                if (clientDebt.MissedDays <= 0 && missedDays < 0) {
                    clientDebt.OverdueDebt += totalEuro;
                    clientDebt.MissedDays += missedDays;
                } else if (clientDebt.MissedDays >= 0 && missedDays > 0) {
                    clientDebt.MissedDays += missedDays;
                }
            } else {
                clientDebt.RemainderDebt = totalEuro;
                if (clientDebt.MissedDays <= 0 && missedDays < 0) {
                    clientDebt.OverdueDebt = totalEuro;
                    clientDebt.MissedDays = missedDays;
                } else if (clientDebt.MissedDays >= 0 && missedDays > 0) {
                    clientDebt.MissedDays = missedDays;
                }
            }

            if (clientDebt.MissedDays > 0 && missedDays < 0) {
                clientDebt.MissedDays = missedDays;
                clientDebt.OverdueDebt = totalEuro;
            }

            if (debt != null) clientDebt.CreatedDebt = debt.Created;

            return clientInDebt;
        };

        _connection.Query(
            sqlQuery,
            types,
            mapper,
            new { CurrencyCode = typeCurrency.ToString(), UserNetId = userNetId, OrganizationNetId = organizationNetId, Ids = clientQtyTotal.ClientIds },
            splitOn: "ID,EuroTotal"
        );

        foreach (ClientInDebtModel clientInDebt in toReturn.ClientInDebtors) {
            clientInDebt.ClientAgreementNetId = _connection.Query<Guid>(
                "SELECT NetUID FROM ClientAgreement " +
                "WHERE ClientAgreement.ClientID = @Id ",
                new { Id = clientInDebt.ClientId }
            ).ToList();

            clientInDebt.debts = _connection.Query<Debt>(
                "SELECT Debt.* FROM ClientAgreement " +
                "LEFT JOIN Agreement " +
                "ON Agreement.ID = ClientAgreement.AgreementID " +
                "LEFT JOIN ClientInDebt " +
                "ON ClientInDebt.AgreementID = Agreement.ID " +
                "AND ClientInDebt.Deleted = 0 " +
                "LEFT JOIN Debt " +
                "ON Debt.ID = ClientInDebt.DebtID " +
                "AND Debt.Deleted = 0 " +
                "WHERE ClientAgreement.ClientID = @Id " +
                "AND ClientAgreement.Deleted = 0 " +
                "ORDER BY Debt.Created ASC ",
                new { Id = clientInDebt.ClientId }
            ).ToList();
        }

        return toReturn;
    }

    public ClientInDebt GetByReSaleAndClientAgreementIds(long reSaleId, long clientAgreementId) {
        return _connection.Query<ClientInDebt, Debt, ClientInDebt>(
                "SELECT ClientInDebt.* " +
                ",Debt.* " +
                "FROM ClientInDebt " +
                "LEFT JOIN Debt " +
                "ON ClientInDebt.DebtID = Debt.ID " +
                "LEFT JOIN Agreement " +
                "ON ClientInDebt.AgreementID = Agreement.ID " +
                "LEFT JOIN ClientAgreement " +
                "ON ClientAgreement.AgreementID = Agreement.ID " +
                "WHERE ClientInDebt.ReSaleID = @ReSaleId " +
                "AND ClientAgreement.ID = @ClientAgreementId " +
                "AND ClientInDebt.Deleted = 0 " +
                "AND Debt.Deleted = 0 ",
                (clientIdDebt, debt) => {
                    if (clientIdDebt != null)
                        clientIdDebt.Debt = debt;

                    return clientIdDebt;
                },
                new { ReSaleId = reSaleId, ClientAgreementId = clientAgreementId }
            )
            .SingleOrDefault();
    }

    private ClientDebtorsModelClient GetTotalQtyClients(Guid? userNetId,
        Guid? organizationNetId,
        TypeOfClientAgreement typeAgreement,
        TypeOfCurrencyOfAgreement typeCurrency,
        long limit,
        long offset) {
        string generalPartOfQueryCount = "FROM [ClientInDebt] " +
                                         "LEFT JOIN [Agreement] " +
                                         "ON [Agreement] .[Deleted] = 0 AND " +
                                         "[Agreement].[ID] = [ClientInDebt].[AgreementID] " +
                                         "LEFT JOIN [Client] " +
                                         "ON [Client].[ID] = [ClientInDebt].[ClientID] " +
                                         "LEFT JOIN [RegionCode] " +
                                         "ON [RegionCode].[ID] = [Client].[RegionCodeID] " +
                                         "LEFT JOIN [ClientUserProfile] " +
                                         "ON [ClientUserProfile].[ClientID] = [Client].[ID] " +
                                         "LEFT JOIN [User] " +
                                         "ON [User].[ID] = [ClientUserProfile].[UserProfileID] " +
                                         "LEFT JOIN [Debt] " +
                                         "ON [Debt].[ID] = [ClientInDebt].[DebtID] " +
                                         "LEFT JOIN [Sale] " +
                                         "ON [Sale].[ID] = [ClientInDebt].[SaleID] " +
                                         "LEFT JOIN [Organization] " +
                                         "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
                                         "LEFT JOIN [Currency] " +
                                         "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                                         "WHERE [ClientInDebt].[Deleted] = 0 ";


        if (!typeCurrency.Equals(TypeOfCurrencyOfAgreement.None))
            generalPartOfQueryCount += "AND [Currency].[Code] = @CurrencyCode ";

        if (organizationNetId.HasValue)
            generalPartOfQueryCount += "AND [Organization].[NetUID] = @OrganizationNetId ";

        if (userNetId.HasValue)
            generalPartOfQueryCount += "AND [User].[NetUID] = @UserNetId ";

        if (typeAgreement == TypeOfClientAgreement.VAT) generalPartOfQueryCount += "AND [Agreement].[WithVATAccounting] = 1 ";
        else if (typeAgreement == TypeOfClientAgreement.WithoutVAT) generalPartOfQueryCount += "AND [Agreement].[WithVATAccounting] = 0 ";

        string sqlQueryCount = ";WITH [RowNumbers_CTE] " +
                               "AS " +
                               "( " +
                               "SELECT " +
                               "DISTINCT ROW_NUMBER() OVER (ORDER BY [ClientInDebt].[ClientID] DESC) AS [RowNumber] " +
                               ",[ClientInDebt].[ClientID] " +
                               "FROM [ClientInDebt] " +
                               "LEFT JOIN [Client] " +
                               "ON [Client].[ID] = [ClientInDebt].[ClientID] " +
                               "WHERE [ClientInDebt].[Deleted] = 0 " +
                               "AND [Client].[Deleted] = 0 " +
                               "GROUP BY [ClientInDebt].[ClientID] " +
                               ") " +
                               "SELECT " +
                               "COUNT(DISTINCT[ClientInDebt].[ClientID]) AS [TotalRowsQty] " +
                               generalPartOfQueryCount +
                               "AND [ClientInDebt].[ClientID] IN ( " +
                               "SELECT [RowNumbers_CTE].[ClientID] " +
                               "FROM [RowNumbers_CTE] " +
                               ") ";


        string generalPartOfQuery = "FROM [ClientInDebt] " +
                                    "LEFT JOIN [Agreement] " +
                                    "ON [Agreement] .[Deleted] = 0 AND " +
                                    "[Agreement].[ID] = [ClientInDebt].[AgreementID] " +
                                    "LEFT JOIN [Client] " +
                                    "ON [Client].[ID] = [ClientInDebt].[ClientID] " +
                                    "LEFT JOIN [RegionCode] " +
                                    "ON [RegionCode].[ID] = [Client].[RegionCodeID] " +
                                    "LEFT JOIN [ClientUserProfile] " +
                                    "ON [ClientUserProfile].[ClientID] = [Client].[ID] " +
                                    "LEFT JOIN [User] " +
                                    "ON [User].[ID] = [ClientUserProfile].[UserProfileID] " +
                                    "LEFT JOIN [Debt] " +
                                    "ON [Debt].[ID] = [ClientInDebt].[DebtID] " +
                                    "LEFT JOIN [Sale] " +
                                    "ON [Sale].[ID] = [ClientInDebt].[SaleID] " +
                                    "LEFT JOIN [Organization] " +
                                    "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
                                    "LEFT JOIN [Currency] " +
                                    "ON [Currency].[ID] = [Agreement].[CurrencyID] " +
                                    "WHERE [ClientInDebt].[Deleted] = 0 ";
        int totalRowsQty = _connection.Query<int>(
            sqlQueryCount,
            new { Limit = limit, Offset = offset, CurrencyCode = typeCurrency.ToString(), UserNetId = userNetId, OrganizationNetId = organizationNetId }
        ).FirstOrDefault();

        if (!typeCurrency.Equals(TypeOfCurrencyOfAgreement.None))
            generalPartOfQuery += "AND [Currency].[Code] = @CurrencyCode ";

        if (organizationNetId.HasValue)
            generalPartOfQuery += "AND [Organization].[NetUID] = @OrganizationNetId ";

        //if (userNetId.HasValue)
        //    generalPartOfQuery += "AND [User].[NetUID] = @UserNetId ";

        if (typeAgreement == TypeOfClientAgreement.VAT) generalPartOfQuery += "AND [Agreement].[WithVATAccounting] = 1 ";
        else if (typeAgreement == TypeOfClientAgreement.WithoutVAT) generalPartOfQuery += "AND [Agreement].[WithVATAccounting] = 0 ";

        string sqlQuery = ";WITH [RowNumbers_CTE] " +
                          "AS " +
                          "( " +
                          "SELECT " +
                          "DISTINCT ROW_NUMBER() OVER (ORDER BY [ClientInDebt].[ClientID] DESC) AS [RowNumber] " +
                          ",[ClientInDebt].[ClientID] " +
                          "FROM [ClientInDebt] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [ClientInDebt].[ClientID] ";

        if (userNetId.HasValue)
            sqlQuery += "LEFT JOIN [ClientUserProfile] " +
                        "ON [ClientUserProfile].[ClientID] = [Client].[ID] " +
                        "LEFT JOIN [User] " +
                        "ON [User].[ID] = [ClientUserProfile].[UserProfileID] " +
                        "WHERE [ClientInDebt].[Deleted] = 0 " +
                        "AND [Client].[Deleted] = 0 " +
                        "AND [User].NetUID = @UserNetId " +
                        "AND [ClientInDebt].[Deleted] = 0 ";
        else
            sqlQuery += "WHERE [ClientInDebt].[Deleted] = 0 ";
        sqlQuery += "AND [Client].[Deleted] = 0 " +
                    "GROUP BY [ClientInDebt].[ClientID] " +
                    ") " +
                    "SELECT " +
                    "[ClientInDebt].[ClientID] as ID " +
                    generalPartOfQuery +
                    "AND [ClientInDebt].[ClientID] IN ( " +
                    "SELECT [RowNumbers_CTE].[ClientID] " +
                    "FROM [RowNumbers_CTE] " +
                    "WHERE [RowNumbers_CTE].RowNumber > @Offset " +
                    "AND [RowNumbers_CTE].RowNumber <= @Limit + @Offset " +
                    ") " +
                    "GROUP BY [ClientInDebt].[ClientID]; ";
        List<long> сlientIds = _connection.Query<long>(
            sqlQuery,
            new { Limit = limit, Offset = offset, CurrencyCode = typeCurrency.ToString(), UserNetId = userNetId, OrganizationNetId = organizationNetId }
        ).ToList();
        return new ClientDebtorsModelClient { TotalRowsQty = totalRowsQty, ClientIds = сlientIds };
    }

    private decimal GetTotalRemainderDebtorsValue(string partQuery) {
        return _connection.Query<decimal>(
            "SELECT " +
            "CASE " +
            "WHEN CONVERT(money, SUM(dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, GETDATE()))) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, GETDATE()))) " +
            "END AS [Value] " +
            partQuery).FirstOrDefault();
    }

    private decimal GetTotalOverdueDebtorsValue() {
        return _connection.Query<decimal>(
            "SELECT " +
            "CASE " +
            "WHEN CONVERT(money, SUM(dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, GETDATE()))) IS NULL " +
            "THEN 0 " +
            "ELSE CONVERT(money, SUM(dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, GETDATE()))) " +
            "END AS [Value] " +
            "FROM [ClientInDebt] " +
            "LEFT JOIN [Debt] " +
            "ON [Debt].[ID] = [ClientInDebt].[DebtID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientInDebt].[AgreementID] " +
            "WHERE (DATENAME(dayofyear , GetDate())  + (DATEPART(YEAR, GETDATE()) - DATEPART(Year, [Debt].Created)) * 365) - " +
            "(DATENAME(dayofyear , [Debt].Created) + [Agreement].NumberDaysDebt) > 0").FirstOrDefault();
    }

    private int GetTotalMissedDays(string partQuery) {
        return _connection.Query<int>(
            "SELECT " +
            "CASE " +
            "WHEN " +
            "SUM(CASE WHEN (0 - ((DATEPART(DAYOFYEAR, GETDATE()) + ((YEAR(GETDATE()) - YEAR([Debt].[Created])) * 365)) - " +
            "(DATEPART(DAYOFYEAR, [Debt].[Created]) + [Agreement].[NumberDaysDebt]))) < 0 " +
            "THEN 0 - ((DATEPART(DAYOFYEAR, GETDATE()) + ((YEAR(GETDATE()) - YEAR([Debt].[Created])) * 365)) - " +
            "(DATEPART(DAYOFYEAR, [Debt].[Created]) + [Agreement].[NumberDaysDebt])) END) IS NULL " +
            "THEN 0 " +
            "ELSE " +
            "SUM(CASE WHEN (0 - ((DATEPART(DAYOFYEAR, GETDATE()) + ((YEAR(GETDATE()) - YEAR([Debt].[Created])) * 365)) - " +
            "(DATEPART(DAYOFYEAR, [Debt].[Created]) + [Agreement].[NumberDaysDebt]))) < 0 " +
            "THEN 0 - ((DATEPART(DAYOFYEAR, GETDATE()) + ((YEAR(GETDATE()) - YEAR([Debt].[Created])) * 365)) - " +
            "(DATEPART(DAYOFYEAR, [Debt].[Created]) + [Agreement].[NumberDaysDebt])) END) " +
            "END AS [Value] " +
            partQuery).SingleOrDefault();
    }
}