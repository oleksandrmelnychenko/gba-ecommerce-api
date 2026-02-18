using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentCostMovementRepository {
    long Add(PaymentCostMovement paymentCostMovement);

    void Update(PaymentCostMovement paymentCostMovement);

    PaymentCostMovement GetById(long id);

    PaymentCostMovement GetByNetId(Guid netId);

    IEnumerable<PaymentCostMovement> GetAllFromSearch(string value);

    IEnumerable<PaymentCostMovement> GetAll();

    void Remove(Guid netId);
}