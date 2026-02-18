using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IIncotermRepository {
    long Add(Incoterm incoterm);

    void Update(Incoterm incoterm);

    void Remove(Guid netId);

    Incoterm GetById(long id);

    Incoterm GetByNetId(Guid netId);

    IEnumerable<Incoterm> GetAll();
}