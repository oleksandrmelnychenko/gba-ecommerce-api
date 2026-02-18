using System.Data;
using GBA.Domain.Repositories.Consignments.Contracts;

namespace GBA.Domain.Repositories.Consignments;

public sealed class ConsignmentRepositoriesFactory : IConsignmentRepositoriesFactory {
    public IConsignmentRepository NewConsignmentRepository(IDbConnection connection) {
        return new ConsignmentRepository(connection);
    }

    public IConsignmentItemRepository NewConsignmentItemRepository(IDbConnection connection) {
        return new ConsignmentItemRepository(connection);
    }

    public IConsignmentItemMovementRepository NewConsignmentItemMovementRepository(IDbConnection connection) {
        return new ConsignmentItemMovementRepository(connection);
    }

    public IRemainingConsignmentRepository NewRemainingConsignmentRepository(IDbConnection connection) {
        return new RemainingConsignmentRepository(connection);
    }

    public IConsignmentInfoRepository NewConsignmentInfoRepository(IDbConnection connection) {
        return new ConsignmentInfoRepository(connection);
    }
}