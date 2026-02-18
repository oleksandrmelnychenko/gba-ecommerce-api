using GBA.Domain.Entities.AccountingDocumentNames;
using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.Repositories.Accounting.Contracts;

public interface IAccountingOperationNameRepository {
    AccountingOperationName GetByOperationType(OperationType type);
}