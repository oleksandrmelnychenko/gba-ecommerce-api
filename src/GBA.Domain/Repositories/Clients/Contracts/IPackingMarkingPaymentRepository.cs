using System.Collections.Generic;
using GBA.Domain.Entities.Clients.PackingMarkings;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IPackingMarkingPaymentRepository {
    List<PackingMarkingPayment> GetAll();
}