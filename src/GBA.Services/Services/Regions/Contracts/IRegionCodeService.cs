using System;
using System.Threading.Tasks;
using GBA.Domain.Entities.Regions;

namespace GBA.Services.Services.Regions.Contracts;

public interface IRegionCodeService {
    Task<RegionCode> GetAvailableRegionCode(Guid netId);
}