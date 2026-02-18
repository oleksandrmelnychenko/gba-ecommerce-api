using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IIncomePaymentOrderSaleRepository {
    void Add(IncomePaymentOrderSale incomePaymentOrderSale);

    void Add(IEnumerable<IncomePaymentOrderSale> incomePaymentOrderSales);

    void UpdateAmount(IncomePaymentOrderSale incomePaymentOrderSale);

    void RemoveAllByIds(IEnumerable<long> ids);

    void Remove(long id);

    bool CheckIsMoreThanOnePaymentBySaleId(long saleId);
}