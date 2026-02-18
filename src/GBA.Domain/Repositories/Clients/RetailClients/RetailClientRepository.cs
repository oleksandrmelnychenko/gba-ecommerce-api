using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Repositories.Clients.RetailClients.Contracts;

namespace GBA.Domain.Repositories.Clients.RetailClients;

public sealed class RetailClientRepository : IRetailClientRepository {
    private readonly IDbConnection _connection;

    public RetailClientRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(RetailClient client) {
        return _connection.Query<long>(
            "INSERT INTO [RetailClient] " +
            "([Name], [PhoneNumber], [EcommerceRegionId], [ShoppingCartJson], [Updated]) " +
            "VALUES (@Name, @PhoneNumber, @EcommerceRegionId, @ShoppingCartJson, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY() ",
            client
        ).FirstOrDefault();
    }

    public void Update(RetailClient retailClient) {
        _connection.Execute(
            "UPDATE [RetailClient] " +
            "SET [Name] = @Name, [PhoneNumber] = @PhoneNumber, [EcommerceRegionId] = @EcommerceRegionId, " +
            "[ShoppingCartJson] = @ShoppingCartJson, [Updated] = GETUTCDATE() " +
            "WHERE [RetailClient].ID = @Id ",
            retailClient);
    }

    public RetailClient GetByPhoneNumber(string number) {
        return _connection.Query<RetailClient, EcommerceRegion, RetailClient>(
            "SELECT * FROM [RetailClient] " +
            "LEFT JOIN [EcommerceRegion] " +
            "ON [EcommerceRegion].ID = [RetailClient].EcommerceRegionId " +
            "WHERE [RetailClient].PhoneNumber LIKE CONCAT('%', @Number) " +
            "AND [RetailClient].[Deleted] = 0 ",
            (retailClient, ecommerceRegion) => {
                if (retailClient != null) retailClient.EcommerceRegion = ecommerceRegion;

                return retailClient;
            },
            new { Number = number }
        ).FirstOrDefault();
    }

    public RetailClient GetByNetId(Guid netId) {
        return _connection.Query<RetailClient, EcommerceRegion, RetailClient>(
            "SELECT * FROM [RetailClient] " +
            "LEFT JOIN [EcommerceRegion] " +
            "ON [EcommerceRegion].ID = [RetailClient].EcommerceRegionId " +
            "WHERE [RetailClient].[NetUID] = @NetId " +
            "AND [RetailClient].[Deleted] = 0 ",
            (retailClient, ecommerceRegion) => {
                if (retailClient != null) retailClient.EcommerceRegion = ecommerceRegion;

                return retailClient;
            },
            new { NetId = netId }
        ).FirstOrDefault();
    }

    public RetailClient GetRetailClientById(long id) {
        return _connection.Query<RetailClient, EcommerceRegion, RetailClient>(
            "SELECT * FROM [RetailClient] " +
            "LEFT JOIN [EcommerceRegion] " +
            "ON [EcommerceRegion].ID = [RetailClient].EcommerceRegionId " +
            "WHERE [RetailClient].[ID] = @Id " +
            "AND [RetailClient].[Deleted] = 0",
            (retailClient, ecommerceRegion) => {
                if (retailClient != null) retailClient.EcommerceRegion = ecommerceRegion;

                return retailClient;
            },
            new { Id = id }
        ).FirstOrDefault();
    }

    public IEnumerable<RetailClient> GetAll() {
        return _connection.Query<RetailClient, EcommerceRegion, RetailClient>(
            "SELECT * FROM [RetailClient] " +
            "LEFT JOIN [EcommerceRegion] " +
            "ON [EcommerceRegion].ID = [RetailClient].EcommerceRegionId " +
            "WHERE [RetailClient].[Deleted] = 0 " +
            "ORDER BY [RetailClient].[Created] DESC ",
            (retailClient, ecommerceRegion) => {
                if (retailClient != null) retailClient.EcommerceRegion = ecommerceRegion;

                return retailClient;
            });
    }

    public IEnumerable<RetailClient> GetAllFiltered(string value, long limit, long offset) {
        return _connection.Query<RetailClient, EcommerceRegion, RetailClient>(
            ";WITH [Search_CTE] AS ( " +
            "SELECT [RetailClient].ID, [RetailClient].Created FROM RetailClient " +
            "WHERE [RetailClient].Deleted = 0 " +
            "AND [RetailClient].PhoneNumber LIKE '%' + @Value + '%' " +
            ") " +
            ", [RowNumbers_CTE] AS ( " +
            "SELECT [Search_CTE].ID " +
            ", [Search_CTE].Created " +
            ", ROW_NUMBER() OVER (ORDER BY Created DESC) AS [RowNumber] " +
            "FROM [Search_CTE] " +
            ") " +
            "SELECT * FROM [RetailClient] " +
            "LEFT JOIN [EcommerceRegion] " +
            "ON [EcommerceRegion].ID = [RetailClient].EcommerceRegionId " +
            "WHERE [RetailClient].ID IN ( " +
            "SELECT [RowNumbers_CTE].ID " +
            "FROM [RowNumbers_CTE] " +
            "WHERE [RowNumbers_CTE].RowNumber > @Offset " +
            "AND [RowNumbers_CTE].RowNumber < @Limit + @Offset " +
            ") ",
            (retailClient, ecommerceRegion) => {
                if (retailClient != null) retailClient.EcommerceRegion = ecommerceRegion;

                return retailClient;
            },
            new {
                Value = value,
                Limit = limit,
                Offset = offset
            });
    }
}