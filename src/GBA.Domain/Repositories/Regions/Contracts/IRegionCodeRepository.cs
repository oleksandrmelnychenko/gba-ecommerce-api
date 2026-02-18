using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Regions;

namespace GBA.Domain.Repositories.Regions.Contracts;

public interface IRegionCodeRepository {
    long Add(RegionCode regionCode);

    void Update(RegionCode regionCode);
    void UpdateRegionId(RegionCode regionCode);
    RegionCode GetById(long id);

    RegionCode GetByNetId(Guid netId);

    RegionCode GetLastRecordByRegionName(string regionName);

    RegionCode GetAvailableRecordByRegionName(string regionName);

    RegionCode GetRecordByCodeAndValue(string value);

    RegionCode GetLastRecord();

    List<RegionCode> GetAll();

    bool IsAssignedToAnyContact(long regionCodeId);

    bool IsAssignedToAnyContact(string value);

    void Remove(Guid netId);
}