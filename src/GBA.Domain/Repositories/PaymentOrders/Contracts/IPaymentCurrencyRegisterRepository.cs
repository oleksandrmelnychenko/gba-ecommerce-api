using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentCurrencyRegisterRepository {
    void Add(IEnumerable<PaymentCurrencyRegister> paymentCurrencyRegisters);

    void UpdateAmount(PaymentCurrencyRegister paymentCurrencyRegister);

    PaymentCurrencyRegister GetByNetId(Guid netId);
    List<PaymentCurrencyRegister> GetAll();

    PaymentCurrencyRegister GetById(long id);

    PaymentCurrencyRegister GetByNetIdFiltered(Guid netId, DateTime from, DateTime to);

    decimal GetPaymentCurrencyRegisterAmountByIdAtSpecifiedDate(DateTime from, long id);

    decimal GetAmountOfAllOperationsAfterDateByIds(DateTime from, long currencyRegisterId, long currencyId, long registerId);
}