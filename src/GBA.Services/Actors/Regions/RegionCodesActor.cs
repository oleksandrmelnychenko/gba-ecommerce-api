using System;
using System.Data;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Messages.Regions;
using GBA.Domain.Repositories.Regions.Contracts;

namespace GBA.Services.Actors.Regions;

public sealed class RegionCodesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;

    public RegionCodesActor(
        IDbConnectionFactory connectionFactory,
        IRegionRepositoriesFactory regionRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _regionRepositoriesFactory = regionRepositoriesFactory;

        Receive<ReserveRegionCodeMessage>(ProcessReserveRegionCodeMessage);

        Receive<IsRegionCodeAvailableMessage>(ProcessIsRegionCodeAvailableMessage);

        Receive<DeleteRegionCodeMessage>(ProcessDeleteRegionCodeMessage);

        Receive<UpdateRegionCodeMessage>(ProcessUpdateRegionCodeMessage);

        Receive<AddRegionCodeMessage>(ProcessAddRegionCodeMessage);
    }

    private void ProcessReserveRegionCodeMessage(ReserveRegionCodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);

        if (!string.IsNullOrEmpty(message.Region.Name)) {
            RegionCode lastRegionCode = regionCodeRepository.GetLastRecordByRegionName(message.Region.Name);

            RegionCode regionCode;

            if (lastRegionCode != null) {
                if (!string.IsNullOrEmpty(lastRegionCode.Value) && !lastRegionCode.Value.StartsWith(lastRegionCode.Region.Name))
                    lastRegionCode.Value = lastRegionCode.Value.Substring(1);

                if (lastRegionCode.Value.Length >= 7) {
                    regionCode = new RegionCode {
                        RegionId = message.Region.Id,
                        Value =
                            $"{message.Region.Name}{string.Format("{0:D5}", Convert.ToInt32(lastRegionCode.Value.Substring(lastRegionCode.Region.Name.Length, 5)) + 1)}"
                    };
                } else {
                    if (lastRegionCode.Value.StartsWith(lastRegionCode.Region.Name)) {
                        regionCode = new RegionCode {
                            RegionId = message.Region.Id,
                            Value =
                                $"{message.Region.Name}{string.Format("{0:D5}", Convert.ToInt32(lastRegionCode.Value.Substring(lastRegionCode.Region.Name.Length, lastRegionCode.Value.Length - lastRegionCode.Region.Name.Length)) + 1)}"
                        };
                    } else {
                        if (int.TryParse(lastRegionCode.Value, out int convertedValue))
                            regionCode = new RegionCode {
                                RegionId = message.Region.Id,
                                Value = $"{message.Region.Name}{string.Format("{0:D5}", convertedValue + 1)}"
                            };
                        else
                            regionCode = new RegionCode {
                                RegionId = message.Region.Id,
                                Value = $"{message.Region.Name}{string.Format("{0:D5}", 1)}"
                            };
                    }
                }
            } else {
                regionCode = new RegionCode {
                    RegionId = message.Region.Id,
                    Value = $"{message.Region.Name}{string.Format("{0:D5}", 1)}"
                };
            }

            long codeId = regionCodeRepository.Add(regionCode);

            Sender.Tell(regionCodeRepository.GetById(codeId));
        } else {
            Sender.Tell(null);
        }
    }

    private void ProcessIsRegionCodeAvailableMessage(IsRegionCodeAvailableMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Region region = _regionRepositoriesFactory.NewRegionRepository(connection).GetByNetId(message.NetId);

        if (region != null) {
            IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);

            if (message.Value.Length.Equals(7)) {
                RegionCode regionCode = regionCodeRepository.GetRecordByCodeAndValue(message.Value);

                Sender.Tell(regionCode == null
                    ? new Tuple<bool, string>(true, string.Empty)
                    : new Tuple<bool, string>(!regionCodeRepository.IsAssignedToAnyContact(regionCode.Id), string.Empty));
            } else if (message.Value.Length < 7) {
                try {
                    int numericValue = Convert.ToInt32(message.Value.Substring(region.Name.Length, message.Value.Length - region.Name.Length));

                    message.Value = $"{region.Name}{string.Format("{0:D5}", numericValue)}";

                    RegionCode regionCode = regionCodeRepository.GetRecordByCodeAndValue($"{region.Name}{message.Value}");

                    Sender.Tell(regionCode == null
                        ? new Tuple<bool, string>(true, string.Empty)
                        : new Tuple<bool, string>(!regionCodeRepository.IsAssignedToAnyContact(regionCode.Id), string.Empty));
                } catch (Exception) {
                    Sender.Tell(new Tuple<bool, string>(false, RegionCodeResourceNames.VALUE_INVALID));
                }
            }
        } else {
            Sender.Tell(new Tuple<bool, string>(false, string.Empty));
        }
    }

    private void ProcessDeleteRegionCodeMessage(DeleteRegionCodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);

        RegionCode regionCode = regionCodeRepository.GetByNetId(message.NetId);

        if (regionCode != null) {
            if (!regionCodeRepository.IsAssignedToAnyContact(regionCode.Id)) {
                regionCodeRepository.Remove(message.NetId);

                Sender.Tell(string.Empty);
            } else {
                Sender.Tell(RegionCodeResourceNames.ASSIGNED_TO_CLIENT);
            }
        } else {
            Sender.Tell(string.Empty);
        }

        regionCodeRepository.Remove(message.NetId);
    }

    private void ProcessUpdateRegionCodeMessage(UpdateRegionCodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);

        Region region = _regionRepositoriesFactory.NewRegionRepository(connection).GetById(message.RegionCode.RegionId);

        if (!message.RegionCode.Value.StartsWith(region.Name)) message.RegionCode.Value = $"{region.Name}{message.RegionCode.Value}";

        regionCodeRepository.Update(message.RegionCode);

        Sender.Tell(regionCodeRepository.GetByNetId(message.RegionCode.NetUid));
    }

    private void ProcessAddRegionCodeMessage(AddRegionCodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRegionCodeRepository regionCodeRepository = _regionRepositoriesFactory.NewRegionCodeRepository(connection);

        if (message.RegionCode.RegionId.Equals(0) && message.RegionCode.Region != null && !message.RegionCode.Region.IsNew())
            message.RegionCode.RegionId = message.RegionCode.Region.Id;

        Region region = _regionRepositoriesFactory.NewRegionRepository(connection).GetById(message.RegionCode.RegionId);

        if (!message.RegionCode.Value.StartsWith(region.Name)) message.RegionCode.Value = $"{region.Name}{message.RegionCode.Value}";

        long codeId = regionCodeRepository.Add(message.RegionCode);

        Sender.Tell(regionCodeRepository.GetById(codeId));
    }
}