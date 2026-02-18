using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface ICompanyCarRepository {
    long Add(CompanyCar companyCar);

    void Update(CompanyCar companyCar);

    void UpdateFuelAmountByCarId(long id, double toAddFuelAmount);

    CompanyCar GetById(long id);

    CompanyCar GetByNetId(Guid netId);

    IEnumerable<CompanyCar> GetAll();

    IEnumerable<CompanyCar> GetAllFromSearch(string value);

    void Remove(Guid netId);
}