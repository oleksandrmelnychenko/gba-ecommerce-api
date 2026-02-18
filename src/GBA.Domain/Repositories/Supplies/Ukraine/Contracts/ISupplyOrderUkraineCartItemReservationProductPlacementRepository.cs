using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyOrderUkraineCartItemReservationProductPlacementRepository {
    void Add(SupplyOrderUkraineCartItemReservationProductPlacement placement);

    void Add(IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> placements);

    void Update(SupplyOrderUkraineCartItemReservationProductPlacement placement);

    IEnumerable<SupplyOrderUkraineCartItemReservationProductPlacement> GetAllByReservationId(long reservationId);
}