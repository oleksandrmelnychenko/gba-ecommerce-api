using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.TaxInspections;
using GBA.Domain.Repositories.TaxInspections.Contracts;

namespace GBA.Services.Actors.TaxInspections;

public sealed class TaxInspectionsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITaxInspectionRepositoryFactory _taxInspectionRepositoryFactory;

    public TaxInspectionsActor(
        IDbConnectionFactory connectionFactory,
        ITaxInspectionRepositoryFactory taxInspectionRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _taxInspectionRepositoryFactory = taxInspectionRepositoryFactory;

        Receive<AddNewTaxInspectionMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITaxInspectionRepository taxInspectionRepository = _taxInspectionRepositoryFactory.New(connection);

            message.TaxInspection.Id = taxInspectionRepository.Add(message.TaxInspection);

            Sender.Tell(
                taxInspectionRepository
                    .GetById(
                        message.TaxInspection.Id
                    )
            );
        });

        Receive<UpdateTaxInspectionMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITaxInspectionRepository taxInspectionRepository = _taxInspectionRepositoryFactory.New(connection);

            taxInspectionRepository.Update(message.TaxInspection);

            Sender.Tell(
                taxInspectionRepository
                    .GetById(
                        message.TaxInspection.Id
                    )
            );
        });

        Receive<GetTaxInspectionByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _taxInspectionRepositoryFactory
                    .New(connection)
                    .GetByNetId(
                        message.NetId
                    )
            );
        });

        Receive<GetAllTaxInspectionsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _taxInspectionRepositoryFactory
                    .New(connection)
                    .GetAll()
            );
        });

        Receive<DeleteTaxInspectionByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _taxInspectionRepositoryFactory
                .New(connection)
                .Remove(
                    message.NetId
                );
        });
    }
}