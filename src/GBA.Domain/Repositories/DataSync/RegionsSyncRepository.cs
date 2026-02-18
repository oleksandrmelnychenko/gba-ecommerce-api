using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Repositories.DataSync.Contracts;

namespace GBA.Domain.Repositories.DataSync;

public sealed class RegionsSyncRepository : IRegionsSyncRepository {
    private readonly IDbConnection _amgConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    public RegionsSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgConnection) {
        _oneCConnection = oneCConnection;

        _remoteSyncConnection = remoteSyncConnection;
        _amgConnection = amgConnection;
    }

    public IEnumerable<string> GetAllRegionsForSync() {
        return _oneCConnection.Query<string>(
            "SELECT " +
            "T1._Code [Code] " +
            "FROM dbo._Reference110 T1 WITH(NOLOCK)"
        );
    }

    public IEnumerable<string> GetAllAmgRegionsForSync() {
        return _amgConnection.Query<string>(
            "SELECT " +
            "T1._Code [Code] " +
            "FROM dbo._Reference137 T1 WITH(NOLOCK)"
        );
    }

    public IEnumerable<Region> GetAllExistingRegions() {
        return _remoteSyncConnection.Query<Region>(
            "SELECT * " +
            "FROM [Region]"
        );
    }

    public void Add(Region region) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [Region] ([Name], Updated) " +
            "VALUES (@Name, GETUTCDATE())",
            region
        );
    }

    public void Update(Region region) {
        _remoteSyncConnection.Execute(
            "UPDATE [Region] " +
            "SET [Name] = @Name, Updated = GETUTCDATE(), Deleted = @Deleted " +
            "WHERE ID = @Id",
            region
        );
    }

    public void RemoveAllExistingRegions() {
        _remoteSyncConnection.Execute(
            "UPDATE [Region] " +
            "SET Updated = GETUTCDATE(), Deleted = 1 " +
            "WHERE Deleted = 0"
        );
    }
}