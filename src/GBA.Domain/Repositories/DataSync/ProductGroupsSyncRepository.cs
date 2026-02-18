using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Repositories.DataSync.Contracts;

namespace GBA.Domain.Repositories.DataSync;

public sealed class ProductGroupsSyncRepository : IProductGroupsSyncRepository {
    private readonly IDbConnection _amgSyncConnection;
    private readonly IDbConnection _oneCConnection;

    private readonly IDbConnection _remoteSyncConnection;

    public ProductGroupsSyncRepository(
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        IDbConnection amgSyncConnection) {
        _oneCConnection = oneCConnection;

        _remoteSyncConnection = remoteSyncConnection;

        _amgSyncConnection = amgSyncConnection;
    }

    public IEnumerable<SyncProductGroup> GetAllSyncProductGroup() {
        return _oneCConnection.Query<SyncProductGroup>(
            "SELECT " +
            "T1._IDRRef [SourceId], " +
            "T1._Description [Name], " +
            "T1._ParentIDRRef [ParentSourceId], " +
            "(CASE WHEN T1._ParentIDRRef = 0x00000000000000000000000000000000 THEN 0 ELSE 1 END) [IsSubGroup] " +
            "FROM dbo._Reference84 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference84 T2 WITH(NOLOCK) " +
            "ON T1._ParentIDRRef = T2._IDRRef " +
            "WHERE (T1._Marked = 0x00) " +
            "AND (T1._Folder = 0x00) " +
            "ORDER BY CASE WHEN T2._Code IS NULL THEN 0 ELSE 1 END "
        );
    }

    public IEnumerable<SyncProductGroup> GetAmgAllSyncProductGroup() {
        return _amgSyncConnection.Query<SyncProductGroup>(
            "SELECT " +
            "T1._IDRRef [SourceId], " +
            "T1._Description [Name], " +
            "T2._IDRRef [ParentSourceId], " +
            "(CASE WHEN T1._ParentIDRRef = 0x00000000000000000000000000000000 THEN 0 ELSE 1 END) [IsSubGroup] " +
            "FROM dbo._Reference108 T1 WITH(NOLOCK) " +
            "LEFT OUTER JOIN dbo._Reference108 T2 WITH(NOLOCK) " +
            "ON T1._ParentIDRRef = T2._IDRRef " +
            "WHERE (T1._Folder = 0x00) AND (T1._Marked = 0x00) " +
            "ORDER BY CASE WHEN T2._Code IS NULL THEN 0 ELSE 1 END "
        );
    }

    public IEnumerable<ProductGroup> GetAllProductGroups() {
        return _remoteSyncConnection.Query<ProductGroup>(
            "SELECT * " +
            "FROM [ProductGroup] "
        );
    }

    public ProductSubGroup GetProductSubGroupByIdsIfExists(long rootProductGroupId, long subProductGroupId) {
        return _remoteSyncConnection.Query<ProductSubGroup>(
            "SELECT TOP(1) * " +
            "FROM [ProductSubGroup] " +
            "WHERE [ProductSubGroup].RootProductGroupID = @RootProductGroupId " +
            "AND [ProductSubGroup].SubProductGroupID = @SubProductGroupId",
            new { RootProductGroupId = rootProductGroupId, SubProductGroupId = subProductGroupId }
        ).SingleOrDefault();
    }

    public ProductGroup GetProductGroupBySourceId(byte[] sourceId) {
        DynamicParameters parameters = new();
        parameters.Add("SourceId", sourceId, DbType.Binary, ParameterDirection.Input, 16);

        return _remoteSyncConnection.Query<ProductGroup>(
            "SELECT TOP(1) * " +
            "FROM [ProductGroup] " +
            "WHERE [ProductGroup].Deleted = 0 " +
            "AND ([ProductGroup].SourceAmgId = @SourceId " +
            "OR [ProductGroup].SourceFenixId = @SourceId) ",
            new { SourceId = sourceId }
        ).SingleOrDefault();
    }

    public long Add(ProductGroup productGroup) {
        return _remoteSyncConnection.Query<long>(
            "INSERT INTO [ProductGroup] " +
            "([Name], [FullName], [Description], [IsSubGroup], [SourceAmgId], [SourceFenixId], Updated) " +
            "VALUES " +
            "(@Name, @FullName, @Description, @IsSubGroup, @SourceAmgId, @SourceFenixId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            productGroup
        ).Single();
    }

    public void Add(ProductSubGroup productSubGroup) {
        _remoteSyncConnection.Execute(
            "INSERT INTO [ProductSubGroup] " +
            "(RootProductGroupID, SubProductGroupID, Updated) " +
            "VALUES " +
            "(@RootProductGroupId, @SubProductGroupId, GETUTCDATE())",
            productSubGroup
        );
    }

    public void Update(ProductGroup productGroup) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductGroup] " +
            "SET [Name] = @Name, [FullName] = @FullName, [Description] = @Description, [IsSubGroup] = @IsSubGroup, Updated = GETUTCDATE(), [Deleted] = @Deleted, " +
            "[SourceAmgId] = @SourceAmgId, [SourceFenixId] = @SourceFenixId " +
            "WHERE ID = @Id",
            productGroup
        );
    }

    public void Update(ProductSubGroup productSubGroup) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductSubGroup] " +
            "SET Deleted = @Deleted, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            productSubGroup
        );
    }

    public void RemoveAssignmentsForRootProductGroupById(long productGroupId) {
        _remoteSyncConnection.Execute(
            "UPDATE [ProductSubGroup] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE RootProductGroupID = @ProductGroupId",
            new { ProductGroupId = productGroupId }
        );
    }
}