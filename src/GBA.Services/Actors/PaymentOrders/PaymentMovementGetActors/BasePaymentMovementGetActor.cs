using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.PaymentOrders.PaymentMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.PaymentOrders.PaymentMovementGetActors;

public sealed class BasePaymentMovementGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public BasePaymentMovementGetActor(
        IDbConnectionFactory connectionFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<GetPaymentMovementByNetIdMessage>(ProcessGetPaymentMovementByNetIdMessage);

        Receive<GetAllPaymentMovementsMessage>(ProcessGetAllPaymentMovementsMessage);

        Receive<GetAllPaymentMovementsFromSearchMessage>(ProcessGetAllPaymentMovementsFromSearchMessage);
    }

    private void ProcessGetPaymentMovementByNetIdMessage(GetPaymentMovementByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentMovementRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetAllPaymentMovementsMessage(GetAllPaymentMovementsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentMovementRepository(connection).GetAll());
    }

    private void ProcessGetAllPaymentMovementsFromSearchMessage(GetAllPaymentMovementsFromSearchMessage message) {
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentMovementRepository(connection).GetAllFromSearch(message.Value));
    }
}