using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.ServicePayers.Contracts;

public interface IServicePayerRepository {
    long Add(ServicePayer servicePayer);

    void Add(IEnumerable<ServicePayer> servicePayers);

    void Update(ServicePayer servicePayer);

    void Update(IEnumerable<ServicePayer> servicePayers);

    List<ServicePayer> GetAllByClientId(long id);

    void Remove(Guid netId);

    void Remove(IEnumerable<ServicePayer> servicePayers);
}