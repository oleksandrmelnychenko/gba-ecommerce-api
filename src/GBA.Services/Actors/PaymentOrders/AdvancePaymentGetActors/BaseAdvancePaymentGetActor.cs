using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.PaymentOrders.AdvancePayments;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.PaymentOrders.AdvancePaymentGetActors;

public sealed class BaseAdvancePaymentGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public BaseAdvancePaymentGetActor(
        IDbConnectionFactory connectionFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<GetAdvancePaymentByNetIdMessage>(ProcessGetAdvancePaymentByNetIdMessage);

        Receive<GetAllAdvancePaymentsFilteredMessage>(ProcessGetAllAdvancePaymentsFilteredMessage);
    }

    private void ProcessGetAdvancePaymentByNetIdMessage(GetAdvancePaymentByNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _paymentOrderRepositoriesFactory.NewAdvancePaymentRepository(connection).GetByNetId(message.NetId)
            );
        } catch (Exception e) {
            Sender.Tell(e);
        }
    }

    private void ProcessGetAllAdvancePaymentsFilteredMessage(GetAllAdvancePaymentsFilteredMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _paymentOrderRepositoriesFactory.NewAdvancePaymentRepository(connection).GetAllFiltered(message.FromDate, message.ToDate)
            );
        } catch (Exception e) {
            Sender.Tell(e);
        }
    }
}