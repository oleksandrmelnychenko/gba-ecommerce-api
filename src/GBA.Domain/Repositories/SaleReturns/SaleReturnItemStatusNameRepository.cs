using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Repositories.SaleReturns.Contracts;

namespace GBA.Domain.Repositories.SaleReturns;

public sealed class SaleReturnItemStatusNameRepository : ISaleReturnItemStatusNameRepository {
    private readonly IDbConnection _connection;

    public SaleReturnItemStatusNameRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<SaleReturnItemStatusName> GetAll() {
        return _connection.Query<SaleReturnItemStatusName>(
            "SELECT * FROM [SaleReturnItemStatusName] " +
            "WHERE [SaleReturnItemStatusName].[Deleted] = 0").ToList();
    }

    public Dictionary<SaleReturnItemStatus, double> GetSaleReturnQuantityGroupByReason(DateTime from, DateTime to, bool forMyClient, long userId, Guid? clientNetId) {
        string sqlQuery = "SELECT [SaleReturnItemStatusName].[SaleReturnItemStatus], " +
                          "SUM([SaleReturnItem].[Qty]) AS Qty " +
                          "FROM [SaleReturnItemStatusName] " +
                          "LEFT JOIN [SaleReturnItem] " +
                          "ON [SaleReturnItem].[Deleted] = 0 AND " +
                          "[SaleReturnItem].[SaleReturnItemStatus] = [SaleReturnItemStatusName].[SaleReturnItemStatus] " +
                          "LEFT JOIN [SaleReturn] " +
                          "ON [SaleReturn].[Deleted] = 0 AND  " +
                          "[SaleReturn].[ID] = [SaleReturnItem].[SaleReturnID] " +
                          "LEFT JOIN [Client]  " +
                          "ON [Client].[ID] = [SaleReturn].[ClientID]  " +
                          "LEFT JOIN [ClientUserProfile]  " +
                          "ON [ClientUserProfile].[ClientID] = [Client].[ID]  " +
                          "WHERE [SaleReturnItemStatusName].[Deleted] = 0 AND  " +
                          "[SaleReturn].[FromDate] >= @from AND " +
                          "[SaleReturn].[FromDate] <= @to ";

        object param = new { From = from, To = to };

        if (clientNetId != null) {
            sqlQuery += "AND [Client].[NetUID] = @ClientNetId ";
            param = new { From = from, To = to, ClientNetId = clientNetId };
        }

        if (forMyClient && userId != 0) {
            sqlQuery += "AND [ClientUserProfile].[UserProfileID] = @UserId ";

            param = new { From = from, To = to, UserId = userId };

            if (clientNetId != null)
                param = new { From = from, To = to, ClientNetId = clientNetId, UserId = userId };
        }

        sqlQuery += "GROUP BY [SaleReturnItemStatusName].[SaleReturnItemStatus]";

        Dictionary<SaleReturnItemStatus, double> saleReturnQuantityGroupByReason = new();

        _connection.Query(
            sqlQuery,
            (SaleReturnItemStatus status, double qty) => {
                saleReturnQuantityGroupByReason.Add(status, qty);

                return qty;
            }, param, splitOn: "SaleReturnItemStatus, Qty"
        );

        return saleReturnQuantityGroupByReason;
    }
}