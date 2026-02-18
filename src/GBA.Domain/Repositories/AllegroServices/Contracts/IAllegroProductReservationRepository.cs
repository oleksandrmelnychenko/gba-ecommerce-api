using System;
using System.Collections.Generic;
using GBA.Domain.Entities.AllegroServices;

namespace GBA.Domain.Repositories.AllegroServices.Contracts;

public interface IAllegroProductReservationRepository {
    void Add(AllegroProductReservation reservation);

    List<AllegroProductReservation> GetAllByProductNetId(Guid netId);
}