using System.Data;

namespace GBA.Domain.Repositories.Consignments.Contracts;

public interface IConsignmentRepositoriesFactory {
    IConsignmentRepository NewConsignmentRepository(IDbConnection connection);

    IConsignmentItemRepository NewConsignmentItemRepository(IDbConnection connection);

    IConsignmentItemMovementRepository NewConsignmentItemMovementRepository(IDbConnection connection);

    IRemainingConsignmentRepository NewRemainingConsignmentRepository(IDbConnection connection);

    IConsignmentInfoRepository NewConsignmentInfoRepository(IDbConnection connection);
}