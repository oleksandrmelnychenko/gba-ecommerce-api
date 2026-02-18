using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Messages.Regions;
using GBA.Domain.Repositories.Regions.Contracts;

namespace GBA.Services.Actors.Regions;

public sealed class RegionsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRegionRepositoriesFactory _regionRepositoriesFactory;

    public RegionsActor(
        IDbConnectionFactory connectionFactory,
        IRegionRepositoriesFactory regionRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _regionRepositoriesFactory = regionRepositoriesFactory;

        Receive<AddRegionMessage>(ProcessAddRegionMessage);

        Receive<UpdateRegionMessage>(ProcessUpdateRegionMessage);

        Receive<GetAllRegionsWithAllCodesMessage>(ProcessGetAllRegionsWithAllCodesMessage);

        Receive<GetAllRegionsMessage>(ProcessGetAllRegionsMessage);

        Receive<GetRegionByNetIdMessage>(ProcessGetRegionByNetIdMessage);

        Receive<DeleteRegionMessage>(ProcessDeleteRegionMessage);

        Receive<GetTopByRegionsMessage>(ProcessGetTopByRegionsMessage);
    }

    private void ProcessAddRegionMessage(AddRegionMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRegionRepository regionRepository = _regionRepositoriesFactory.NewRegionRepository(connection);

        Sender.Tell(regionRepository.GetById(regionRepository.Add(message.Region)));
    }

    private void ProcessUpdateRegionMessage(UpdateRegionMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRegionRepository regionRepository = _regionRepositoriesFactory.NewRegionRepository(connection);

        regionRepository.Update(message.Region);

        Sender.Tell(regionRepository.GetByNetId(message.Region.NetUid));
    }

    private void ProcessGetAllRegionsWithAllCodesMessage(GetAllRegionsWithAllCodesMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_regionRepositoriesFactory.NewRegionRepository(connection).GetAllWithAllCodes());
    }

    private void ProcessGetAllRegionsMessage(GetAllRegionsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_regionRepositoriesFactory.NewRegionRepository(connection).GetAll());
    }

    private void ProcessGetRegionByNetIdMessage(GetRegionByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_regionRepositoriesFactory.NewRegionRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessDeleteRegionMessage(DeleteRegionMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IRegionRepository regionRepository = _regionRepositoriesFactory.NewRegionRepository(connection);

        Region region = regionRepository.GetByNetId(message.NetId);

        if (region != null) {
            if (regionRepository.IsAssignedToClient(region.Id)) {
                Sender.Tell(new Tuple<string, Region>(RegionResourceNames.ASSIGNED_TO_CLIENT, null));
            } else if (regionRepository.IsAssignedToRegionCode(region.Id)) {
                Sender.Tell(new Tuple<string, Region>(RegionResourceNames.ASSIGNED_TO_REGION_CODE, null));
            } else {
                regionRepository.Remove(message.NetId);

                Sender.Tell(new Tuple<string, Region>(string.Empty, region));
            }
        } else {
            Sender.Tell(new Tuple<string, Region>(string.Empty, null));
        }
    }

    private void ProcessGetTopByRegionsMessage(GetTopByRegionsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<dynamic> topResult = _regionRepositoriesFactory.NewRegionRepository(connection).GetTopByRegions();

        dynamic[] toReturnData = new dynamic[topResult.Count];

        for (int i = 0; i < topResult.Count; i++) {
            dynamic result = new ExpandoObject();

            result.RegionName = topResult[i].RegionName;
            result.TotalSalesCount = topResult[i].TotalSalesCount;
            result.TotalAmount = topResult[i].TotalAmount;
            result.Color = ChartColors.COLORS[i];

            toReturnData[i] = result;
        }

        Sender.Tell(toReturnData);
    }
}