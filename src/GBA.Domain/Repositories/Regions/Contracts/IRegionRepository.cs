using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Regions;

namespace GBA.Domain.Repositories.Regions.Contracts;

public interface IRegionRepository {
    long Add(Region region);

    void Update(Region region);

    Region GetById(long id);

    Region GetByNetId(Guid netId);

    Region GetByName(string name);

    Region GetLastRecord();

    List<Region> GetAll();

    List<Region> GetAllWithAllCodes();

    List<dynamic> GetTopByRegions();

    bool IsAssignedToRegionCode(long regionId);

    bool IsAssignedToClient(long regionId);

    void Remove(Guid netId);
}