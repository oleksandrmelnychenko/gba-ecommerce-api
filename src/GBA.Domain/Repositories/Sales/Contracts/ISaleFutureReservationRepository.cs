using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleFutureReservationRepository {
    long Add(AddSaleFutureReservationQuery message);

    SaleFutureReservation GetById(long id);

    List<SaleFutureReservation> GetAll();

    void Delete(Guid netId);
}