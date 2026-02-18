using GBA.Domain.Entities.Consignments;

namespace GBA.Domain.Repositories.Consignments.Contracts;

public interface IConsignmentRepository {
    long Add(Consignment consignment);

    Consignment GetIfExistsByConsignmentParams(Consignment consignment);
}