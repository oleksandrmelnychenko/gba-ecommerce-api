using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Delivery;

namespace GBA.Services.Services.DeliveryRecipients.Contracts;

public interface IDeliveryRecipientService {
    Task<List<DeliveryRecipient>> GetAllRecipientsByClientNetId(Guid netId);

    Task<List<DeliveryRecipientAddress>> GetAllAddressesByRecipientNetId(Guid netId);
}