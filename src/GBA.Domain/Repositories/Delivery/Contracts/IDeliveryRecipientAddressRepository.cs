using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Delivery;

namespace GBA.Domain.Repositories.Delivery.Contracts;

public interface IDeliveryRecipientAddressRepository {
    long Add(DeliveryRecipientAddress deliveryRecipientAddress);

    void Update(DeliveryRecipientAddress deliveryRecipientAddress);

    DeliveryRecipientAddress GetById(long id);

    DeliveryRecipientAddress GetByNetId(Guid netId);

    List<DeliveryRecipientAddress> GetAllByRecipientNetId(Guid recipientNetId);

    void IncreasePriority(long id);

    void DecreasePriority(long id);

    void Remove(Guid netId);
}