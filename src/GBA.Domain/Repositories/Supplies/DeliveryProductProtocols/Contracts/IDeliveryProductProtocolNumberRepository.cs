using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;

namespace GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;

public interface IDeliveryProductProtocolNumberRepository {
    DeliveryProductProtocolNumber GetLastNumber(string defaultComment);

    long Add(DeliveryProductProtocolNumber number);
}