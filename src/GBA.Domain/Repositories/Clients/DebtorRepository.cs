using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Regions;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class DebtorRepository : IDebtorRepository {
    private readonly IDbConnection _connection;

    public DebtorRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<Debtor> GetAllFiltered(string value, bool allDebtors, Guid userNetId, long limit, long offset) {
        List<Debtor> debtors = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT [Client].ID " +
            ",ISNULL( " +
            "( " +
            "SELECT SUM(dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, @FromDate)) " +
            "FROM [ClientInDebt] " +
            "LEFT JOIN [Debt] " +
            "ON [Debt].ID = [ClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientInDebt].AgreementID " +
            "WHERE [ClientInDebt].Deleted = 0 " +
            "AND [Debt].Deleted = 0 " +
            "AND [ClientInDebt].ClientID = [Client].ID " +
            ") " +
            ", 0) AS [EuroTotal] " +
            "FROM [Client] " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].ClientID = [Client].ID " +
            "AND [ClientUserProfile].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ClientUserProfile].UserProfileID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [Client].Deleted = 0 " +
            "AND ([Client].FullName like '%' + @Value + '%' OR [RegionCode].[Value] like '%' + @Value + '%') ";

        if (!allDebtors) sqlExpression += "AND [User].NetUID = @UserNetId ";

        sqlExpression +=
            ") " +
            ", [RowNumbers_CTE] " +
            "AS " +
            "( " +
            "SELECT [Search_CTE].ID " +
            ",[Search_CTE].EuroTotal " +
            ", ROW_NUMBER() OVER (ORDER BY [EuroTotal] DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT [Client].* " +
            ",[ClientUserProfile].* " +
            ",[User].* " +
            ",[RegionCode].* " +
            ",(CONVERT(money, ISNULL( " +
            "( " +
            "SELECT SUM(dbo.GetExchangedToEuroValue([Debt].Total, [Agreement].CurrencyID, @FromDate)) " +
            "FROM [ClientInDebt] " +
            "LEFT JOIN [Debt] " +
            "ON [Debt].ID = [ClientInDebt].DebtID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientInDebt].AgreementID " +
            "WHERE [ClientInDebt].Deleted = 0 " +
            "AND [Debt].Deleted = 0 " +
            "AND [ClientInDebt].ClientID = [Client].ID " +
            ") " +
            ", 0.00))) AS [TotalDebtForMonthEnd] " +
            "FROM [Client] " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].ClientID = [Client].ID " +
            "AND [ClientUserProfile].Deleted = 0 " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ClientUserProfile].UserProfileID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [Client].ID IN ( " +
            "SELECT [RowNumbers_CTE].ID " +
            "FROM [RowNumbers_CTE] " +
            "WHERE [RowNumbers_CTE].RowNumber > @Offset " +
            "AND [RowNumbers_CTE].RowNumber < @Limit + @Offset " +
            ") " +
            "ORDER BY [TotalDebtForMonthEnd] DESC";

        Type[] types = {
            typeof(Client),
            typeof(ClientUserProfile),
            typeof(User),
            typeof(RegionCode),
            typeof(decimal)
        };

        Func<object[], Client> mapper = objects => {
            Client client = (Client)objects[0];
            ClientUserProfile clientUserProfile = (ClientUserProfile)objects[1];
            User user = (User)objects[2];
            RegionCode regionCode = (RegionCode)objects[3];
            decimal totalEuro = (decimal)objects[4];

            if (debtors.Any(c => c.Client.Id.Equals(client.Id))) {
                Debtor fromList = debtors.First(c => c.Client.Id.Equals(client.Id));

                if (clientUserProfile != null && !fromList.Client.ClientManagers.Any(m => m.Id.Equals(clientUserProfile.Id))) {
                    clientUserProfile.UserProfile = user;

                    fromList.Client.ClientManagers.Add(clientUserProfile);
                }
            } else {
                if (clientUserProfile != null) {
                    clientUserProfile.UserProfile = user;

                    client.ClientManagers.Add(clientUserProfile);
                }

                client.RegionCode = regionCode;

                debtors.Add(new Debtor {
                    Client = client,
                    Solvency = decimal.Zero,
                    TotalDebtForMonthEnd = totalEuro,
                    TotalDebtForToday = totalEuro
                });
            }

            return client;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Value = value, UserNetId = userNetId, FromDate = DateTime.UtcNow, Offset = offset, Limit = limit },
            splitOn: "ID,TotalDebtForMonthEnd"
        );

        return debtors;
    }
}