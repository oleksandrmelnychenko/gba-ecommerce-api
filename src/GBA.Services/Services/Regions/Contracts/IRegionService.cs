using System.Collections.Generic;
using System.Threading.Tasks;
using GBA.Domain.Entities.Regions;

namespace GBA.Services.Services.Regions.Contracts;

public interface IRegionService {
    Task<List<Region>> GetAllRegions();
}