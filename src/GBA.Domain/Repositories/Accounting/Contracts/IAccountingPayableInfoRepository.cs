using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Domain.Repositories.Accounting.Contracts;

public interface IAccountingPayableInfoRepository {
    AccountingPayableInfo GetAllDebitInfo();

    AccountingPayableInfo GetAllCreditInfo();
}