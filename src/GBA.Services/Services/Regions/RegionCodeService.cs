using System;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Repositories.Regions.Contracts;
using GBA.Services.Services.Regions.Contracts;

namespace GBA.Services.Services.Regions;

public sealed class RegionCodeService : IRegionCodeService {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;

    public RegionCodeService(
        IRegionRepositoriesFactory regionRepositoriesFactory,
        IDbConnectionFactory connectionFactory) {
        _regionRepositoriesFactory = regionRepositoriesFactory;

        _connectionFactory = connectionFactory;
    }

    public Task<RegionCode> GetAvailableRegionCode(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Region region = _regionRepositoriesFactory.NewRegionRepository(connection).GetByNetId(netId);

            if (region == null) throw new Exception("Region with provided NetId does not exists");

            IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);

            RegionCode lastRegionCode = regionCodeRepository.GetLastRecordByRegionName(region.Name);

            RegionCode regionCode = null;

            if (lastRegionCode != null) {
                if (lastRegionCode.Value.Length >= 7) {
                    regionCode = new RegionCode {
                        RegionId = region.Id,
                        Value = $"{region.Name}{string.Format("{0:D5}", Convert.ToInt32(lastRegionCode.Value.Substring(lastRegionCode.Region.Name.Length, 5)) + 1)}"
                    };
                } else {
                    if (lastRegionCode.Value.StartsWith(lastRegionCode.Region.Name))
                        regionCode = new RegionCode {
                            RegionId = region.Id,
                            Value =
                                $"{region.Name}{string.Format("{0:D5}", Convert.ToInt32(lastRegionCode.Value.Substring(lastRegionCode.Region.Name.Length, lastRegionCode.Value.Length - lastRegionCode.Region.Name.Length)) + 1)}"
                        };
                    else
                        regionCode = new RegionCode {
                            RegionId = region.Id,
                            Value = $"{region.Name}{string.Format("{0:D5}", Convert.ToInt32(lastRegionCode.Value) + 1)}"
                        };
                }
            } else {
                regionCode = new RegionCode {
                    RegionId = region.Id,
                    Value = $"{region.Name}{string.Format("{0:D5}", 1)}"
                };
            }

            regionCode.Id = regionCodeRepository.Add(regionCode);

            return Task.FromResult(regionCodeRepository.GetById(regionCode.Id));
    }
}
