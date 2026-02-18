using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Measures;
using GBA.Domain.Repositories.Measures;
using GBA.Domain.Repositories.Measures.Contracts;

namespace GBA.Services.Actors.Measures;

public sealed class MeasureUnitsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMeasureRepositoriesFactory _measureRepositoriesFactory;

    public MeasureUnitsActor(
        IDbConnectionFactory connectionFactory,
        IMeasureRepositoriesFactory measureRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _measureRepositoriesFactory = measureRepositoriesFactory;

        Receive<AddMeasureUnitMessage>(ProcessAddMeasureUnitMessage);

        Receive<GetAllMeasureUnitsMessage>(ProcessGetAllMeasureUnitsMessage);

        Receive<GetAllFromSearchMessage>(ProcessGetAllFromSearchMessage);

        Receive<GetMeasureUnitByNetIdMessage>(ProcessGetMeasureUnitByNetIdMessage);

        Receive<UpdateMeasureUnitMessage>(ProcessUpdateMeasureUnitMessage);

        Receive<DeleteMeasureUnitMessage>(ProcessDeleteMeasureUnitMessage);
    }

    private void ProcessAddMeasureUnitMessage(AddMeasureUnitMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IMeasureUnitRepository measureUnitRepository = _measureRepositoriesFactory.NewMeasureUnitRepository(connection);

        measureUnitRepository.Add(message.MeasureUnit);

        Sender.Tell(measureUnitRepository.GetAll());
    }

    private void ProcessGetAllMeasureUnitsMessage(GetAllMeasureUnitsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_measureRepositoriesFactory.NewMeasureUnitRepository(connection).GetAll());
    }

    private void ProcessGetAllFromSearchMessage(GetAllFromSearchMessage message) {
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_measureRepositoriesFactory.NewMeasureUnitRepository(connection).GetAllFromSearch(message.Value));
    }

    private void ProcessGetMeasureUnitByNetIdMessage(GetMeasureUnitByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_measureRepositoriesFactory.NewMeasureUnitRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessUpdateMeasureUnitMessage(UpdateMeasureUnitMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IMeasureUnitRepository measureUnitRepository = _measureRepositoriesFactory.NewMeasureUnitRepository(connection);

        measureUnitRepository.Update(message.MeasureUnit);

        Sender.Tell(measureUnitRepository.GetAll());
    }

    private void ProcessDeleteMeasureUnitMessage(DeleteMeasureUnitMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        MeasureUnitRepository measureUnitRepository = _measureRepositoriesFactory.NewMeasureUnitRepository(connection);
        measureUnitRepository.Remove(message.NetId);

        Sender.Tell(measureUnitRepository.GetAll());
    }
}