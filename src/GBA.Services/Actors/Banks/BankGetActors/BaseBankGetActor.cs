using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Banks;
using GBA.Domain.Repositories.Banks.Contracts;

namespace GBA.Services.Actors.Banks.BankGetActors;

public sealed class BaseBankGetActor : ReceiveActor {
    private readonly IBankRepositoryFactory _bankRepositoryFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public BaseBankGetActor(
        IDbConnectionFactory connectionFactory,
        IBankRepositoryFactory bankRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _bankRepositoryFactory = bankRepositoryFactory;

        Receive<GetAllBanksMessage>(ProcessGetAllBanksMessage);
    }

    private void ProcessGetAllBanksMessage(GetAllBanksMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IBankRepository bankRepository = _bankRepositoryFactory.NewBankRepository(connection);

        Sender.Tell(bankRepository.GetAll());
    }
}