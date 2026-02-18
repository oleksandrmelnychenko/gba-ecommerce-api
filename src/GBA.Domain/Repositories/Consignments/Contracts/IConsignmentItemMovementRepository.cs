using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;

namespace GBA.Domain.Repositories.Consignments.Contracts;

public interface IConsignmentItemMovementRepository {
    void Add(ConsignmentItemMovement movement);

    void Add(IEnumerable<ConsignmentItemMovement> movements);

    void UpdateRemainingQty(ConsignmentItemMovement movement);
}