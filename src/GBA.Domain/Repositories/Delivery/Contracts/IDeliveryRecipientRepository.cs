using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Delivery;

namespace GBA.Domain.Repositories.Delivery.Contracts;

public interface IDeliveryRecipientRepository {
    long Add(DeliveryRecipient deliveryRecipient);

    void Update(DeliveryRecipient deliveryRecipient);

    DeliveryRecipient GetById(long id);

    DeliveryRecipient GetByNetId(Guid netId);

    List<DeliveryRecipient> GetAll();

    List<DeliveryRecipient> GetAllRecipientsByClientNetId(Guid clientNetId);
    List<DeliveryRecipient> GetAllRecipientsDeletedByClientNetId(Guid clientNetId);
    void IncreasePriority(long id);

    void DecreasePriority(long id);

    void Remove(Guid netId);
    void ReturnRemove(Guid netId);
}