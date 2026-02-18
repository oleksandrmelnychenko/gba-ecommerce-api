using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemReservationProductPlacementRepository : ISupplyOrderUkraineCartItemReservationProductPlacementRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderUkraineCartItemReservationProductPlacementRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(SupplyOrderUkraineCartItemReservationProductPlacement placement) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderUkraineCartItemReservationProductPlacement] " +
            "(Qty, ProductPlacementID, SupplyOrderUkraineCartItemReservationID, Updated) " +
            "VALUES " +
            "(@Qty, @ProductPlacementId, @SupplyOrderUkraineCartItemReservationId, GETUTCDATE())",
            placement
        );
    }

    public void Add(IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderUkraineCartItemReservationProductPlacement] " +
            "(Qty, ProductPlacementID, SupplyOrderUkraineCartItemReservationID, Updated) " +
            "VALUES " +
            "(@Qty, @ProductPlacementId, @SupplyOrderUkraineCartItemReservationId, GETUTCDATE())",
            placements
        );
    }

    public void Update(SupplyOrderUkraineCartItemReservationProductPlacement placement) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItemReservationProductPlacement] " +
            "SET Qty = @Qty, Deleted = @Deleted, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            placement
        );
    }

    public IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> GetAllByReservationId(long reservationId) {
        Type[] types = {
            typeof(SupplyOrderUkraineCartItemReservationProductPlacement),
            typeof(ProductPlacement)
        };

        Func<object[], SupplyOrderUkraineCartItemReservationProductPlacement> mapper = objects => {
            SupplyOrderUkraineCartItemReservationProductPlacement reservationProductPlacement = (SupplyOrderUkraineCartItemReservationProductPlacement)objects[0];
            ProductPlacement productPlacement = (ProductPlacement)objects[1];

            reservationProductPlacement.ProductPlacement = productPlacement;

            return reservationProductPlacement;
        };

        return _connection.Query(
            "SELECT * " +
            "FROM [SupplyOrderUkraineCartItemReservationProductPlacement] " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [SupplyOrderUkraineCartItemReservationProductPlacement].ProductPlacementID = [ProductPlacement].ID " +
            "WHERE [SupplyOrderUkraineCartItemReservationProductPlacement].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItemReservationProductPlacement].SupplyOrderUkraineCartItemReservationID = @ReservationId",
            types,
            mapper,
            new { ReservationId = reservationId }
        );
    }
}