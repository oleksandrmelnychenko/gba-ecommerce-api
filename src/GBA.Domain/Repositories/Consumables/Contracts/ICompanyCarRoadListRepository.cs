using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface ICompanyCarRoadListRepository {
    long Add(CompanyCarRoadList companyCarRoadList);

    void Update(CompanyCarRoadList companyCarRoadList);

    CompanyCarRoadList GetById(long id);

    CompanyCarRoadList GetByNetId(Guid netId);

    IEnumerable<CompanyCarRoadList> GetAll();

    IEnumerable<CompanyCarRoadList> GetAll(Guid companyCarNetId, DateTime from, DateTime to);

    void Remove(Guid netId);
}