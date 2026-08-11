using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Delivery;

namespace GBA.Services.Services.DeliveryRecipients.Contracts;

public interface IDeliveryRecipientService {
    Task<List<DeliveryRecipient>> GetAllRecipientsByClientNetId(Guid netId);

    Task<DeliveryRecipient> AddRecipient(Guid actorNetId, string fullName, string mobilePhone);

    Task<List<DeliveryRecipientAddress>> GetAllAddressesByRecipientNetId(Guid netId);
}
