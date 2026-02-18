using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IAdvancePaymentRepository {
    long Add(AdvancePayment advancePayment);

    void Update(AdvancePayment advancePayment);

    AdvancePayment GetLastRecord();

    AdvancePayment GetById(long id);

    AdvancePayment GetByNetId(Guid netId);

    IEnumerable<AdvancePayment> GetAllFiltered(
        DateTime fromDate,
        DateTime toDate);
}