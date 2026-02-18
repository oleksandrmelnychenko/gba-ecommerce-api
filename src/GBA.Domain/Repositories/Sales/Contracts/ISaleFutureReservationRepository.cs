using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Messages.Sales.Reservations;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleFutureReservationRepository {
    long Add(AddSaleFutureReservationMessage message);

    SaleFutureReservation GetById(long id);

    List<SaleFutureReservation> GetAll();

    void Delete(Guid netId);
}