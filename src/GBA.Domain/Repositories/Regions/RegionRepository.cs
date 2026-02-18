using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Repositories.Regions.Contracts;

namespace GBA.Domain.Repositories.Regions;

public sealed class RegionRepository : IRegionRepository {
    private readonly IDbConnection _connection;

    public RegionRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(Region region) {
        return _connection.Query<long>(
                "INSERT INTO Region (Name, Updated) " +
                "VALUES (@Name, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                region
            )
            .Single();
    }

    public void Update(Region region) {
        _connection.Execute(
            "UPDATE Region SET " +
            "Name = @Name, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            region
        );
    }

    public Region GetById(long id) {
        return _connection.Query<Region>(
                "SELECT * FROM Region " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public Region GetByNetId(Guid netId) {
        return _connection.Query<Region>(
                "SELECT * FROM Region " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public Region GetByName(string name) {
        return _connection.Query<Region>(
                "SELECT * FROM Region " +
                "WHERE Name = @Name " +
                "AND Deleted = 0",
                new { Name = name }
            )
            .FirstOrDefault();
    }

    public Region GetLastRecord() {
        return _connection.Query<Region>(
                "SELECT TOP 1 * FROM Region " +
                "WHERE Deleted = 0 " +
                "ORDER BY Created DESC"
            )
            .SingleOrDefault();
    }


    public List<Region> GetAll() {
        return _connection.Query<Region>(
                "SELECT * FROM Region " +
                "WHERE Deleted = 0 ORDER BY Name"
            )
            .ToList();
    }

    public List<dynamic> GetTopByRegions() {
        return _connection.Query<dynamic>(
                "WITH Region_CTE (RegionName, SaleID, ProductPrice) " +
                "AS " +
                "( " +
                "SELECT Region.Name AS RegionName, Sale.ID AS SaleID, SUM(ProductPricing.Price * OrderItem.Qty) AS ProductPrice " +
                "FROM Region " +
                "LEFT JOIN Client " +
                "ON Region.ID = Client.RegionID " +
                "AND Client.Deleted = 0 " +
                "LEFT JOIN ClientAgreement " +
                "ON Client.ID = ClientAgreement.ClientID " +
                "AND ClientAgreement.Deleted = 0 " +
                "LEFT JOIN Sale " +
                "ON ClientAgreement.ID = Sale.ClientAgreementID " +
                "AND Sale.Deleted = 0 " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = Sale.OrderID " +
                "AND [Order].Deleted = 0 " +
                "LEFT JOIN OrderItem " +
                "ON OrderItem.OrderID = [Order].ID " +
                "AND OrderItem.Deleted = 0 " +
                "LEFT JOIN Product " +
                "ON Product.ID = OrderItem.ProductID " +
                "LEFT JOIN ProductPricing " +
                "ON ProductPricing.ID = (" +
                "SELECT TOP (1) ProductPricing.ID " +
                "FROM ProductPricing " +
                "WHERE ProductPricing.ProductID = Product.ID " +
                "AND ProductPricing.Deleted = 0 " +
                ")" +
                "WHERE Region.Deleted = 0 " +
                "GROUP BY Region.Name, Sale.ID " +
                ")" +
                "SELECT TOP(5) RegionName, COUNT(SaleID) AS TotalSalesCount, ROUND(SUM(ProductPrice), 2) AS TotalAmount " +
                "FROM Region_CTE " +
                "GROUP BY RegionName " +
                "ORDER BY TotalSalesCount DESC, RegionName "
            )
            .ToList();
    }

    public List<Region> GetAllWithAllCodes() {
        List<Region> regions = new();

        _connection.Query<Region, RegionCode, Region>(
            "SELECT * FROM Region " +
            "LEFT JOIN RegionCode " +
            "ON Region.ID = RegionCode.RegionID " +
            "AND RegionCode.Deleted = 0 " +
            "WHERE Region.Deleted = 0 " +
            "ORDER BY Region.Name, RegionCode.Value",
            (region, regionCode) => {
                if (regions.Any(r => r.Id.Equals(region.Id))) {
                    regions.First(r => r.Id.Equals(region.Id)).RegionCodes.Add(regionCode);
                } else {
                    if (regionCode != null) region.RegionCodes.Add(regionCode);

                    regions.Add(region);
                }

                return region;
            }
        );

        return regions;
    }

    public bool IsAssignedToRegionCode(long regionId) {
        return _connection.Query<long>(
            "SELECT Region.ID FROM Region " +
            "LEFT JOIN RegionCode " +
            "ON RegionCode.RegionID = Region.ID " +
            "LEFT JOIN Client " +
            "ON RegionCode.ID = Client.RegionCodeID " +
            "WHERE Region.ID = @Id " +
            "AND Region.Deleted = 0 " +
            "AND RegionCode.Deleted = 0 " +
            "AND Client.Deleted = 0",
            new { Id = regionId }
        ).Any();
    }

    public bool IsAssignedToClient(long regionId) {
        return _connection.Query<long>(
            "SELECT Region.ID FROM Region " +
            "LEFT JOIN Client " +
            "ON Region.ID = Client.RegionID " +
            "WHERE Region.ID = @Id " +
            "AND Region.Deleted = 0 " +
            "AND Client.Deleted = 0",
            new { Id = regionId }
        ).Any();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE Region SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}