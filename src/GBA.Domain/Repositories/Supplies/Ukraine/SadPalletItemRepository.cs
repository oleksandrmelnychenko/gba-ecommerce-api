using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SadPalletItemRepository : ISadPalletItemRepository {
    private readonly IDbConnection _connection;

    public SadPalletItemRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SadPalletItem sadPalletItem) {
        return _connection.Query<long>(
                "INSERT INTO [SadPalletItem] (Qty, SadItemId, SadPalletId, Updated) " +
                "VALUES (@Qty, @SadItemId, @SadPalletId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                sadPalletItem
            )
            .Single();
    }

    public void Update(SadPalletItem sadPalletItem) {
        _connection.Execute(
            "UPDATE [SadPalletItem] " +
            "SET Qty = @Qty, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            sadPalletItem
        );
    }

    public SadPalletItem GetById(long id) {
        return _connection.Query<SadPalletItem>(
                "SELECT * " +
                "FROM [SadPalletItem] " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public SadPalletItem GetByPalletAndSadItemIdIfExists(long palletId, long sadItemId) {
        return _connection.Query<SadPalletItem>(
                "SELECT * " +
                "FROM [SadPalletItem] " +
                "WHERE SadItemID = @SadItemId " +
                "AND SadPalletID = @PalletId " +
                "AND Deleted = 0",
                new { PalletId = palletId, SadItemId = sadItemId }
            )
            .SingleOrDefault();
    }

    public void RemoveAllByPalletIdExceptProvided(long palletId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SadPalletItem] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [SadPalletItem].SadPalletID = @PalletId " +
            "AND [SadPalletItem].ID NOT IN @Ids " +
            "AND [SadPalletItem].Deleted = 0",
            new { PalletId = palletId, Ids = ids }
        );
    }

    public void RestoreUnpackedQtyByPalletIdExceptProvidedIds(long palletId, IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [SadItem] " +
            "SET UnpackedQty = UnpackedQty + [SadPalletItem].Qty, Updated = GETUTCDATE() " +
            "FROM [SadItem] " +
            "LEFT JOIN [SadPalletItem] " +
            "ON [SadPalletItem].SadItemID = [SadItem].ID " +
            "WHERE [SadPalletItem].SadPalletID = @PalletId " +
            "AND [SadPalletItem].ID NOT IN @Ids " +
            "AND [SadPalletItem].Deleted = 0",
            new { PalletId = palletId, Ids = ids }
        );
    }
}