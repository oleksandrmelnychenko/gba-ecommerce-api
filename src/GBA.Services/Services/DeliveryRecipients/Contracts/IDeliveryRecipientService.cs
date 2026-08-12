using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Delivery;

namespace GBA.Services.Services.DeliveryRecipients.Contracts;

public interface IDeliveryRecipientService {
    Task<List<DeliveryRecipient>> GetAllRecipientsByClientNetId(Guid netId);

    Task<DeliveryRecipient> AddRecipient(Guid actorNetId, string fullName, string mobilePhone);

    Task<DeliveryRecipientAddress> AddAddress(
        Guid actorNetId,
        Guid recipientNetId,
        string value,
        string city,
        string department);

    Task<List<DeliveryRecipientAddress>> GetAllAddressesByRecipientNetId(Guid netId);
}
