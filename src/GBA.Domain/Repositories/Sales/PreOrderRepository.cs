using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Domain.Repositories.Sales;

public sealed class PreOrderRepository : IPreOrderRepository {
    private readonly IDbConnection _connection;

    public PreOrderRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PreOrder preOrder) {
        return _connection.Query<long>(
                "INSERT INTO [PreOrder] (Comment, MobileNumber, Qty, ProductID, ClientID, Culture, Updated) " +
                "VALUES (@Comment, @MobileNumber, @Qty, @ProductId, @ClientId, @Culture, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                preOrder
            )
            .Single();
    }

    public PreOrder GetById(long id) {
        return _connection.Query<PreOrder, Product, Client, RegionCode, PreOrder>(
                "SELECT * " +
                "FROM [PreOrder] " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [PreOrder].ProductID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [PreOrder].ClientID " +
                "LEFT JOIN [RegionCode] " +
                "ON [RegionCode].ID = [Client].RegionCodeID " +
                "WHERE [PreOrder].ID = @Id",
                (preOrder, product, client, regionCode) => {
                    if (client != null) client.RegionCode = regionCode;

                    preOrder.Client = client;
                    preOrder.Product = product;

                    return preOrder;
                },
                new {
                    Id = id
                }
            )
            .SingleOrDefault();
    }

    public IEnumerable<PreOrder> GetAllByCurrentCultureFiltered(long limit, long offset) {
        return _connection.Query<PreOrder, Product, Client, RegionCode, PreOrder>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ID " +
            ", ROW_NUMBER() OVER(ORDER BY Created DESC) AS RowNumber " +
            "FROM [PreOrder] " +
            "WHERE Culture = @Culture" +
            ") " +
            "SELECT * " +
            "FROM [PreOrder] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [PreOrder].ProductID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [PreOrder].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "WHERE [PreOrder].ID IN ( " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE RowNumber > @Offset " +
            "AND RowNumber <= @Offset + @Limit " +
            ")",
            (preOrder, product, client, regionCode) => {
                if (client != null) client.RegionCode = regionCode;

                preOrder.Client = client;
                preOrder.Product = product;

                return preOrder;
            },
            new {
                Limit = limit,
                Offset = offset,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
    }
}