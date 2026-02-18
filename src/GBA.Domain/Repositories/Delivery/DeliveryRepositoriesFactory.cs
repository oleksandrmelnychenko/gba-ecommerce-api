using System.Data;
using GBA.Domain.Repositories.Delivery.Contracts;

namespace GBA.Domain.Repositories.Delivery;

public sealed class DeliveryRepositoriesFactory : IDeliveryRepositoriesFactory {
    public IDeliveryRecipientAddressRepository NewDeliveryRecipientAddressRepository(IDbConnection connection) {
        return new DeliveryRecipientAddressRepository(connection);
    }

    public IDeliveryRecipientRepository NewDeliveryRecipientRepository(IDbConnection connection) {
        return new DeliveryRecipientRepository(connection);
    }

    public ITermsOfDeliveryRepository NewTermsOfDeliveryRepository(IDbConnection connection) {
        return new TermsOfDeliveryRepository(connection);
    }
}