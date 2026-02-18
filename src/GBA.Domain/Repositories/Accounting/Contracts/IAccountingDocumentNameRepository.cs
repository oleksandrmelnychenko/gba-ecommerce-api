using System.Collections.Generic;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.Repositories.Accounting.Contracts;

public interface IAccountingDocumentNameRepository {
    List<AccountingCashFlowHeadItem> GetDocumentNames(List<AccountingCashFlowHeadItem> documents, TypePaymentTask typePaymentTask = TypePaymentTask.All);

    List<AccountingCashFlowHeadItem> GetDocumentNamesForClients(List<AccountingCashFlowHeadItem> documents);
}