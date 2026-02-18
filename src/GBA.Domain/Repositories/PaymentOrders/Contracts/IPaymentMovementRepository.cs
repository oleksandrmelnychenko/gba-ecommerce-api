using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentMovementRepository {
    long Add(PaymentMovement paymentMovement);

    void Update(PaymentMovement paymentMovement);

    PaymentMovement GetById(long id);

    PaymentMovement GetByNetId(Guid netId);

    IEnumerable<PaymentMovement> GetAllFromSearch(string value);

    IEnumerable<PaymentMovement> GetAll();

    void Remove(Guid netId);
}