using System.Collections.Generic;
using GBA.Domain.Entities.Regions;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IRegionsSyncRepository {
    IEnumerable<string> GetAllRegionsForSync();

    IEnumerable<string> GetAllAmgRegionsForSync();

    IEnumerable<Region> GetAllExistingRegions();

    void Add(Region region);

    void Update(Region region);

    void RemoveAllExistingRegions();
}