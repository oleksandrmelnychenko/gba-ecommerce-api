using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.ActProvidingServices;

namespace GBA.Domain.Repositories.Supplies.ActProvidingServices.Contracts;

public interface IActProvidingServiceRepository {
    IEnumerable<ActProvidingService> GetAll(DateTime from, DateTime to, int limit, int offset);

    long New(ActProvidingService act);

    void Update(ActProvidingService act);

    void Remove(long id);

    ActProvidingService GetByNetId(Guid netId);

    ActProvidingService GetLastRecord(string defaultComment);
}