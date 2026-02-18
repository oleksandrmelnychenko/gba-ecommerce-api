using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.VatRates;
using GBA.Domain.Messages.VatRates;
using GBA.Domain.Repositories.VatRates.Contracts;

namespace GBA.Services.Actors.Vats;

public sealed class VatRatesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IVatRateRepositoriesFactory _vatRateRepositoriesFactory;

    public VatRatesActor(
        IDbConnectionFactory connectionFactory,
        IVatRateRepositoriesFactory vatRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _vatRateRepositoriesFactory = vatRateRepositoriesFactory;

        Receive<GetAllVatRatesMessage>(ProcessGetAllVatRates);

        Receive<AddNewVatRateMessage>(ProcessAddNewVatRate);

        Receive<UpdateVatRateMessage>(ProcessUpdateVatRate);

        Receive<RemoveVatRateMessage>(ProcessRemoveVatRate);
    }

    private void ProcessGetAllVatRates(GetAllVatRatesMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _vatRateRepositoriesFactory
                    .NewVatRateRepository(connection)
                    .GetAll());
        } catch {
            Sender.Tell(new List<VatRate>());
        }
    }

    private void ProcessAddNewVatRate(AddNewVatRateMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IVatRateRepository vatRateRepository =
                _vatRateRepositoriesFactory.NewVatRateRepository(connection);

            vatRateRepository.New(message.VatRate);

            Sender.Tell(vatRateRepository.GetAll());
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateVatRate(UpdateVatRateMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IVatRateRepository vatRateRepository =
                _vatRateRepositoriesFactory.NewVatRateRepository(connection);

            vatRateRepository.Update(message.VatRate);

            Sender.Tell(vatRateRepository.GetAll());
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveVatRate(RemoveVatRateMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IVatRateRepository vatRateRepository =
                _vatRateRepositoriesFactory.NewVatRateRepository(connection);

            vatRateRepository.Remove(message.Id);

            Sender.Tell(vatRateRepository.GetAll());
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}