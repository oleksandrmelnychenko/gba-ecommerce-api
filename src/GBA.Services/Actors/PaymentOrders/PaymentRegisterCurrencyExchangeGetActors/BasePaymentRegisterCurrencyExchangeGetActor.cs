using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.PaymentOrders.PaymentRegisters;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Services.Actors.PaymentOrders.PaymentRegisterCurrencyExchangeGetActors;

public sealed class BasePaymentRegisterCurrencyExchangeGetActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;

    public BasePaymentRegisterCurrencyExchangeGetActor(
        IDbConnectionFactory connectionFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;

        Receive<CalculateCurrencyExchangeResultAmountMessage>(ProcessCalculateCurrencyExchangeResultAmountMessage);

        Receive<GetAllPaymentRegisterCurrencyExchangesByPaymentRegisterNetIdMessage>(ProcessGetAllPaymentRegisterCurrencyExchangesByPaymentRegisterNetIdMessage);

        Receive<GetPaymentRegisterCurrencyExchangeByNetIdMessage>(ProcessGetPaymentRegisterCurrencyExchangeByNetIdMessage);
    }

    private void ProcessCalculateCurrencyExchangeResultAmountMessage(CalculateCurrencyExchangeResultAmountMessage message) {
        if (message.CurrencyCode.ToLower().Equals("uah"))
            Sender.Tell(new {
                Amount = Math.Round(message.Amount / (message.ExchangeRate == 0 ? 1 : message.ExchangeRate), 2)
            });
        else
            Sender.Tell(new {
                Amount = Math.Round(message.Amount * message.ExchangeRate, 2)
            });
    }

    private void ProcessGetAllPaymentRegisterCurrencyExchangesByPaymentRegisterNetIdMessage(GetAllPaymentRegisterCurrencyExchangesByPaymentRegisterNetIdMessage message) {
        if (message.From.Year.Equals(1)) message.From = DateTime.UtcNow;
        if (message.To.Year.Equals(1)) message.To = DateTime.UtcNow;

        message.To = message.To.AddHours(23).AddMinutes(59).AddSeconds(59);

        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory
            .NewPaymentRegisterCurrencyExchangeRepository(connection)
            .GetAllByPaymentRegisterNetId(
                message.From,
                message.To,
                message.PaymentRegisterNetId,
                message.FromCurrencyNetId,
                message.ToCurrencyNetId
            )
        );
    }

    private void ProcessGetPaymentRegisterCurrencyExchangeByNetIdMessage(GetPaymentRegisterCurrencyExchangeByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_paymentOrderRepositoriesFactory.NewPaymentRegisterCurrencyExchangeRepository(connection).GetByNetId(message.NetId));
    }
}