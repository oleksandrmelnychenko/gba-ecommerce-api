using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductSubGroupRepository : IProductSubGroupRepository {
    private readonly IDbConnection _connection;

    public ProductSubGroupRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductSubGroup productSubGroup) {
        _connection.Execute(
            "INSERT INTO ProductSubGroup (RootProductGroupID, SubProductGroupID, Updated) " +
            "VALUES(@RootProductGroupId, @SubProductGroupId, getutcdate())",
            productSubGroup
        );
    }

    public void Add(IEnumerable<ProductSubGroup> productSubGroups) {
        _connection.Execute(
            "INSERT INTO ProductSubGroup (RootProductGroupID, SubProductGroupID, Updated) " +
            "VALUES(@RootProductGroupId, @SubProductGroupId, getutcdate())",
            productSubGroups
        );
    }

    public void Remove(ProductSubGroup productSubGroup) {
        _connection.Execute(
            "UPDATE ProductSubGroup " +
            "SET RootProductGroupID = @RootProductGroupId, SubProductGroupID = @SubProductGroupId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productSubGroup
        );
    }

    public void Remove(IEnumerable<ProductSubGroup> productSubGroups) {
        _connection.Execute(
            "UPDATE ProductSubGroup " +
            "SET RootProductGroupID = @RootProductGroupId, SubProductGroupID = @SubProductGroupId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productSubGroups
        );
    }

    public void Update(ProductSubGroup productSubGroup) {
        _connection.Execute(
            "UPDATE ProductSubGroup " +
            "SET RootProductGroupID = @RootProductGroupId, SubProductGroupID = @SubProductGroupId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productSubGroup
        );
    }

    public void Update(IEnumerable<ProductSubGroup> productSubGroups) {
        _connection.Execute(
            "UPDATE ProductSubGroup " +
            "SET RootProductGroupID = @RootProductGroupId, SubProductGroupID = @SubProductGroupId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productSubGroups
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductSubGroup] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductSubGroup].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public ProductSubGroup GetByRootAndSubIds(long rootId, long subId) {
        return _connection.Query<ProductSubGroup>(
            "SELECT TOP 1 * FROM [ProductSubGroup] " +
            "WHERE [ProductSubGroup].[RootProductGroupID] = @RootId " +
            "AND [ProductSubGroup].[SubProductGroupID] = @SubId; ",
            new { RootId = rootId, SubId = subId }).FirstOrDefault();
    }

    public void Restore(long id) {
        _connection.Execute(
            "UPDATE [ProductSubGroup] " +
            "SET [Updated] = getutcdate() " +
            ", [Deleted] = 0 " +
            "WHERE [ProductSubGroup].[ID] = @Id; ",
            new { Id = id });
    }
}