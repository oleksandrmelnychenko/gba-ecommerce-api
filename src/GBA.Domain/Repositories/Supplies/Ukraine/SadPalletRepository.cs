using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SadPalletRepository : ISadPalletRepository {
    private readonly IDbConnection _connection;

    public SadPalletRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SadPallet sadPallet) {
        return _connection.Query<long>(
                "INSERT INTO [SadPallet] (SadId, SadPalletTypeId, Number, Comment, Updated) " +
                "VALUES (@SadId, @SadPalletTypeId, @Number, @Comment, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                sadPallet
            )
            .Single();
    }

    public void Update(SadPallet sadPallet) {
        _connection.Execute(
            "UPDATE [SadPallet] " +
            "SET SadPalletTypeId = @SadPalletTypeId, Number = @Number, Comment = @Comment, Updated = GETUTCDATE() " +
            "WHERE [SadPallet].ID = @Id",
            sadPallet
        );
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [SadPallet] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [SadPallet].ID = @Id",
            new { Id = id }
        );
    }

    public List<SadPallet> GetAllBySadIdExceptProvided(long sadId, IEnumerable<long> ids) {
        List<SadPallet> pallets = new();

        _connection.Query<SadPallet, SadPalletItem, SadPallet>(
            "SELECT * " +
            "FROM [SadPallet] " +
            "LEFT JOIN [SadPalletItem] " +
            "ON [SadPalletItem].SadPalletID = [SadPallet].ID " +
            "AND [SadPalletItem].Deleted = 0 " +
            "WHERE [SadPallet].SadID = @SadId " +
            "AND [SadPallet].Deleted = 0 " +
            "AND [SadPallet].ID NOT IN @Ids",
            (pallet, item) => {
                if (!pallets.Any(p => p.Id.Equals(pallet.Id))) {
                    if (item != null) pallet.SadPalletItems.Add(item);

                    pallets.Add(pallet);
                } else if (item != null) {
                    pallets.First(p => p.Id.Equals(pallet.Id)).SadPalletItems.Add(item);
                }

                return pallet;
            },
            new { SadId = sadId, Ids = ids }
        );

        return pallets;
    }
}