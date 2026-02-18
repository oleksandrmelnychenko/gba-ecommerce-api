using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers.PrintingDocuments;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Messages.Supplies.Ukraine.Carriers;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class CarrierStathamActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public CarrierStathamActor(
        IDbConnectionFactory connectionFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager) {
        _connectionFactory = connectionFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;

        Receive<AddNewStathamMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ICarrierStathamRepository carrierStathamRepository = _supplyUkraineRepositoriesFactory.NewCarrierStathamRepository(connection);

            message.Statham.Id = carrierStathamRepository.Add(message.Statham);

            _supplyUkraineRepositoriesFactory
                .NewStathamCarRepository(connection)
                .Add(
                    message
                        .Statham
                        .StathamCars
                        .Where(c => !c.Deleted)
                        .Select(car => {
                            car.StathamId = message.Statham.Id;

                            return car;
                        })
                );

            _supplyUkraineRepositoriesFactory
                .NewStathamPassportRepository(connection)
                .Add(
                    message
                        .Statham
                        .StathamPassports
                        .Where(p => !p.Deleted)
                        .Select(passport => {
                            passport.StathamId = message.Statham.Id;

                            if (passport.PassportIssuedDate.Year.Equals(1)) passport.PassportIssuedDate = DateTime.UtcNow;
                            if (passport.PassportCloseDate.Year.Equals(1)) passport.PassportCloseDate = DateTime.UtcNow;

                            return passport;
                        })
                );

            Sender.Tell(carrierStathamRepository.GetById(message.Statham.Id));
        });

        Receive<UpdateStathamMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IStathamCarRepository stathamCarRepository = _supplyUkraineRepositoriesFactory.NewStathamCarRepository(connection);
            ICarrierStathamRepository carrierStathamRepository = _supplyUkraineRepositoriesFactory.NewCarrierStathamRepository(connection);
            IStathamPassportRepository stathamPassportRepository = _supplyUkraineRepositoriesFactory.NewStathamPassportRepository(connection);

            carrierStathamRepository.Update(message.Statham);

            stathamCarRepository
                .RemoveAllByStathamIdExceptProvided(
                    message.Statham.Id,
                    message
                        .Statham
                        .StathamCars
                        .Where(c => !c.Deleted && !c.IsNew())
                        .Select(c => c.Id)
                );

            stathamCarRepository
                .Add(
                    message
                        .Statham
                        .StathamCars
                        .Where(c => !c.Deleted && c.IsNew())
                        .Select(car => {
                            car.StathamId = message.Statham.Id;

                            return car;
                        })
                );

            stathamCarRepository
                .Update(
                    message
                        .Statham
                        .StathamCars
                        .Where(c => !c.Deleted && !c.IsNew())
                );

            stathamPassportRepository
                .RemoveAllByStathamIdExceptProvided(
                    message.Statham.Id,
                    message
                        .Statham
                        .StathamPassports
                        .Where(p => !p.IsNew())
                        .Select(p => p.Id)
                );

            stathamPassportRepository
                .Add(
                    message
                        .Statham
                        .StathamPassports
                        .Where(p => p.IsNew())
                        .Select(passport => {
                            passport.StathamId = message.Statham.Id;

                            if (passport.PassportIssuedDate.Year.Equals(1)) passport.PassportIssuedDate = DateTime.UtcNow;
                            if (passport.PassportCloseDate.Year.Equals(1)) passport.PassportCloseDate = DateTime.UtcNow;

                            return passport;
                        })
                );

            stathamPassportRepository
                .Update(
                    message
                        .Statham
                        .StathamPassports
                        .Where(p => !p.IsNew())
                        .Select(passport => {
                            passport.StathamId = message.Statham.Id;

                            if (passport.PassportIssuedDate.Year.Equals(1)) passport.PassportIssuedDate = DateTime.UtcNow;
                            if (passport.PassportCloseDate.Year.Equals(1)) passport.PassportCloseDate = DateTime.UtcNow;

                            return passport;
                        })
                );

            Sender.Tell(carrierStathamRepository.GetById(message.Statham.Id));
        });

        Receive<GetStathamByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyUkraineRepositoriesFactory.NewCarrierStathamRepository(connection).GetByNetId(message.NetId));
        });

        Receive<GetAllStathamsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyUkraineRepositoriesFactory.NewCarrierStathamRepository(connection).GetAll());
        });

        Receive<GetAllStathamsFromSearchMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(_supplyUkraineRepositoriesFactory.NewCarrierStathamRepository(connection).GetAllFromSearch(message.Value));
        });

        Receive<DeleteStathamByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _supplyUkraineRepositoriesFactory.NewCarrierStathamRepository(connection).RemoveByNetId(message.NetId);
        });

        Receive<GetPrintingDocumentsStathamCarriersMessage>(ProcessGetPrintingDocumentsStathamCarriers);
    }

    private void ProcessGetPrintingDocumentsStathamCarriers(GetPrintingDocumentsStathamCarriersMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            object forPrint =
                _supplyUkraineRepositoriesFactory
                    .NewCarrierStathamRepository(connection)
                    .GetAll();

            if (forPrint == null) {
                Sender.Tell((string.Empty, string.Empty));
                return;
            }

            PrintDocumentsHelper printDocumentsHelper = new(forPrint, message.DataForPrintingDocument);

            List<Dictionary<string, string>> rows = printDocumentsHelper.GetRowsForPrintDocument();

            (string pathXls, string pathPdf) =
                _xlsFactoryManager
                    .NewPrintDocumentsManager()
                    .GetPrintDocument(
                        message.PathToFolder,
                        message.DataForPrintingDocument,
                        rows);

            Sender.Tell((pathXls, pathPdf));
        } catch {
            Sender.Tell((string.Empty, string.Empty));
        }
    }
}