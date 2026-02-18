using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductOneCRepository : IProductOneCRepository {
    private readonly IDbConnection _connection;

    public ProductOneCRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<long> GetMostPurchasedByClientRefId(string clientRefId) {
        return _connection.Query<long>(
            ";WITH [ClientOrders] AS ( " +
            "SELECT [Orders].[_IDRRef] AS [Reference] " +
            "FROM [_Document186] AS [Orders] WITH(NOLOCK) " +
            "WHERE Convert(NCHAR(32), [Orders].[_Fld3196RRef], 2) = @CustomerRefId), " +
            "[ProductSourceIds] AS ( " +
            "SELECT TOP 20 " +
            "[OrderProducts].[_Fld3226RRef] AS [SourceID] " +
            "FROM [ClientOrders] " +
            "LEFT JOIN [_Document186_VT3219] AS [OrderProducts] WITH(NOLOCK) " +
            "ON [OrderProducts].[_Document186_IDRRef] = [ClientOrders].[Reference] " +
            "WHERE [OrderProducts].[_Fld3226RRef] IS NOT NULL " +
            "GROUP BY [OrderProducts].[_Fld3226RRef] " +
            "ORDER BY SUM(ISNULL([OrderProducts].[_Fld3223], 0)) DESC) " +
            "SELECT CAST([Products].[_Code] AS NUMERIC(9)) AS [OldEcommerceId] " +
            "FROM [ProductSourceIds] " +
            "LEFT JOIN [_Reference84] AS [Products] WITH(NOLOCK) " +
            "ON [ProductSourceIds].[SourceID] = [Products].[_IDRRef]",
            new { CustomerRefId = clientRefId }
        );
    }

    public IEnumerable<long> GetMostPurchasedByRegionName(string regionName) {
        return _connection.Query<long>(
            ";WITH [Clients] AS( " +
            "SELECT " +
            "[Clients].[_IDRRef] AS [Client] " +
            "FROM [_Reference110] AS [Regions] WITH(NOLOCK) " +
            "LEFT JOIN [_Reference68] AS [Clients] WITH(NOLOCK) " +
            "ON [Regions].[_IDRRef] = [Clients].[_Fld1129RRef] " +
            "WHERE TRIM([Regions].[_Code]) = TRIM(@RegionName)), " +
            "[ClientOrders] AS( " +
            "SELECT [ClientOrders].[_IDRRef] AS [Reference] " +
            "FROM [_Document186] AS [ClientOrders] WITH(NOLOCK) " +
            "WHERE [ClientOrders].[_Fld3196RRef] IN (SELECT [Client] FROM [Clients])), " +
            "[ProductSourceIds] AS( " +
            "SELECT TOP 20 " +
            "[DocProducts].[_Fld3226RRef] AS [Reference] " +
            "FROM [ClientOrders] " +
            "LEFT JOIN [_Document186_VT3219] AS [DocProducts] WITH(NOLOCK) " +
            "ON [DocProducts].[_Document186_IDRRef] = [ClientOrders].[Reference] " +
            "WHERE [DocProducts].[_Fld3226RRef] IS NOT NULL " +
            "GROUP BY [DocProducts].[_Fld3226RRef] " +
            "ORDER BY SUM(ISNULL([DocProducts].[_Fld3223], 0)) DESC) " +
            "SELECT CAST([Products].[_Code] AS NUMERIC(9)) AS [OldEcommerceId] " +
            "FROM [ProductSourceIds] " +
            "LEFT JOIN [_Reference84] AS [Products] WITH(NOLOCK) " +
            "ON [ProductSourceIds].[Reference] = [Products].[_IDRRef]",
            new { RegionName = regionName }
        );
    }

    public IEnumerable<long> GetFromSearchBySales(string searchValue) {
        return _connection.Query<long>(
            "SELECT [Products]._Code AS OldEcommerceId " +
            "FROM [_Reference84] AS [Products] " +
            "WHERE [Products]._Marked = 0 " +
            "AND PATINDEX('%' + @SearchValue + '%', [Products]._Fld1306) > 0 " +
            "AND [Products]._IDRRef IN (SELECT [OrderItems]._Fld3226RRef " +
            "FROM[_Document186_VT3219] AS[OrderItems] " +
            "LEFT JOIN[_Document186] AS[ClientOrder] " +
            "ON[ClientOrder]._IDRRef = [OrderItems]._Document186_IDRRef " +
            "WHERE ClientOrder._Posted = 1)",
            new { SearchValue = searchValue }
        );
    }
}