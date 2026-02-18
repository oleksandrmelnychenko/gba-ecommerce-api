using System.Data;

namespace GBA.Domain.Repositories.Delivery.Contracts;

public interface IDeliveryRepositoriesFactory {
    ITermsOfDeliveryRepository NewTermsOfDeliveryRepository(IDbConnection connection);

    IDeliveryRecipientAddressRepository NewDeliveryRecipientAddressRepository(IDbConnection connection);

    IDeliveryRecipientRepository NewDeliveryRecipientRepository(IDbConnection connection);
}