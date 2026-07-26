using System;

namespace GBA.Services.Services.Clients.Contracts;

public interface IClientResourceAccessService {
    bool CanAccessClient(Guid actorNetId, Guid clientNetId);

    bool CanAccessClientOrAgreement(Guid actorNetId, Guid resourceNetId);

    bool CanAccessDeliveryRecipient(Guid actorNetId, Guid recipientNetId);

    bool CanAccessDeliveryRecipientAddress(Guid actorNetId, Guid addressNetId);

    bool CanAccessSale(Guid actorNetId, Guid saleNetId);
}
