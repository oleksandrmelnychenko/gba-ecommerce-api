using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ITransportationServiceRepository {
    long Add(TransportationService transportationService);

    void Update(TransportationService transportationService);

    TransportationService GetById(long id);

    TransportationService GetByNetId(Guid netId);

    List<TransportationService> GetAllRanged(DateTime from, DateTime to);

    List<TransportationService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    TransportationService GetByIdWithoutIncludes(long id);
}