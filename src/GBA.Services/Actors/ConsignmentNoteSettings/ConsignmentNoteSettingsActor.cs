using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.ConsignmentNoteSettings;
using GBA.Domain.Repositories.ConsignmentNoteSettings.Contracts;

namespace GBA.Services.Actors.ConsignmentNoteSettings;

public sealed class ConsignmentNoteSettingsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IConsignmentNoteSettingRepositoriesFactory _consignmentNoteSettingRepositoriesFactory;

    public ConsignmentNoteSettingsActor(
        IDbConnectionFactory connectionFactory,
        IConsignmentNoteSettingRepositoriesFactory consignmentNoteSettingRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _consignmentNoteSettingRepositoriesFactory = consignmentNoteSettingRepositoriesFactory;

        Receive<AddConsignmentNoteSettingMessage>(ProcessAddConsignmentNoteSetting);

        Receive<UpdateConsignmentNoteSettingMessage>(ProcessUpdateConsignmentNoteSetting);

        Receive<RemoveConsignmentNoteSettingMessage>(ProcessRemoveConsignmentNoteSetting);
    }

    private void ProcessAddConsignmentNoteSetting(AddConsignmentNoteSettingMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsignmentNoteSettingRepository consignmentNoteSettingRepository =
                _consignmentNoteSettingRepositoriesFactory.NewConsignmentNoteSettingRepository(connection);

            message.ConsignmentNoteSetting.ForReSale = message.ForReSale;

            consignmentNoteSettingRepository.Add(message.ConsignmentNoteSetting);

            Sender.Tell(
                consignmentNoteSettingRepository
                    .GetAll(message.ForReSale)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateConsignmentNoteSetting(UpdateConsignmentNoteSettingMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsignmentNoteSettingRepository consignmentNoteSettingRepository =
                _consignmentNoteSettingRepositoriesFactory.NewConsignmentNoteSettingRepository(connection);

            consignmentNoteSettingRepository.Update(message.ConsignmentNoteSetting);

            Sender.Tell(
                consignmentNoteSettingRepository
                    .GetAll(message.ForReSale)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveConsignmentNoteSetting(RemoveConsignmentNoteSettingMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IConsignmentNoteSettingRepository consignmentNoteSettingRepository =
                _consignmentNoteSettingRepositoriesFactory.NewConsignmentNoteSettingRepository(connection);

            consignmentNoteSettingRepository.Remove(message.NetId);

            Sender.Tell(
                consignmentNoteSettingRepository
                    .GetAll(message.ForReSale)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}