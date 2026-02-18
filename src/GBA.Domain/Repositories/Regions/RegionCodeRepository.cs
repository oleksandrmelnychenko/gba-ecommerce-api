using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Repositories.Regions.Contracts;

namespace GBA.Domain.Repositories.Regions;

public sealed class RegionCodeRepository : IRegionCodeRepository {
    private readonly IDbConnection _connection;

    public RegionCodeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(RegionCode regionCode) {
        return _connection.Query<long>(
                "INSERT INTO RegionCode (Value, RegionId, City, District, Updated) " +
                "VALUES (@Value, @RegionId, @City, @District, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                regionCode
            )
            .Single();
    }

    public void Update(RegionCode regionCode) {
        _connection.Execute(
            "UPDATE RegionCode SET " +
            "Value = @Value, RegionId = @RegionId, City = @City, District = @District, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid ",
            regionCode
        );
    }

    public void UpdateRegionId(RegionCode regionCode) {
        _connection.Execute(
            "UPDATE RegionCode SET " +
            " RegionId = @RegionId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid ",
            regionCode
        );
    }

    public RegionCode GetById(long id) {
        return _connection.Query<RegionCode>(
                "SELECT * FROM RegionCode " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public RegionCode GetByNetId(Guid netId) {
        return _connection.Query<RegionCode>(
                "SELECT * FROM RegionCode " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public RegionCode GetLastRecordByRegionName(string regionName) {
        return _connection.Query<RegionCode, Region, RegionCode>(
                "SELECT TOP 1 * FROM RegionCode " +
                "LEFT JOIN Region " +
                "ON RegionCode.RegionID = Region.ID " +
                "WHERE Region.Name = @RegionName " +
                "AND RegionCode.Deleted = 0 " +
                "ORDER BY RegionCode.ID DESC",
                (regionCode, region) => {
                    regionCode.Region = region;

                    return regionCode;
                },
                new { RegionName = regionName }
            )
            .SingleOrDefault();
    }

    public RegionCode GetAvailableRecordByRegionName(string regionName) {
        return _connection.Query<RegionCode, Region, Client, RegionCode>(
                "SELECT TOP 1 * FROM RegionCode " +
                "LEFT JOIN Region " +
                "ON RegionCode.RegionID = Region.ID " +
                "LEFT JOIN Client " +
                "ON Client.RegionCodeID = RegionCode.ID " +
                "WHERE Region.Name = @RegionName " +
                "AND RegionCode.Deleted = 0 " +
                "AND Client.ID IS NULL " +
                "ORDER BY RegionCode.ID ASC",
                (regionCode, region, client) => {
                    regionCode.Region = region;

                    return regionCode;
                },
                new { RegionName = regionName }
            )
            .SingleOrDefault();
    }

    public RegionCode GetRecordByCodeAndValue(string value) {
        return _connection.Query<RegionCode>(
                "SELECT TOP 1 * FROM RegionCode " +
                "WHERE Value = @Value ORDER BY ID DESC",
                new { Value = value }
            )
            .SingleOrDefault();
    }

    public RegionCode GetLastRecord() {
        return _connection.Query<RegionCode>(
                "SELECT TOP 1 * FROM RegionCode " +
                "WHERE Deleted = 0 " +
                "ORDER BY ID DESC "
            )
            .SingleOrDefault();
    }

    public List<RegionCode> GetAll() {
        return _connection.Query<RegionCode>(
                "SELECT * FROM RegionCode " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public bool IsAssignedToAnyContact(long regionCodeId) {
        return _connection.Query<long>(
            "SELECT DISTINCT RegionCode.ID FROM RegionCode " +
            "LEFT JOIN Client " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "WHERE RegionCode.ID = @Id " +
            "AND RegionCode.Deleted = 0 " +
            "AND Client.Deleted = 0",
            new { Id = regionCodeId }
        ).Any();
    }

    public bool IsAssignedToAnyContact(string value) {
        return _connection.Query<long>(
            "SELECT DISTINCT RegionCode.ID FROM RegionCode " +
            "LEFT JOIN Client " +
            "ON Client.RegionCodeID = RegionCode.ID " +
            "WHERE RegionCode.[Value] = @Value " +
            "AND RegionCode.Deleted = 0 " +
            "AND Client.Deleted = 0 " +
            "AND Client.ID IS NOT NULL",
            new { Value = value }
        ).Any();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE RegionCode SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId ",
            new { NetId = netId.ToString() }
        );
    }
}