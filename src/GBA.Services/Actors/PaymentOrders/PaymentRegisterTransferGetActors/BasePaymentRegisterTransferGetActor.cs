using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Messages.PaymentOrders.PaymentRegisters;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.PaymentOrders.PaymentRegisterTransferGetActors;

public sealed class BasePaymentRegisterTransferGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public BasePaymentRegisterTransferGetActor(
        IDbConnectionFactory connectionFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<GetAllPaymentRegisterTransfersByPaymentRegisterNetIdMessage>(ProcessGetAllPaymentRegisterTransfersByPaymentRegisterNetIdMessage);

        Receive<GetPaymentRegisterTransferByNetIdMessage>(ProcessGetPaymentRegisterTransferByNetIdMessage);
    }

    private void ProcessGetAllPaymentRegisterTransfersByPaymentRegisterNetIdMessage(GetAllPaymentRegisterTransfersByPaymentRegisterNetIdMessage message) {
        if (message.From.Year.Equals(1)) message.From = DateTime.UtcNow;
        if (message.To.Year.Equals(1)) message.To = DateTime.UtcNow;

        message.To = message.To.AddHours(23).AddMinutes(59).AddSeconds(59);

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        switch (message.Type) {
            case PaymentRegisterTransferType.Income:
                if (message.PaymentRegisterNetId.HasValue)
                    Sender.Tell(_paymentOrderRepositoriesFactory
                        .NewPaymentRegisterTransferRepository(connection)
                        .GetAllIncomingByPaymentRegisterNetId(message.PaymentRegisterNetId.Value, message.From, message.To, message.CurrencyNetId)
                    );
                else
                    Sender.Tell(new List<PaymentRegisterTransfer>());
                break;
            case PaymentRegisterTransferType.Outcome:
                if (message.PaymentRegisterNetId.HasValue)
                    Sender.Tell(_paymentOrderRepositoriesFactory
                        .NewPaymentRegisterTransferRepository(connection)
                        .GetAllOutcomingByPaymentRegisterNetId(message.PaymentRegisterNetId.Value, message.From, message.To, message.CurrencyNetId)
                    );
                else
                    Sender.Tell(new List<PaymentRegisterTransfer>());
                break;
            case PaymentRegisterTransferType.All:
            default:
                Sender.Tell(_paymentOrderRepositoriesFactory
                    .NewPaymentRegisterTransferRepository(connection)
                    .GetAllFiltered(message.From, message.To, message.PaymentRegisterNetId, message.CurrencyNetId)
                );
                break;
        }
    }

    private void ProcessGetPaymentRegisterTransferByNetIdMessage(GetPaymentRegisterTransferByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentRegisterTransferRepository(connection).GetByNetId(message.NetId));
    }
}