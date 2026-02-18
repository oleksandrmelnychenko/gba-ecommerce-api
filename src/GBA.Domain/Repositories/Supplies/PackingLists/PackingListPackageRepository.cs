using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.PackingLists;

public sealed class PackingListPackageRepository : IPackingListPackageRepository {
    private readonly IDbConnection _connection;

    public PackingListPackageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PackingListPackage packingListPackage) {
        return _connection.Query<long>(
                "INSERT INTO [PackingListPackage] (GrossWeight, NetWeight, Lenght, Width, Height, CBM, PackingListId, Type, Updated) " +
                "VALUES (@GrossWeight, @NetWeight, @Lenght, @Width, @Height, @CBM, @PackingListId, @Type, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                packingListPackage
            )
            .Single();
    }

    public void Add(IEnumerable<PackingListPackage> packingListPackages) {
        _connection.Execute(
            "INSERT INTO [PackingListPackage] (GrossWeight, NetWeight, Lenght, Width, Height, CBM, PackingListId, Type, Updated) " +
            "VALUES (@GrossWeight, @NetWeight, @Lenght, @Width, @Height, @CBM, @PackingListId, @Type, getutcdate())",
            packingListPackages
        );
    }

    public void Update(PackingListPackage packingListPackage) {
        _connection.Execute(
            "UPDATE [PackingListPackage] " +
            "SET GrossWeight = @GrossWeight, NetWeight = @NetWeight, Lenght = @Lenght, Width = @Width, Height = @Height, CBM = @CBM, " +
            "PackingListId = @PackingListId, Type = @Type, Updated = getutcdate() " +
            "WHERE [PackingListPackage].NetUID = @NetUid",
            packingListPackage
        );
    }

    public void Update(IEnumerable<PackingListPackage> packingListPackages) {
        _connection.Execute(
            "UPDATE [PackingListPackage] " +
            "SET GrossWeight = @GrossWeight, NetWeight = @NetWeight, Lenght = @Lenght, Width = @Width, Height = @Height, CBM = @CBM, " +
            "PackingListId = @PackingListId, Type = @Type, Updated = getutcdate() " +
            "WHERE [PackingListPackage].NetUID = @NetUid",
            packingListPackages
        );
    }

    public void RemoveAllByPackingListIdExceptProvided(long packingListId, IEnumerable<long> boxIds, IEnumerable<long> palletIds) {
        _connection.Execute(
            "UPDATE [PackingListPackage] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PackingListPackage].PackingListId = @PackingListId " +
            "AND [PackingListPackage].ID NOT IN @BoxIds " +
            "AND [PackingListPackage].ID NOT IN @PalletIds",
            new { PackingListId = packingListId, BoxIds = boxIds, PalletIds = palletIds }
        );
    }

    public void UpdateRemainingQty(PackingListPackageOrderItem item) {
        _connection.Execute(
            "UPDATE [PackingListPackageOrderItem] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [RemainingQty] = @RemainingQty " +
            "WHERE [PackingListPackageOrderItem].[ID] = @Id ",
            item);
    }
}