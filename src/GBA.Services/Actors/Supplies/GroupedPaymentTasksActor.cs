using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supplies.GroupedPaymentTasks;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies;

public sealed class GroupedPaymentTasksActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public GroupedPaymentTasksActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<GetAllGroupedPaymentTasksMessage>(ProcessGetAllGroupedPaymentTasksMessage);

        Receive<GetAllFutureGroupedPaymentTasksMessage>(ProcessGetAllFutureGroupedPaymentTasksMessage);

        Receive<GetAllPastGroupedPaymentTasksMessage>(ProcessGetAllPastGroupedPaymentTasksMessage);

        Receive<GetAllGroupedPaymentTasksFilteredMessage>(ProcessGetAllGroupedPaymentTasksFilteredMessage);

        Receive<GetAllAvailableForPaymentGroupedPaymentTasksFilteredMessage>(ProcessGetAllAvailableForPaymentGroupedPaymentTasksFilteredMessage);
    }

    private void ProcessGetAllGroupedPaymentTasksMessage(GetAllGroupedPaymentTasksMessage message) {
        if (message.Limit <= 0) message.Limit = 10;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection currencyExchangeConnection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewGroupedPaymentTasksRepository(connection, currencyExchangeConnection)
            .GetGroupedPaymentTasksForCurrentDate(message.Limit));
    }

    private void ProcessGetAllFutureGroupedPaymentTasksMessage(GetAllFutureGroupedPaymentTasksMessage message) {
        if (message.Limit <= 0) message.Limit = 10;

        if (message.FromDate.Year.Equals(1)) message.FromDate = DateTime.Now;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection currencyExchangeConnection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewGroupedPaymentTasksRepository(connection, currencyExchangeConnection)
            .GetGroupedPaymentTasksForFutureFromDate(message.Limit, message.FromDate));
    }

    private void ProcessGetAllPastGroupedPaymentTasksMessage(GetAllPastGroupedPaymentTasksMessage message) {
        if (message.Limit <= 0) message.Limit = 10;

        if (message.FromDate.Year.Equals(1)) message.FromDate = DateTime.Now;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection currencyExchangeConnection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewGroupedPaymentTasksRepository(connection, currencyExchangeConnection)
            .GetGroupedPaymentTasksForPastFromDate(message.Limit, message.FromDate));
    }

    private void ProcessGetAllGroupedPaymentTasksFilteredMessage(GetAllGroupedPaymentTasksFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection currencyExchangeConnection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewGroupedPaymentTasksRepository(connection, currencyExchangeConnection)
                .GetGroupedPaymentTasksFiltered(
                    message.From,
                    message.To,
                    message.OrganizationNetId,
                    message.Limit,
                    message.Offset,
                    message.TypePaymentTask
                )
        );
    }

    private void ProcessGetAllAvailableForPaymentGroupedPaymentTasksFilteredMessage(GetAllAvailableForPaymentGroupedPaymentTasksFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection currencyExchangeConnection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _supplyRepositoriesFactory
                .NewGroupedPaymentTasksRepository(connection, currencyExchangeConnection)
                .GetAvailableForPaymentGroupedPaymentTasksFiltered(
                    message.From,
                    message.To,
                    message.OrganizationNetId,
                    message.Limit,
                    message.Offset
                )
        );
    }
}