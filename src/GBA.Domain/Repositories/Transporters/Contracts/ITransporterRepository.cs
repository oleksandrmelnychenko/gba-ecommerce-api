using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Transporters;

namespace GBA.Domain.Repositories.Transporters.Contracts;

public interface ITransporterRepository {
    long Add(Transporter transporter);

    void Update(Transporter transporter);

    Transporter GetById(long id);

    Transporter GetByNetId(Guid netId);

    List<Transporter> GetAll();

    List<Transporter> GetAllByTransporterTypeNetId(Guid transporterTypeNetId);

    List<Transporter> GetAllByTransporterTypeNetIdDeleted(Guid transporterTypeNetId);

    void IncreasePriority(Guid netId);

    void DecreasePriority(Guid netId);

    void Remove(Guid netId);
}