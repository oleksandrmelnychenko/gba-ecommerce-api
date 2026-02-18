using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyInformationDeliveryProtocolKeyRepository {
    long Add(SupplyInformationDeliveryProtocolKey key);

    void Update(SupplyInformationDeliveryProtocolKey key);

    void Update(IEnumerable<SupplyInformationDeliveryProtocolKey> keys);

    List<SupplyInformationDeliveryProtocolKey> GetAll();

    List<SupplyInformationDeliveryProtocolKey> GetAllDefaultByTransportationTypeAndDestination(SupplyTransportationType type, KeyAssignedTo destination);

    List<SupplyInformationDeliveryProtocolKey> GetAllDefaultByDestination(KeyAssignedTo destination);
}