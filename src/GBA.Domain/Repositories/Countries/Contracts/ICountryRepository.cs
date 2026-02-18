using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Countries.Contracts;

public interface ICountryRepository {
    long Add(Country country);

    void Update(Country country);

    void Remove(Guid netId);

    Country GetById(long id);

    Country GetByNetId(Guid netId);

    List<Country> GetAll();
}