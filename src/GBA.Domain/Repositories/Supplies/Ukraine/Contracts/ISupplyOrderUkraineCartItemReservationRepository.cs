using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyOrderUkraineCartItemReservationRepository {
    long Add(SupplyOrderUkraineCartItemReservation reservation);

    void Add(IEnumerable<SupplyOrderUkraineCartItemReservation> reservations);

    void Update(SupplyOrderUkraineCartItemReservation reservation);

    SupplyOrderUkraineCartItemReservation GetByIdsIfExists(long cartItemId, long availabilityId);

    SupplyOrderUkraineCartItemReservation GetByIdsIfExists(long cartItemId, long availabilityId, long consignmentId);

    IEnumerable<SupplyOrderUkraineCartItemReservation> GetAllByCartItemId(long cartItemId);
}