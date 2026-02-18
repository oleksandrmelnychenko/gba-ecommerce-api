using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supplies.Ukraine.ActReconciliations;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class ActReconciliationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;

    public ActReconciliationsActor(
        IDbConnectionFactory connectionFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<GetAllActReconciliationsMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewActReconciliationRepository(connection)
                    .GetAll()
            );
        });

        Receive<GetActReconciliationByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewActReconciliationRepository(connection)
                    .GetByNetId(
                        message.NetId
                    )
            );
        });

        Receive<GetAllActReconciliationsFilteredMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewActReconciliationRepository(connection)
                    .GetAllFiltered(
                        message.From,
                        message.To
                    )
            );
        });

        Receive<GetAllAppliedActionsByActReconciliationNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .ActReconciliationAppliedActionsRepository(connection)
                    .GetAllAppliedActionsByActReconciliationNetId(
                        message.NetId
                    )
            );
        });
    }
}