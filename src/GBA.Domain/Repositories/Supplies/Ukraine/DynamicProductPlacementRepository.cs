using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class DynamicProductPlacementRepository : IDynamicProductPlacementRepository {
    private readonly IDbConnection _connection;

    public DynamicProductPlacementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(DynamicProductPlacement placement) {
        return _connection.Query<long>(
                "INSERT INTO [DynamicProductPlacement] (IsApplied, Qty, StorageNumber, RowNumber, CellNumber, DynamicProductPlacementRowId, Updated) " +
                "VALUES (@IsApplied, @Qty, @StorageNumber, @RowNumber, @CellNumber, @DynamicProductPlacementRowId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                placement
            )
            .Single();
    }

    public void Add(IEnumerable<DynamicProductPlacement> placements) {
        _connection.Execute(
            "INSERT INTO [DynamicProductPlacement] (IsApplied, Qty, StorageNumber, RowNumber, CellNumber, DynamicProductPlacementRowId, Updated) " +
            "VALUES (@IsApplied, @Qty, @StorageNumber, @RowNumber, @CellNumber, @DynamicProductPlacementRowId, GETUTCDATE())",
            placements
        );
    }

    public void Update(DynamicProductPlacement placement) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacement] " +
            "SET Qty = @Qty, StorageNumber = @StorageNumber, RowNumber = @RowNumber, CellNumber = @CellNumber, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            placement
        );
    }

    public void Update(IEnumerable<DynamicProductPlacement> placements) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacement] " +
            "SET Qty = @Qty, StorageNumber = @StorageNumber, RowNumber = @RowNumber, CellNumber = @CellNumber, Updated = GETUTCDATE(), Deleted = @Deleted " +
            "WHERE ID = @Id",
            placements
        );
    }

    public void SetIsAppliedById(long id) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacement] " +
            "SET IsApplied = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }

    public void RemoveAllByRowIdExceptProvided(long rowId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE DynamicProductPlacementRowID = @RowId " +
            "AND ID NOT IN @Ids " +
            "AND IsApplied = 0",
            new { RowId = rowId, Ids = ids }
        );
    }

    public void RemoveAllByRowId(long rowId) {
        _connection.Execute(
            "UPDATE [DynamicProductPlacement] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE DynamicProductPlacementRowID = @RowId " +
            "AND IsApplied = 0",
            new { RowId = rowId }
        );
    }

    public IEnumerable<DynamicProductPlacement> GetAllAppliedByRowId(long rowId) {
        return _connection.Query<DynamicProductPlacement>(
            "SELECT * " +
            "FROM [DynamicProductPlacement] " +
            "WHERE DynamicProductPlacementRowID = @RowId " +
            "AND IsApplied = 1",
            new { RowId = rowId }
        );
    }

    public IEnumerable<DynamicProductPlacement> GetAllByRowIdExceptProvided(long rowId, IEnumerable<long> ids) {
        return _connection.Query<DynamicProductPlacement>(
            "SELECT * " +
            "FROM [DynamicProductPlacement] " +
            "WHERE DynamicProductPlacementRowID = @RowId " +
            "AND ID NOT IN @Ids " +
            "AND IsApplied = 0 " +
            "AND Deleted = 0",
            new { RowId = rowId, Ids = ids }
        );
    }
}