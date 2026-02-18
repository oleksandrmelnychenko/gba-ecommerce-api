using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Domain.Repositories.Supplies.Ukraine;

public sealed class SupplyOrderUkraineCartItemReservationRepository : ISupplyOrderUkraineCartItemReservationRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderUkraineCartItemReservationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyOrderUkraineCartItemReservation reservation) {
        return _connection.Query<long>(
            "INSERT INTO [SupplyOrderUkraineCartItemReservation] " +
            "(Qty, ProductAvailabilityId, SupplyOrderUkraineCartItemId, ConsignmentItemId, Updated) " +
            "VALUES " +
            "(@Qty, @ProductAvailabilityId, @SupplyOrderUkraineCartItemId, @ConsignmentItemId, GETUTCDATE()); " +
            "SELECT SCOPE_IDENTITY()",
            reservation
        ).Single();
    }

    public void Add(IEnumerable<SupplyOrderUkraineCartItemReservation> reservations) {
        _connection.Execute(
            "INSERT INTO [SupplyOrderUkraineCartItemReservation] " +
            "(Qty, ProductAvailabilityId, SupplyOrderUkraineCartItemId, ConsignmentItemId, Updated) " +
            "VALUES " +
            "(@Qty, @ProductAvailabilityId, @SupplyOrderUkraineCartItemId, @ConsignmentItemId, GETUTCDATE())",
            reservations
        );
    }

    public void Update(SupplyOrderUkraineCartItemReservation reservation) {
        _connection.Execute(
            "UPDATE [SupplyOrderUkraineCartItemReservation] " +
            "SET Qty = @Qty, ProductAvailabilityId = @ProductAvailabilityId, ConsignmentItemId = @ConsignmentItemId, " +
            "SupplyOrderUkraineCartItemId = @SupplyOrderUkraineCartItemId, Deleted = @Deleted, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            reservation
        );
    }

    public SupplyOrderUkraineCartItemReservation GetByIdsIfExists(long cartItemId, long availabilityId) {
        return _connection.Query<SupplyOrderUkraineCartItemReservation>(
            "SELECT TOP(1) * " +
            "FROM [SupplyOrderUkraineCartItemReservation] " +
            "WHERE [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemId = @CartItemId " +
            "AND [SupplyOrderUkraineCartItemReservation].ConsignmentItemId IS NULL " +
            "AND [SupplyOrderUkraineCartItemReservation].ProductAvailabilityId = @AvailabilityId",
            new { CartItemId = cartItemId, AvailabilityId = availabilityId }
        ).SingleOrDefault();
    }

    public SupplyOrderUkraineCartItemReservation GetByIdsIfExists(long cartItemId, long availabilityId, long consignmentId) {
        return _connection.Query<SupplyOrderUkraineCartItemReservation>(
            "SELECT TOP(1) * " +
            "FROM [SupplyOrderUkraineCartItemReservation] " +
            "WHERE [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemId = @CartItemId " +
            "AND [SupplyOrderUkraineCartItemReservation].ConsignmentItemId = @ConsignmentId " +
            "AND [SupplyOrderUkraineCartItemReservation].ProductAvailabilityId = @AvailabilityId",
            new { CartItemId = cartItemId, AvailabilityId = availabilityId, ConsignmentId = consignmentId }
        ).SingleOrDefault();
    }

    public IEnumerable<SupplyOrderUkraineCartItemReservation> GetAllByCartItemId(long cartItemId) {
        return _connection.Query<SupplyOrderUkraineCartItemReservation, ProductAvailability, SupplyOrderUkraineCartItemReservation>(
            "SELECT * " +
            "FROM [SupplyOrderUkraineCartItemReservation] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [SupplyOrderUkraineCartItemReservation].ProductAvailabilityID " +
            "WHERE [SupplyOrderUkraineCartItemReservation].Deleted = 0 " +
            "AND [SupplyOrderUkraineCartItemReservation].SupplyOrderUkraineCartItemId = @CartItemId " +
            "ORDER BY [SupplyOrderUkraineCartItemReservation].ID DESC",
            (reservation, availability) => {
                reservation.ProductAvailability = availability;

                return reservation;
            },
            new { CartItemId = cartItemId }
        );
    }
}