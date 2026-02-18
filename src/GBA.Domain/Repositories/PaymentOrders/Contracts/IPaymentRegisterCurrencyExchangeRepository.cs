using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentRegisterCurrencyExchangeRepository {
    long Add(PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange);

    void Update(PaymentRegisterCurrencyExchange paymentRegisterCurrencyExchange);

    void SetCanceled(Guid netId);

    PaymentRegisterCurrencyExchange GetById(long id);

    PaymentRegisterCurrencyExchange GetByNetId(Guid netId);

    PaymentRegisterCurrencyExchange GetLastRecord();

    List<PaymentRegisterCurrencyExchange> GetAllByPaymentRegisterNetId(DateTime from, DateTime to, Guid? paymentRegisterNetId, Guid? fromCurrencyNetId, Guid? toCurrencyNetId);
}