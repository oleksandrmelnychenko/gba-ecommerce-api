using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentRegisterTransferRepository {
    long Add(PaymentRegisterTransfer paymentRegisterTransfer);

    void Update(PaymentRegisterTransfer paymentRegisterTransfer);

    PaymentRegisterTransfer GetById(long id);

    PaymentRegisterTransfer GetByNetId(Guid netId);

    PaymentRegisterTransfer GetLastRecord();

    void SetCanceled(Guid netId);

    List<PaymentRegisterTransfer> GetAllFiltered(DateTime from, DateTime to, Guid? paymentRegisterNetId, Guid? currencyNetId);

    List<PaymentRegisterTransfer> GetAllOutcomingByPaymentRegisterNetId(Guid paymentRegisterNetId, DateTime from, DateTime to, Guid? currencyNetId);

    List<PaymentRegisterTransfer> GetAllIncomingByPaymentRegisterNetId(Guid paymentRegisterNetId, DateTime from, DateTime to, Guid? currencyNetId);
}