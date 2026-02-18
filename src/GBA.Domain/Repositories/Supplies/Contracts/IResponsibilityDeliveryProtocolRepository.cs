using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IResponsibilityDeliveryProtocolRepository {
    void Add(IEnumerable<ResponsibilityDeliveryProtocol> responsibilityDeliveryProtocols);

    void Update(IEnumerable<ResponsibilityDeliveryProtocol> responsibilityDeliveryProtocols);
}