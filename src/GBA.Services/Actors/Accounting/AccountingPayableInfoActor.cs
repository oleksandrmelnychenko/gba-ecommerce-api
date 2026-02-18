using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Accounting.PayableInfo;
using GBA.Domain.Repositories.Accounting.Contracts;

namespace GBA.Services.Actors.Accounting;

public sealed class AccountingPayableInfoActor : ReceiveActor {
    private readonly IAccountingRepositoriesFactory _accountingRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public AccountingPayableInfoActor(IDbConnectionFactory connectionFactory, IAccountingRepositoriesFactory accountingRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _accountingRepositoriesFactory = accountingRepositoriesFactory;

        Receive<GetAllDebitInfoMessage>(ProcessGetAllDebitInfoMessage);

        Receive<GetAllCreditInfoMessage>(ProcessGetAllCreditInfoMessage);
    }

    private void ProcessGetAllDebitInfoMessage(GetAllDebitInfoMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection currencyExchangeConnection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _accountingRepositoriesFactory.NewAccountingPayableInfoRepository(connection, currencyExchangeConnection).GetAllDebitInfo()
        );
    }

    private void ProcessGetAllCreditInfoMessage(GetAllCreditInfoMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        using IDbConnection currencyExchangeConnection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _accountingRepositoriesFactory.NewAccountingPayableInfoRepository(connection, currencyExchangeConnection).GetAllCreditInfo()
        );
    }
}