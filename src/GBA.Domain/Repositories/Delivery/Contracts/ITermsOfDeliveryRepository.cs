using System.Collections.Generic;
using GBA.Domain.Entities.Delivery;

namespace GBA.Domain.Repositories.Delivery.Contracts;

public interface ITermsOfDeliveryRepository {
    List<TermsOfDelivery> GetAll();
}