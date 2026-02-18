using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Banks;
using GBA.Domain.Repositories.Banks.Contracts;

namespace GBA.Services.Actors.Banks;

public sealed class BankActor : ReceiveActor {
    private readonly IBankRepositoryFactory _bankRepositoryFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public BankActor(
        IDbConnectionFactory connectionFactory,
        IBankRepositoryFactory bankRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _bankRepositoryFactory = bankRepositoryFactory;

        Receive<UpdateBankMessage>(ProcessUpdateBankMessage);
    }

    private void ProcessUpdateBankMessage(UpdateBankMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IBankRepository bankRepository = _bankRepositoryFactory.NewBankRepository(connection);

        if (message.Bank.IsNew())
            bankRepository.Add(message.Bank);
        else
            bankRepository.Update(message.Bank);

        Sender.Tell(bankRepository.GetAll());
    }
}