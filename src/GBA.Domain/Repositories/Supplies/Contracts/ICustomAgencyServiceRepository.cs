using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ICustomAgencyServiceRepository {
    long Add(CustomAgencyService customAgencyService);

    void Update(CustomAgencyService customAgencyService);

    void Remove(Guid netId);

    CustomAgencyService GetById(long id);

    CustomAgencyService GetByNetId(Guid netId);

    List<CustomAgencyService> GetAll();

    List<CustomAgencyService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    CustomAgencyService GetByIdWithoutIncludes(long id);
}