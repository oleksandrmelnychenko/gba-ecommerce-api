using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Transporters;

namespace GBA.Domain.Repositories.Transporters.Contracts;

public interface ITransporterTypeRepository {
    long Add(TransporterType transporterType);

    void Update(TransporterType transporterType);

    TransporterType GetById(long id);

    TransporterType GetByNetId(Guid netId);

    List<TransporterType> GetAll();

    void Remove(Guid netId);
}