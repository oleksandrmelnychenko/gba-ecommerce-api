using GBA.Common.Helpers;
using GBA.Domain.Entities.Sales.PaymentStatuses;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IBaseSalePaymentStatusRepository {
    long Add(BaseSalePaymentStatus baseSalePaymentStatus);

    void Update(BaseSalePaymentStatus baseSalePaymentStatus);

    void SetSalePaymentStatusTypeById(SalePaymentStatusType type, long id);
}