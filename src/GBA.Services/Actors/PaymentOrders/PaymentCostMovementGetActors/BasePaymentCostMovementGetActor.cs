using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.PaymentOrders.PaymentCostMovements;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.PaymentOrders.PaymentCostMovementGetActors;

public sealed class BasePaymentCostMovementGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public BasePaymentCostMovementGetActor(
        IDbConnectionFactory connectionFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<GetPaymentCostMovementByNetIdMessage>(ProcessGetPaymentCostMovementByNetIdMessage);

        Receive<GetAllPaymentCostMovementsMessage>(ProcessGetAllPaymentCostMovementsMessage);

        Receive<GetAllPaymentCostMovementsFromSearchMessage>(ProcessGetAllPaymentCostMovementsFromSearchMessage);
    }

    private void ProcessGetPaymentCostMovementByNetIdMessage(GetPaymentCostMovementByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentCostMovementRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetAllPaymentCostMovementsMessage(GetAllPaymentCostMovementsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentCostMovementRepository(connection).GetAll());
    }

    private void ProcessGetAllPaymentCostMovementsFromSearchMessage(GetAllPaymentCostMovementsFromSearchMessage message) {
        if (string.IsNullOrEmpty(message.Value)) message.Value = string.Empty;

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentCostMovementRepository(connection).GetAllFromSearch(message.Value));
    }
}